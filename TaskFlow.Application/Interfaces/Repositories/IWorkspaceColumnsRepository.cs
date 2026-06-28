using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspaceColumnsRepository
{
    Task<IReadOnlyList<WorkspaceColumn>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceColumn?>               GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int>                            CountByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspaceColumn>                AddAsync(WorkspaceColumn column, CancellationToken ct = default);
    Task                                 UpdateAsync(WorkspaceColumn column, CancellationToken ct = default);
    Task                                 UpdateRangeAsync(IEnumerable<WorkspaceColumn> columns, CancellationToken ct = default);
    Task                                 ClearDoneColumnAsync(Guid workspaceId, Guid exceptColumnId, CancellationToken ct = default);
    Task<bool>                           DeleteAsync(Guid id, CancellationToken ct = default);
}
