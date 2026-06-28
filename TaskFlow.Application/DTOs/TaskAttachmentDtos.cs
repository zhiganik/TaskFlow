using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record UploadFileRequest(
    Stream Stream,
    string FileName,
    string ContentType,
    long   SizeBytes);

public record AttachmentDto(
    Guid             Id,
    Guid             TaskId,
    string           OriginalFileName,
    string           ContentType,
    long             FileSizeBytes,
    AttachmentStatus Status,
    string?          ProcessingError,
    string           UploadedById,
    string           UploadedByName,
    string           UploadedByAvatarColor,
    DateTime         UploadedAt,
    DateTime?        ProcessedAt);
