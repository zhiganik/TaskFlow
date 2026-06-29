namespace TaskFlow.Contracts.Messages;

public record TaskAssignedEvent(
    Guid TaskId,
    string TaskTitle,
    Guid ProjectId,
    Guid WorkspaceId,
    string AssignedToId,
    string AssignedById,
    string AssignedByDisplayName,
    DateTime AssignedAt);
