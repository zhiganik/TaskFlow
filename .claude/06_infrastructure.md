# 06 — Infrastructure: Redis, File Storage, Background Services, Serilog, Middleware

## Redis Caching

### ICacheService Interface
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default);
    Task InvalidateAsync(string key, CancellationToken ct = default);
}
```

### RedisCacheService Implementation
```csharp
public class RedisCacheService(IConnectionMultiplexer redis, IOptions<CacheOptions> opts)
    : ICacheService
{
    private readonly IDatabase _db = redis.GetDatabase();
    private readonly TimeSpan _defaultExpiry =
        TimeSpan.FromMinutes(opts.Value.DefaultExpiryMinutes);

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default)
    {
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task SetAsync<T>(string key, T value,
        TimeSpan? expiry = null, CancellationToken ct = default)
        => await _db.StringSetAsync(key, JsonSerializer.Serialize(value), expiry ?? _defaultExpiry);

    public async Task InvalidateAsync(string key, CancellationToken ct = default)
        => await _db.KeyDeleteAsync(key);
}
```

Cache keys: `workspace:{id}`, `workspace:{workspaceId}:projects`

---

## File Storage — IBlobService

`IBlobService` is the abstraction for all file I/O. The local disk implementation is used now. The interface is designed so Azure Blob Storage can replace it with zero changes to services or controllers.

```csharp
// TaskFlow.Application/Interfaces/IBlobService.cs
public interface IBlobService
{
    /// <summary>Persist the stream to storage. Returns the stored file path.</summary>
    Task<string> SaveAsync(Stream content, string storedFileName, CancellationToken ct = default);

    /// <summary>Open a read stream for the stored file.</summary>
    Task<Stream> ReadAsync(string storagePath, CancellationToken ct = default);

    /// <summary>Delete the file. Does not throw if the file is already gone.</summary>
    Task DeleteAsync(string storagePath, CancellationToken ct = default);
}
```

### LocalFileBlobService
```csharp
// TaskFlow.Infrastructure/Storage/LocalFileBlobService.cs
public class LocalFileBlobService(IOptions<StorageOptions> opts) : IBlobService
{
    private readonly string _basePath = opts.Value.BasePath;

    public async Task<string> SaveAsync(
        Stream content, string storedFileName, CancellationToken ct = default)
    {
        Directory.CreateDirectory(_basePath);
        var fullPath = Path.Combine(_basePath, storedFileName);

        await using var fs = File.Create(fullPath);
        await content.CopyToAsync(fs, ct);

        return fullPath;
    }

    public Task<Stream> ReadAsync(string storagePath, CancellationToken ct = default)
    {
        if (!File.Exists(storagePath))
            throw new NotFoundException($"File not found at path: {storagePath}");

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
```

Registration:
```csharp
services.Configure<StorageOptions>(config.GetSection("Storage"));
services.AddSingleton<IBlobService, LocalFileBlobService>();
```

> **Azure migration path:** Replace `LocalFileBlobService` with `AzureBlobService` that wraps `BlobContainerClient`. Register it in DependencyConfig. No other file changes needed.

---

## File Processing Queue — Channel<T>

`IFileProcessingQueue` decouples the upload endpoint (producer) from the background processing worker (consumer). Backed by a bounded `Channel<Guid>` in-process. Azure Service Bus replaces the implementation without touching services.

```csharp
// TaskFlow.Application/Interfaces/IFileProcessingQueue.cs
public interface IFileProcessingQueue
{
    ValueTask EnqueueAsync(Guid attachmentId, CancellationToken ct = default);
    IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct = default);
}
```

### InMemoryFileProcessingQueue
```csharp
// TaskFlow.Infrastructure/Queue/InMemoryFileProcessingQueue.cs
public class InMemoryFileProcessingQueue : IFileProcessingQueue
{
    // Bounded channel — backpressure if the consumer falls behind
    private readonly Channel<Guid> _channel =
        Channel.CreateBounded<Guid>(new BoundedChannelOptions(500)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = true,
            SingleWriter = false  // multiple HTTP requests can write concurrently
        });

