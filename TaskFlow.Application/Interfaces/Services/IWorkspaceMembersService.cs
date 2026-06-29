using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceMembersService
{
    Task<IReadOnlyList<MemberDto>> GetMembersAsync(Guid workspaceId, CancellationToken ct);
    Task<MemberDto>                AddAsync(Guid workspaceId, InviteMemberRequest request, string invitedById, CancellationToken ct);
    Task<MemberDto>                UpdateRoleAsync(Guid workspaceId, string userId, UpdateMemberRoleRequest request, CancellationToken ct);
    Task                           RemoveAsync(Guid workspaceId, string userId, CancellationToken ct);
}
