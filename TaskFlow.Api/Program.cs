using Serilog;
using Serilog.Formatting.Json;
using TaskFlow.Api.Config;
using TaskFlow.Infrastructure.Logging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow API");
    DotNetEnv.Env.TraversePath().Load();
    DotNetEnv.Env.TraversePath().Load(".env.local"); // host-only overrides; silently skipped if absent
    
    var builder = WebApplication.CreateBuilder(args);

    builder.Services.AddDependencies(builder.Configuration);
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new JsonFormatter())
           .AddSeqIfConfigured("taskflow-api"));

    var app = builder.Build();

    app.UseApplicationPipeline();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
