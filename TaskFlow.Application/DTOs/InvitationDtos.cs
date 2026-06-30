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
