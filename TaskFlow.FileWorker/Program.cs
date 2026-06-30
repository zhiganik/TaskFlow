using MassTransit;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using TaskFlow.Infrastructure.Logging;
using StackExchange.Redis;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;
using TaskFlow.FileWorker.Consumers;
using TaskFlow.Infrastructure.HealthChecks; // AddInfrastructureChecks
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Repositories;
using TaskFlow.Infrastructure.Storage;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow File Worker");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new CompactJsonFormatter())
           .AddSeqIfConfigured("taskflow-file-worker"));

    var postgresConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
        ?? throw new InvalidOperationException("POSTGRES_CONNECTION env var is required");

    var redisConn = Environment.GetEnvironmentVariable("REDIS_CONNECTION")
        ?? throw new InvalidOperationException("REDIS_CONNECTION env var is required");

    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConn));
    builder.Services.AddScoped<ITaskAttachmentRepository, TaskAttachmentRepository>();

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
        ConnectionMultiplexer.Connect(redisConn));
    builder.Services.AddSingleton<ITemporaryFileStore, RedisTemporaryFileStore>();

    builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
    builder.Services.Configure<S3Options>(opts =>
    {
        opts.BucketName = Environment.GetEnvironmentVariable("S3_BUCKET_NAME") ?? string.Empty;
        opts.ServiceUrl = Environment.GetEnvironmentVariable("S3_SERVICE_URL") ?? string.Empty;
        opts.AccessKey  = Environment.GetEnvironmentVariable("S3_ACCESS_KEY")  ?? string.Empty;
        opts.SecretKey  = Environment.GetEnvironmentVariable("S3_SECRET_KEY")  ?? string.Empty;
        opts.Region     = Environment.GetEnvironmentVariable("S3_REGION")      ?? "auto";
    });

    var storageType = Environment.GetEnvironmentVariable("STORAGE_TYPE") ?? "local";
    if (storageType == "s3")
        builder.Services.AddSingleton<IBlobService, S3BlobService>();
    else
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

            // Each worker pulls 1 message at a time — ensures even distribution across all replicas
            cfg.PrefetchCount = 1;

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
