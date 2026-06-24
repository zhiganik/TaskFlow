using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspaceMembersRepository
{
    Task<WorkspaceMember?>               GetMemberAsync(Guid workspaceId, string userId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceMember>> GetMembershipsForUserAsync(string userId, CancellationToken ct = default);
    Task<WorkspaceMember>                AddAsync(WorkspaceMember member, CancellationToken ct = default);
    Task                                 UpdateAsync(WorkspaceMember member, CancellationToken ct = default);
    Task<bool>                           DeleteAsync(Guid workspaceId, string userId, CancellationToken ct = default);
}
