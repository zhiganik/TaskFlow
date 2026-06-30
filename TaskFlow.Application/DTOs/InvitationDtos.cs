using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record CreateInvitationRequest(string Email, WorkspaceRole Role);

public record InvitationDto(
    Guid         Id,
    string       Email,
    WorkspaceRole Role,
    string       WorkspaceName,
    string       InvitedByName,
    DateTime     ExpiresAt);

public record InvitationInfoDto(
    string       Email,
    string       WorkspaceName,
    WorkspaceRole Role,
    bool         UserExists);

public record AcceptInvitationRequest(string? DisplayName, string? Password);

public record AcceptInvitationResultDto(Guid WorkspaceId, AuthResponseDto? Auth);

/// <summary>
/// Returned by POST /workspaces/{id}/invitations.
/// DirectlyAdded=true means an existing user was added immediately (no email sent).
/// DirectlyAdded=false means an email invitation was queued.
/// </summary>
public record CreateInvitationResponseDto(
    bool          DirectlyAdded,
    InvitationDto? Invitation,
    string?        AddedUserId,
    string?        AddedDisplayName);