    public ValueTask EnqueueAsync(Guid attachmentId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(attachmentId, ct);

    public IAsyncEnumerable<Guid> ReadAllAsync(CancellationToken ct = default)
        => _channel.Reader.ReadAllAsync(ct);
}
```

Registration:
```csharp
// Singleton — the channel must live for the entire app lifetime
services.AddSingleton<IFileProcessingQueue, InMemoryFileProcessingQueue>();
```

---

## TaskAttachmentService

```csharp
// TaskFlow.Application/Services/TaskAttachmentService.cs
public class TaskAttachmentService(
    ITaskAttachmentRepository attachmentRepo,
    ITaskRepository taskRepo,
    IBlobService blobService,
    IFileProcessingQueue queue,
    IOptions<StorageOptions> storageOpts,
    ILogger<TaskAttachmentService> logger)
{
    private readonly StorageOptions _storage = storageOpts.Value;

    public async Task<AttachmentDto> UploadAsync(
        Guid taskId, IFormFile file, string uploadedById, CancellationToken ct)
    {
        // 1. Validate task exists
        _ = await taskRepo.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException($"Task {taskId} not found.");

        // 2. Validate file
        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_storage.AllowedExtensions.Contains(ext))
            throw new ValidationException($"File type '{ext}' is not allowed.");

        if (file.Length > _storage.MaxFileSizeBytes)
            throw new ValidationException(
                $"File exceeds maximum size of {_storage.MaxFileSizeBytes / 1_048_576} MB.");

        // 3. Generate server-controlled filename — never use the original
        var storedFileName = $"{Guid.NewGuid()}{ext}";

        // 4. Save to disk via IBlobService
        await using var stream = file.OpenReadStream();
        var storagePath = await blobService.SaveAsync(stream, storedFileName, ct);

        // 5. Persist record with Pending status
        var attachment = new TaskAttachment
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UploadedById = uploadedById,
            OriginalFileName = file.FileName,
            ContentType = file.ContentType,
            FileSizeBytes = file.Length,
            StoredFileName = storedFileName,
            StoragePath = storagePath,
            Status = AttachmentStatus.Pending,
            UploadedAt = DateTime.UtcNow
        };

        await attachmentRepo.AddAsync(attachment, ct);

        // 6. Enqueue for background processing
        await queue.EnqueueAsync(attachment.Id, ct);

        logger.LogInformation(
            "Attachment {AttachmentId} uploaded for task {TaskId} by {UserId}. " +
            "File: {FileName} ({SizeBytes} bytes). Queued for processing.",
            attachment.Id, taskId, uploadedById, file.FileName, file.Length);

        return attachment.ToDto();
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadAsync(
        Guid taskId, Guid attachmentId, CancellationToken ct)
    {
        var attachment = await attachmentRepo.GetByIdAsync(attachmentId, ct)
            ?? throw new NotFoundException($"Attachment {attachmentId} not found.");

        if (attachment.TaskId != taskId)
            throw new NotFoundException($"Attachment {attachmentId} not found.");

        if (attachment.Status != AttachmentStatus.Ready)
            throw new ConflictException(
                $"Attachment is not ready yet. Current status: {attachment.Status}.");

        var stream = await blobService.ReadAsync(attachment.StoragePath, ct);
        return (stream, attachment.OriginalFileName, attachment.ContentType);
    }

