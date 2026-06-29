namespace TaskFlow.Contracts.Messages;

public record MemberInvitedEvent(
    Guid WorkspaceId,
    string WorkspaceName,
    string InvitedUserId,
    string InvitedByDisplayName,
    string Role,
    DateTime InvitedAt);
