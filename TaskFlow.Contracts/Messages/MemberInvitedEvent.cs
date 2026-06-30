namespace TaskFlow.Contracts.Messages;

public record MemberInvitedEvent(
    string RecipientId,
    string InvitedByDisplayName,
    string WorkspaceId,
    string WorkspaceName);
