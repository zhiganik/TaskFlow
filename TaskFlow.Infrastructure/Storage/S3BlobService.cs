using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;

namespace TaskFlow.Infrastructure.Storage;

public sealed class S3BlobService : IBlobService, IDisposable
{
    private readonly AmazonS3Client _client;
    private readonly string         _bucketName;

    public S3BlobService(IOptions<S3Options> opts)
    {
        var o = opts.Value;
        _bucketName = o.BucketName;

        var config = new AmazonS3Config
        {
            ServiceURL           = o.ServiceUrl,
            ForcePathStyle       = true,
            AuthenticationRegion = o.Region,
        };

        _client = new AmazonS3Client(new BasicAWSCredentials(o.AccessKey, o.SecretKey), config);
    }

    public async Task SaveAsync(Stream content, string key, CancellationToken ct = default)
    {
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName  = _bucketName,
            Key         = key,
            InputStream = content,
        }, ct);
    }

    public async Task<Stream> ReadAsync(string key, CancellationToken ct = default)
    {
        try
        {
            var response = await _client.GetObjectAsync(_bucketName, key, ct);
            return response.ResponseStream;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            throw new NotFoundException($"File not found: {key}");
        }
    }

    public async Task DeleteAsync(string key, CancellationToken ct = default)
    {
        await _client.DeleteObjectAsync(_bucketName, key, ct);
    }

    public Task<string?> GetDownloadUrlAsync(string key, TimeSpan expiry, CancellationToken ct = default)
    {
        var url = _client.GetPreSignedURL(new GetPreSignedUrlRequest
        {
            BucketName = _bucketName,
            Key        = key,
            Expires    = DateTime.UtcNow.Add(expiry),
            Verb       = HttpVerb.GET,
        });
        return Task.FromResult<string?>(url);
    }

    public void Dispose() => _client.Dispose();
}
