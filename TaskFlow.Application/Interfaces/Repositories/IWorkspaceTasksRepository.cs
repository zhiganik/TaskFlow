using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspaceTasksRepository
{
    Task<IReadOnlyList<WorkspaceTask>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceTask>> GetByColumnIdAsync(Guid columnId, CancellationToken ct = default);
    Task<WorkspaceTask?>               GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<int>                          CountByColumnIdAsync(Guid columnId, CancellationToken ct = default);
    Task<WorkspaceTask>                AddAsync(WorkspaceTask task, CancellationToken ct = default);
    Task                               UpdateAsync(WorkspaceTask task, CancellationToken ct = default);
    Task                               UpdateRangeAsync(IEnumerable<WorkspaceTask> tasks, CancellationToken ct = default);
    Task<bool>                         DeleteAsync(Guid id, CancellationToken ct = default);
}
