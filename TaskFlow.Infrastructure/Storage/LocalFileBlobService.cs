using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;

namespace TaskFlow.Infrastructure.Storage;

public class LocalFileBlobService(
    IOptions<StorageOptions> opts,
    ILogger<LocalFileBlobService> logger) : IBlobService
{
    private readonly string _basePath = opts.Value.BasePath;

    public async Task SaveAsync(Stream content, string key, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(key);
        var dir = Path.GetDirectoryName(fullPath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream> ReadAsync(string key, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(key);
        logger.LogInformation("Reading file {StoragePath}", fullPath);

        if (!File.Exists(fullPath))
            throw new NotFoundException($"File not found: {key}");

        Stream stream = File.OpenRead(fullPath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string key, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(key);
        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }

    public Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
        => Task.FromResult<string?>(null);

    private string ToFullPath(string key) =>
        Path.Combine(_basePath, key.Replace('/', Path.DirectorySeparatorChar));
}
