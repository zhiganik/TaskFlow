namespace TaskFlow.Application.DTOs;

public record TaskCommentDto(
    Guid                  Id,
    Guid                  TaskId,
    string                Content,
    string                CreatedById,
    string                CreatedByName,
    string                CreatedByAvatarColor,
    DateTime              CreatedAt,
    DateTime              UpdatedAt,
    bool                  IsEdited,
    IReadOnlyList<string> MentionedUserIds);

public record CreateCommentRequest(string Content);
public record UpdateCommentRequest(string Content);
