namespace TaskFlow.Application.Interfaces.Services;

public interface IBlobService
{
    Task         SaveAsync(Stream content, string storagePath, CancellationToken ct = default);
    Task<Stream> ReadAsync(string storagePath, CancellationToken ct = default);
    Task         DeleteAsync(string storagePath, CancellationToken ct = default);
}
