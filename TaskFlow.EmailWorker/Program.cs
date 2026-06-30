using FluentEmail.MailKitSmtp;
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

    // SMTP / email
    var smtpHost  = Environment.GetEnvironmentVariable("SMTP__HOST")      ?? "localhost";
    var smtpPort  = int.TryParse(Environment.GetEnvironmentVariable("SMTP__PORT"), out var p) ? p : 1025;
    var fromEmail = Environment.GetEnvironmentVariable("SMTP__FROMEMAIL") ?? "noreply@taskflow.local";
    var fromName  = Environment.GetEnvironmentVariable("SMTP__FROMNAME")  ?? "TaskFlow";
    var username  = Environment.GetEnvironmentVariable("SMTP__USERNAME");
    var password  = Environment.GetEnvironmentVariable("SMTP__PASSWORD");

    builder.Services
        .AddFluentEmail(fromEmail, fromName)
        .AddMailKitSender(new SmtpClientOptions
        {
            Server                  = smtpHost,
            Port                    = smtpPort,
            UseSsl                  = smtpPort == 465,
            RequiresAuthentication  = !string.IsNullOrWhiteSpace(username),
            User                    = username ?? string.Empty,
            Password                = password ?? string.Empty,
        });

    // MassTransit
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
