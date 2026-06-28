using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using StackExchange.Redis;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.FileLoader.Worker.Consumers;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Storage;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow FileLoader Worker");

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

            // Database — scoped per MassTransit consumer message
            services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConn));
            services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();

            // Redis temp store — reads file bytes uploaded by the API
            services.AddSingleton<IConnectionMultiplexer>(_ =>
                ConnectionMultiplexer.Connect(redisConn));
            services.AddSingleton<ITemporaryFileStore, RedisTemporaryFileStore>();

            // Blob service — writes processed files to permanent disk storage
            services.AddSingleton<IBlobService, LocalFileBlobService>();

            services.AddMassTransit(x =>
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
        })
        .Build();

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
