using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using TaskFlow.AvatarWorker.Consumers;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Storage;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow Avatar Worker");

    var host = Host.CreateDefaultBuilder(args)
        .UseSerilog((ctx, services, cfg) =>
            cfg.ReadFrom.Configuration(ctx.Configuration)
               .ReadFrom.Services(services)
               .Enrich.FromLogContext()
               .WriteTo.Console(new CompactJsonFormatter()))
        .ConfigureServices((_, services) =>
        {
            var postgresConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
                ?? throw new InvalidOperationException("POSTGRES_CONNECTION env var is required");

            var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
                ?? throw new InvalidOperationException("REDIS_CONNECTION env var is required");

            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConn));

            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<ITemporaryFileStore, RedisTemporaryFileStore>();
            services.AddSingleton<IBlobService, LocalFileBlobService>();

            services.AddMassTransit(x =>
            {
                x.AddConsumer<AvatarUploadConsumer>();

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
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Avatar worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
