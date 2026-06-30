namespace TaskFlow.Application.Interfaces.Services;

public interface IBlobService
{
    Task         SaveAsync(Stream content, string key, CancellationToken ct = default);
    Task<Stream> ReadAsync(string key, CancellationToken ct = default);
    Task         DeleteAsync(string key, CancellationToken ct = default);

    // Returns a presigned URL for direct client download; null when not supported (local dev).
    Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default);
}
