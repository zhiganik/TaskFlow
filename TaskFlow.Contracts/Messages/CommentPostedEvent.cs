namespace TaskFlow.Contracts.Messages;

public record CommentPostedEvent(
    Guid CommentId,
    Guid TaskId,
    Guid ProjectId,
    Guid WorkspaceId,
    string AuthorId,
    string AuthorDisplayName,
    string Content,
    DateTime PostedAt);
