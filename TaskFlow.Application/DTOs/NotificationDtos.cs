using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record NotificationDto(
    Guid             Id,
    NotificationType Type,
    string           Title,
    string           Body,
    bool             IsRead,
    DateTime         CreatedAt,
    Guid?            WorkspaceId,
    Guid?            TaskId,
    Guid?            CommentId);

public record UnreadCountDto(int Count);
