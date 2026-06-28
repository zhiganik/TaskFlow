using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Infrastructure.Storage;

public class LocalFileBlobService : IBlobService
{
    public async Task SaveAsync(Stream content, string storagePath, CancellationToken ct = default)
    {
        var dir = Path.GetDirectoryName(storagePath);
        if (dir is not null)
            Directory.CreateDirectory(dir);

        await using var fs = File.Create(storagePath);
        await content.CopyToAsync(fs, ct);
    }

    public Task<Stream> ReadAsync(string storagePath, CancellationToken ct = default)
    {
        if (!File.Exists(storagePath))
            throw new NotFoundException($"File not found: {storagePath}");

        Stream stream = File.OpenRead(storagePath);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storagePath, CancellationToken ct = default)
    {
        if (File.Exists(storagePath))
            File.Delete(storagePath);

        return Task.CompletedTask;
    }
}
