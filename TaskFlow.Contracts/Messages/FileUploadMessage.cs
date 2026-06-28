namespace TaskFlow.Contracts.Messages;

public record FileUploadMessage
{
    public Guid FileId { get; init; }
    public string FileName { get; init; } = string.Empty;
    public string ContentType { get; init; } = string.Empty;
    public long FileSizeBytes { get; init; }
    public Guid WorkspaceId { get; init; }
    public Guid TaskId { get; init; }
    public Guid UploadedByUserId { get; init; }
    public DateTimeOffset UploadedAt { get; init; }
}
