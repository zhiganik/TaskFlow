namespace TaskFlow.Contracts.Messages;

public record MemberRoleChangedEvent(
    string RecipientId,
    string ChangedByDisplayName,
    string WorkspaceId,
    string WorkspaceName,
    string NewRole);
