using AutoMapper;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Caching;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.Application.Services;

public class TaskAttachmentService(
    ITaskAttachmentRepository attachmentRepo,
    IWorkspaceTasksRepository tasksRepo,
    IBlobService blobService,
    ITemporaryFileStore tempStore,
    IMessagePublisher publisher,
    IOptions<StorageOptions> storageOpts,
    IMapper mapper,
    ILogger<TaskAttachmentService> logger) : ITaskAttachmentService
{
    private readonly StorageOptions _storage = storageOpts.Value;

    public async Task<AttachmentDto> GetByIdAsync(
        Guid workspaceId, Guid taskId, Guid id, CancellationToken ct = default)
    {
        var attachment = await GetAttachmentAsync(workspaceId, taskId, id, ct);
        return mapper.Map<AttachmentDto>(attachment);
    }

    public async Task<IReadOnlyList<AttachmentDto>> GetByTaskIdAsync(
        Guid workspaceId, Guid taskId, CancellationToken ct = default)
    {
        await VerifyTaskOwnershipAsync(workspaceId, taskId, ct);
        var attachments = await attachmentRepo.GetByTaskIdAsync(taskId, ct);
        return attachments.Select(mapper.Map<AttachmentDto>).ToList().AsReadOnly();
    }

    public async Task<AttachmentDto> UploadAsync(
        Guid workspaceId, Guid taskId, string uploadedById,
        UploadFileRequest file,
        Guid? commentId = null,
        CancellationToken ct = default)
    {
        await VerifyTaskOwnershipAsync(workspaceId, taskId, ct);

        if (file.SizeBytes > _storage.MaxFileSizeBytes)
            throw new BadRequestException(
                $"File exceeds the {_storage.MaxFileSizeBytes / 1_048_576} MB limit.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (_storage.BlockedExtensions.Contains(ext))
            throw new BadRequestException($"File type '{ext}' is not allowed.");

        var storedFileName = string.IsNullOrEmpty(ext)
            ? Guid.NewGuid().ToString()
            : $"{Guid.NewGuid()}{ext}";

        var attachment = new TaskAttachment
        {
            TaskId           = taskId,
            CommentId        = commentId,
            UploadedById     = uploadedById,
            OriginalFileName = file.FileName,
            ContentType      = file.ContentType,
            FileSizeBytes    = file.SizeBytes,
            StoredFileName   = storedFileName,
            StoragePath      = string.Empty,
            Status           = AttachmentStatus.Pending,
        };

        await attachmentRepo.AddAsync(attachment, ct);

        var redisKey      = CacheKeys.TempFile(attachment.Id);
        var permanentPath = Path.Combine(_storage.BasePath, "processed", storedFileName);

        await tempStore.StoreAsync(redisKey, file.Stream, CacheKeys.Ttl.TempFile, ct);

        await publisher.PublishAsync(new FileUploadMessage
        {
            AttachmentId     = attachment.Id,
            RedisKey         = redisKey,
            PermanentPath    = permanentPath,
            StoredFileName   = storedFileName,
            OriginalFileName = file.FileName,
            ContentType      = file.ContentType,
            FileSizeBytes    = file.SizeBytes,
            WorkspaceId      = workspaceId,
            TaskId           = taskId,
            UploadedByUserId = uploadedById,
            UploadedAt       = DateTimeOffset.UtcNow,
        }, ct);

        logger.LogInformation(
            "Attachment {AttachmentId} queued for task {TaskId} by {UserId} — {FileName} ({SizeBytes} bytes)",
            attachment.Id, taskId, uploadedById, file.FileName, file.SizeBytes);

        var created = await attachmentRepo.GetByIdAsync(attachment.Id, ct) ?? attachment;
        return mapper.Map<AttachmentDto>(created);
    }

    public async Task<(Stream Stream, string FileName, string ContentType)> DownloadAsync(
        Guid workspaceId, Guid taskId, Guid id, CancellationToken ct = default)
    {
        var attachment = await GetAttachmentAsync(workspaceId, taskId, id, ct);

        if (attachment.Status != AttachmentStatus.Ready)
            throw new ConflictException(
                $"Attachment is not ready for download. Current status: {attachment.Status}.");

        var stream = await blobService.ReadAsync(attachment.StoragePath, ct);
        return (stream, attachment.OriginalFileName, attachment.ContentType);
    }

    public async Task DeleteAsync(
        Guid workspaceId, Guid taskId, Guid id, CancellationToken ct = default)
    {
        var attachment = await GetAttachmentAsync(workspaceId, taskId, id, ct);

        if (attachment.Status == AttachmentStatus.Ready)
            await blobService.DeleteAsync(attachment.StoragePath, ct);

        await attachmentRepo.DeleteAsync(id, ct);

        logger.LogInformation("Attachment {AttachmentId} deleted from task {TaskId}", id, taskId);
    }

    private async Task VerifyTaskOwnershipAsync(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var task = await tasksRepo.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException($"Task {taskId} not found.");

        if (task.WorkspaceId != workspaceId)
            throw new NotFoundException($"Task {taskId} not found.");
    }

    private async Task<TaskAttachment> GetAttachmentAsync(
        Guid workspaceId, Guid taskId, Guid id, CancellationToken ct)
    {
        await VerifyTaskOwnershipAsync(workspaceId, taskId, ct);

        var attachment = await attachmentRepo.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Attachment {id} not found.");

        if (attachment.TaskId != taskId)
            throw new NotFoundException($"Attachment {id} not found.");

        return attachment;
    }
}
