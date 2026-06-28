using MassTransit;
using Serilog;
using Serilog.Formatting.Compact;
using TaskFlow.FileLoader.Worker.Consumers;

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
        .ConfigureServices(services =>
        {
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
