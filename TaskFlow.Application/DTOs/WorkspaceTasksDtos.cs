using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record WorkspaceTaskDto(
    Guid                    Id,
    int                     Number,
    Guid                    WorkspaceId,
    Guid                    ColumnId,
    string                  ColumnName,
    string                  Title,
    string?                 Description,
    int                     Order,
    TaskPriority            Priority,
    string?                 AssigneeId,
    string?                 AssigneeName,
    string?                 AssigneeAvatarColor,
    DateTime?               DueDate,
    bool                    IsOverdue,
    string                  CreatedById,
    string                  CreatedByName,
    DateTime                CreatedAt,
    DateTime                UpdatedAt,
    IReadOnlyList<LabelDto> Labels
);

public record CreateTaskRequest(
    string                 Title,
    Guid                   ColumnId,
    string?                Description  = null,
    TaskPriority           Priority     = TaskPriority.Medium,
    string?                AssigneeId   = null,
    DateTime?              DueDate      = null,
    IReadOnlyList<Guid>?   LabelIds     = null
);

public record UpdateTaskRequest(
    string       Title,
    string?      Description,
    TaskPriority Priority,
    string?      AssigneeId,
    DateTime?    DueDate
);

public record MoveTaskRequest(Guid ColumnId);
