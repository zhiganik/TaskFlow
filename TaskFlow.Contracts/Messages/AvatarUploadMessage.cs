namespace TaskFlow.Contracts.Messages;

public record AvatarUploadMessage
{
    public string UserId        { get; init; } = string.Empty;
    public string RedisKey      { get; init; } = string.Empty;
    public string PermanentPath { get; init; } = string.Empty;
}
