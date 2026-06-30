using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspaceInvitationRepository
{
    Task<WorkspaceInvitation?>              GetByTokenAsync(string token, CancellationToken ct = default);
    Task<WorkspaceInvitation?>              GetByWorkspaceAndEmailAsync(Guid workspaceId, string email, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceInvitation>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceInvitation>               AddAsync(WorkspaceInvitation invitation, CancellationToken ct = default);
    Task                                    DeleteAsync(Guid id, CancellationToken ct = default);
}
