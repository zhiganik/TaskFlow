using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceInvitationService
{
    Task<InvitationDto>              CreateAsync(Guid workspaceId, CreateInvitationRequest request, string invitedById, CancellationToken ct = default);
    Task<IReadOnlyList<InvitationDto>> ListAsync(Guid workspaceId, CancellationToken ct = default);
    Task                             CancelAsync(Guid workspaceId, Guid invitationId, CancellationToken ct = default);
    Task<InvitationInfoDto>          GetInfoAsync(string token, CancellationToken ct = default);
    Task<AcceptInvitationResultDto>  AcceptAsync(string token, AcceptInvitationRequest request, CancellationToken ct = default);
}
