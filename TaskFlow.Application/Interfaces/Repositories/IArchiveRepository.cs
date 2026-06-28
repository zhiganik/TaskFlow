using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IArchiveRepository
{
    Task<IReadOnlyList<WorkspaceTask>> GetByWorkspaceAsync(Guid workspaceId, TaskFilterQuery filter, CancellationToken ct = default);
    Task<int>                          BulkCloseExpiredDoneTasksAsync(Guid workspaceId, DateTime cutoff, CancellationToken ct = default);
}
