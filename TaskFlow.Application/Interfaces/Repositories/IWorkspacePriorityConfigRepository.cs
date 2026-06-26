using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspacePriorityConfigRepository
{
    Task<IReadOnlyList<WorkspacePriorityConfig>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default);
    Task<WorkspacePriorityConfig?>               GetByPriorityAsync(Guid workspaceId, TaskPriority priority, CancellationToken ct = default);
    Task                                         UpdateAsync(WorkspacePriorityConfig config, CancellationToken ct = default);
}
