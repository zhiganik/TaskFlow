using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspaceLabelsRepository
{
    Task<IReadOnlyList<WorkspaceLabel>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceLabel?>               GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<WorkspaceLabel>                AddAsync(WorkspaceLabel label, CancellationToken ct = default);
    Task                                UpdateAsync(WorkspaceLabel label, CancellationToken ct = default);
    Task<bool>                          DeleteAsync(Guid id, CancellationToken ct = default);
}
