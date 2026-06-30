namespace TaskFlow.Contracts.Messages;

public record MemberRemovedEvent(
    string RecipientId,
    string RemovedByDisplayName,
    string WorkspaceId,
    string WorkspaceName);
