namespace TaskFlow.Contracts.Messages;

public record SendInvitationEmailMessage(
    string ToEmail,
    string WorkspaceName,
    string Role,
    string InviteLink);