    public async Task DeleteAsync(Guid taskId, Guid attachmentId, CancellationToken ct)
    {
        var attachment = await attachmentRepo.GetByIdAsync(attachmentId, ct)
            ?? throw new NotFoundException($"Attachment {attachmentId} not found.");

        if (attachment.TaskId != taskId)
            throw new NotFoundException($"Attachment {attachmentId} not found.");

        await blobService.DeleteAsync(attachment.StoragePath, ct);
        await attachmentRepo.DeleteAsync(attachmentId, ct);

        logger.LogInformation(
            "Attachment {AttachmentId} deleted from task {TaskId}",
            attachmentId, taskId);
    }
}
```

---

## Background Service — FileProcessingService

Consumes `IFileProcessingQueue` and processes each attachment. On startup it also recovers any `Pending` or `Processing` records from the DB (handles app restarts mid-processing).

```csharp
// TaskFlow.Api/BackgroundServices/FileProcessingService.cs
public class FileProcessingService(
    IFileProcessingQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<FileProcessingService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("FileProcessingService started");

        // On startup, recover any attachments left in Pending/Processing state
        await RecoverPendingAttachmentsAsync(stoppingToken);

        // Main loop — consume from Channel<T>
        await foreach (var attachmentId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessAttachmentAsync(attachmentId, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex,
                    "Failed to process attachment {AttachmentId}", attachmentId);

                // Mark as Failed in DB so it doesn't block forever
                await MarkFailedAsync(attachmentId, ex.Message, stoppingToken);
            }
        }
    }

    private async Task ProcessAttachmentAsync(Guid attachmentId, CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskAttachmentRepository>();

        var attachment = await repo.GetByIdAsync(attachmentId, ct);
        if (attachment is null)
        {
            logger.LogWarning(
                "Attachment {AttachmentId} not found — skipping", attachmentId);
            return;
        }

        // Mark as Processing
        attachment.Status = AttachmentStatus.Processing;
        await repo.UpdateAsync(attachment, ct);

        logger.LogInformation(
            "Processing attachment {AttachmentId} ({FileName})",
            attachmentId, attachment.OriginalFileName);

        // --- Processing logic goes here ---
        // Stub: simulate work (e.g. virus scan, thumbnail generation, metadata extraction)
        // Azure upgrade: call Azure Cognitive Services, push to Service Bus, etc.
        await Task.Delay(TimeSpan.FromSeconds(1), ct);
        // ----------------------------------

        attachment.Status = AttachmentStatus.Ready;
        attachment.ProcessedAt = DateTime.UtcNow;
        await repo.UpdateAsync(attachment, ct);

        logger.LogInformation(
            "Attachment {AttachmentId} processed successfully", attachmentId);
    }

    private async Task RecoverPendingAttachmentsAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskAttachmentRepository>();

        // Uses IX_TaskAttachments_Status index
        var stuck = await repo.GetByStatusAsync(
            [AttachmentStatus.Pending, AttachmentStatus.Processing], ct);

        foreach (var attachment in stuck)
        {
            logger.LogWarning(
                "Recovering stuck attachment {AttachmentId} (was {Status})",
                attachment.Id, attachment.Status);

            await queue.EnqueueAsync(attachment.Id, ct);
        }

        if (stuck.Count > 0)
            logger.LogInformation(
                "Recovered {Count} stuck attachment(s) on startup", stuck.Count);
    }

    private async Task MarkFailedAsync(Guid attachmentId, string error, CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var repo = scope.ServiceProvider.GetRequiredService<ITaskAttachmentRepository>();

            var attachment = await repo.GetByIdAsync(attachmentId, ct);
            if (attachment is null) return;

            attachment.Status = AttachmentStatus.Failed;
            attachment.ProcessingError = error;
            await repo.UpdateAsync(attachment, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to mark attachment {AttachmentId} as Failed", attachmentId);
        }
    }
}
```

Registration:
```csharp
services.AddHostedService<FileProcessingService>();
```

---

## Background Service — DeadlineNotifierService

```csharp
public class DeadlineNotifierService(
    IServiceScopeFactory scopeFactory,
    ILogger<DeadlineNotifierService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await CheckOverdueTasksAsync(stoppingToken); }
            catch (Exception ex) when (ex is not OperationCanceledException)
            { logger.LogError(ex, "Error in DeadlineNotifierService"); }

            await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
        }
    }

    private async Task CheckOverdueTasksAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var repo = scope.ServiceProvider.GetRequiredService<ITaskRepository>();

        var overdue = await repo.GetOverdueAsync(ct);  // uses IX_Tasks_DueDate

        foreach (var task in overdue)
            logger.LogWarning(
                "Task {TaskId} '{Title}' overdue. Assignee: {AssigneeId}. Due: {DueDate:yyyy-MM-dd}",
                task.Id, task.Title, task.AssigneeId, task.DueDate);
    }
}
```

---

## Serilog Setup

```csharp
// Program.cs — bootstrap logger first (before host build)
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

// Inside try block:
builder.Host.UseSerilog((ctx, services, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration)
       .ReadFrom.Services(services)
       .Enrich.FromLogContext()
       .Enrich.WithMachineName()
       .WriteTo.Console(new JsonFormatter()));
```

Structured logging — always use named properties:
```csharp
// ✅
_logger.LogInformation("Attachment {AttachmentId} processed in {ElapsedMs}ms",
    id, elapsed);
// ❌
_logger.LogInformation($"Attachment {id} processed");
```

---

## GlobalExceptionMiddleware

```csharp
public class GlobalExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext ctx, ILogger<GlobalExceptionMiddleware> logger)
    {
        try { await next(ctx); }
        catch (Exception ex) { await HandleAsync(ctx, ex, logger); }
    }

    private static async Task HandleAsync(HttpContext ctx, Exception ex, ILogger logger)
    {
        var (status, title) = ex switch
        {
            NotFoundException  e => (404, e.Message),
            ConflictException  e => (409, e.Message),
            ForbiddenException e => (403, e.Message),
            ValidationException e => (400, e.Message),  // domain validation (not FluentValidation)
            _                    => (500, "An unexpected error occurred.")
        };

        if (status == 500)
            logger.LogError(ex, "Unhandled exception on {Method} {Path}",
                ctx.Request.Method, ctx.Request.Path);

        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        await ctx.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = status, Title = title, Instance = ctx.Request.Path
        });
    }
}
```

Custom exception types in Application layer:
```csharp
public class NotFoundException(string message)   : Exception(message);
public class ConflictException(string message)   : Exception(message);
public class ForbiddenException(string message)  : Exception(message);
public class ValidationException(string message) : Exception(message);  // domain rules, not FluentValidation
```

---

## ITaskAttachmentRepository

```csharp
public interface ITaskAttachmentRepository
{
    Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<IReadOnlyList<TaskAttachment>> GetByStatusAsync(
        IEnumerable<AttachmentStatus> statuses, CancellationToken ct = default);
    Task AddAsync(TaskAttachment attachment, CancellationToken ct = default);
    Task UpdateAsync(TaskAttachment attachment, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
```

`GetByStatusAsync` uses `IX_TaskAttachments_Status` — called only on startup recovery, not per-request.
