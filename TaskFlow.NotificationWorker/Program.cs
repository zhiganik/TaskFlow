using System.Text;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.HealthChecks;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.NotificationWorker.Consumers;
using TaskFlow.NotificationWorker.Hubs;
using TaskFlow.NotificationWorker.Services;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow Notification Worker");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new CompactJsonFormatter()));

    var postgresConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
        ?? throw new InvalidOperationException("POSTGRES_CONNECTION env var is required");
    var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
        ?? throw new InvalidOperationException("REDIS_CONNECTION env var is required");
    var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET")
        ?? throw new InvalidOperationException("JWT_SECRET env var is required");
    var jwtIssuer   = Environment.GetEnvironmentVariable("JWT_ISSUER")   ?? "taskflow-api";
    var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? "taskflow-clients";

    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConn));

    var mux = ConnectionMultiplexer.Connect(redisConn);
    builder.Services.AddSingleton<IConnectionMultiplexer>(mux);

    builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
    builder.Services.AddScoped<INotificationDispatchService, NotificationDispatchService>();

    // SignalR backplane gets its own dedicated connection — do not share mux.
    // The backplane manages subscriptions on a separate physical connection from
    // the one used by IConnectionMultiplexer (health checks, future presence, etc.).
    builder.Services.AddSignalR().AddStackExchangeRedis(redisConn);

    builder.Services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer           = true,
                ValidateAudience         = true,
                ValidateLifetime         = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer              = jwtIssuer,
                ValidAudience            = jwtAudience,
                IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
            };

            opts.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var token = context.Request.Query["access_token"];
                    if (!string.IsNullOrEmpty(token) &&
                        context.Request.Path.StartsWithSegments("/hubs"))
                    {
                        context.Token = token;
                    }
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<CommentPostedConsumer>();
        x.AddConsumer<TaskAssignedConsumer>();
        x.AddConsumer<TaskStatusChangedConsumer>();
        x.AddConsumer<MemberInvitedConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            var host = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                ?? throw new InvalidOperationException("RABBITMQ_HOST env var is required");
            var user = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                ?? throw new InvalidOperationException("RABBITMQ_USER env var is required");
            var pass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                ?? throw new InvalidOperationException("RABBITMQ_PASSWORD env var is required");

            cfg.Host(host, "/", h =>
            {
                h.Username(user);
                h.Password(pass);
            });

            cfg.ConfigureEndpoints(ctx);
        });
    });

    builder.Services.AddHealthChecks().AddInfrastructureChecks();

    var app = builder.Build();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapHub<NotificationHub>("/hubs/notifications");
    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Notification worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
