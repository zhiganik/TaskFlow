namespace TaskFlow.Contracts.Messages;

public record TaskStatusChangedEvent(
    Guid TaskId,
    string TaskTitle,
    Guid ProjectId,
    Guid WorkspaceId,
    string ChangedById,
    string ChangedByDisplayName,
    string OldStatus,
    string NewStatus,
    string? AssigneeId,
    DateTime ChangedAt);
