using MassTransit;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.FileWorker.Consumers;

public class FileUploadConsumer(
    ITaskAttachmentRepository repo,
    IBlobService blobService,
    ITemporaryFileStore tempStore,
    ILogger<FileUploadConsumer> logger) : IConsumer<FileUploadMessage>
{
    public async Task Consume(ConsumeContext<FileUploadMessage> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var attachment = await repo.GetByIdAsync(msg.AttachmentId, ct);
        if (attachment is null)
        {
            logger.LogWarning(
                "Attachment {AttachmentId} not found — message may be stale, skipping",
                msg.AttachmentId);
            return;
        }

        attachment.Status = AttachmentStatus.Processing;
        await repo.UpdateAsync(attachment, ct);

        try
        {
            // Read from Redis temp store
            var fileBytes = await tempStore.GetAsync(msg.RedisKey, ct);
            if (fileBytes is null)
            {
                throw new InvalidOperationException(
                    $"Temp file expired in Redis before processing (key: {msg.RedisKey}). " +
                    $"Consider increasing the Redis TTL.");
            }

            // Save to permanent disk storage
            using var stream = new MemoryStream(fileBytes);
            await blobService.SaveAsync(stream, msg.PermanentPath, ct);

            // Clean up the Redis key
            await tempStore.DeleteAsync(msg.RedisKey, ct);

            // Mark as Ready
            attachment.StoragePath = msg.PermanentPath;
            attachment.Status      = AttachmentStatus.Ready;
            attachment.ProcessedAt = DateTime.UtcNow;
            await repo.UpdateAsync(attachment, ct);

            logger.LogInformation(
                "Attachment {AttachmentId} processed — {FileName} for task {TaskId}",
                msg.AttachmentId, msg.OriginalFileName, msg.TaskId);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex,
                "Failed to process attachment {AttachmentId} ({FileName})",
                msg.AttachmentId, msg.OriginalFileName);

            attachment.Status          = AttachmentStatus.Failed;
            attachment.ProcessingError = ex.Message;
            await repo.UpdateAsync(attachment, ct);
        }
    }
}
