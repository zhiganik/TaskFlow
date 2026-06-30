using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Formatting.Compact;
using TaskFlow.Infrastructure.Logging;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.ArchiveWorker.Services;
using TaskFlow.Infrastructure.HealthChecks;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Infrastructure.Repositories;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow Archive Worker");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new CompactJsonFormatter())
           .AddSeqIfConfigured("taskflow-archive-worker"));

    var postgresConn = Environment.GetEnvironmentVariable("POSTGRES_CONNECTION")
        ?? throw new InvalidOperationException("POSTGRES_CONNECTION env var is required");

    builder.Services.AddDbContext<AppDbContext>(opts => opts.UseNpgsql(postgresConn));
    builder.Services.AddScoped<IArchiveRepository, ArchiveRepository>();

    builder.Services.AddHostedService<ArchiveWorkerService>();

    builder.Services.AddHealthChecks().AddCheck<PostgresHealthCheck>("postgres");

    var app = builder.Build();

    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Archive worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
