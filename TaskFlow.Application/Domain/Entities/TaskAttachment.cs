using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Domain.Entities;

public class TaskAttachment
{
    public Guid             Id               { get; set; } = Guid.NewGuid();
    public Guid             TaskId           { get; set; }
    public string           UploadedById     { get; set; } = string.Empty;
    public string           OriginalFileName { get; set; } = string.Empty;
    public string           ContentType      { get; set; } = string.Empty;
    public long             FileSizeBytes    { get; set; }
    public string           StoredFileName   { get; set; } = string.Empty;
    public string           StoragePath      { get; set; } = string.Empty;
    public AttachmentStatus Status           { get; set; } = AttachmentStatus.Pending;
    public string?          ProcessingError  { get; set; }
    public DateTime         UploadedAt       { get; set; } = DateTime.UtcNow;
    public DateTime?        ProcessedAt      { get; set; }

    public Guid?         CommentId  { get; set; }

    public WorkspaceTask  Task      { get; set; } = null!;
    public AppUser        UploadedBy { get; set; } = null!;
    public TaskComment?   Comment   { get; set; }
}
