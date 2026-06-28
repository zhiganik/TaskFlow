using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.FileLoader.Worker.Consumers;
using TaskFlow.Infrastructure.HealthChecks; // AddInfrastructureChecks
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Storage;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow FileLoader Worker");

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

    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConn));
    builder.Services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConn));
    builder.Services.AddSingleton<ITemporaryFileStore, RedisTemporaryFileStore>();
    builder.Services.AddSingleton<IBlobService, LocalFileBlobService>();

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<FileUploadConsumer>();

        x.UsingRabbitMq((ctx, cfg) =>
        {
            var rabbitHost = Environment.GetEnvironmentVariable("RABBITMQ_HOST")
                ?? throw new InvalidOperationException("RABBITMQ_HOST env var is required");
            var rabbitUser = Environment.GetEnvironmentVariable("RABBITMQ_USER")
                ?? throw new InvalidOperationException("RABBITMQ_USER env var is required");
            var rabbitPass = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
                ?? throw new InvalidOperationException("RABBITMQ_PASSWORD env var is required");

            cfg.Host(rabbitHost, "/", h =>
            {
                h.Username(rabbitUser);
                h.Password(rabbitPass);
            });

            cfg.ConfigureEndpoints(ctx);
        });
    });

    builder.Services.AddHealthChecks().AddInfrastructureChecks();

    var app = builder.Build();

    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
