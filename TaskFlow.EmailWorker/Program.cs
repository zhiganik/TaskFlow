using MassTransit;
using Serilog;
using Serilog.Formatting.Compact;
using TaskFlow.EmailWorker.Consumers;
using TaskFlow.Infrastructure.Logging;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting TaskFlow Email Worker");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(new CompactJsonFormatter())
           .AddSeqIfConfigured("taskflow-email-worker"));

    var resendApiKey = Environment.GetEnvironmentVariable("RESEND_API_KEY")
        ?? throw new InvalidOperationException("RESEND_API_KEY env var is required");

    builder.Services.AddHttpClient("resend", client =>
    {
        client.BaseAddress = new Uri("https://api.resend.com/");
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {resendApiKey}");
    });

    builder.Services.AddMassTransit(x =>
    {
        x.AddConsumer<SendInvitationEmailConsumer>();

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

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.MapHealthChecks("/health");

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Email worker host terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
