using Serilog;

namespace TaskFlow.Infrastructure.Logging;

public static class SerilogExtensions
{
    /// <summary>
    /// Enriches every log event with the service name and — if SEQ_URL is set —
    /// adds a Seq sink so all services appear in one place.
    /// SEQ_API_KEY is optional (omit for Seq instances without auth).
    /// </summary>
    public static LoggerConfiguration AddSeqIfConfigured(
        this LoggerConfiguration cfg, string applicationName)
    {
        cfg = cfg.Enrich.WithProperty("Application", applicationName);

        var url = Environment.GetEnvironmentVariable("SEQ_URL");
        if (string.IsNullOrWhiteSpace(url))
            return cfg;

        var apiKey = Environment.GetEnvironmentVariable("SEQ_API_KEY");
        return cfg.WriteTo.Seq(url, apiKey: string.IsNullOrWhiteSpace(apiKey) ? null : apiKey);
    }
}
