namespace TaskFlow.Application.Interfaces.Services;

public interface ITemporaryFileStore
{
    Task          StoreAsync(string key, Stream content, TimeSpan expiry, CancellationToken ct = default);
    Task<byte[]?> GetAsync(string key, CancellationToken ct = default);
    Task          DeleteAsync(string key, CancellationToken ct = default);
}
