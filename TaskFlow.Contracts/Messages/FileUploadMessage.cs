namespace TaskFlow.Contracts.Messages;

public record FileUploadMessage
{
    public Guid           AttachmentId     { get; init; }
    public string         RedisKey         { get; init; } = string.Empty;
    public string         PermanentPath    { get; init; } = string.Empty;
    public string         StoredFileName   { get; init; } = string.Empty;
    public string         OriginalFileName { get; init; } = string.Empty;
    public string         ContentType      { get; init; } = string.Empty;
    public long           FileSizeBytes    { get; init; }
    public Guid           WorkspaceId      { get; init; }
    public Guid           TaskId           { get; init; }
    public string         UploadedByUserId { get; init; } = string.Empty;
    public DateTimeOffset UploadedAt       { get; init; }
}
