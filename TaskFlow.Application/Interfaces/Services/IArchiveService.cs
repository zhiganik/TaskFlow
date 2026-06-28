using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IArchiveService
{
    Task<IReadOnlyList<WorkspaceTaskDto>> GetAsync(Guid workspaceId, TaskFilterQuery filter, CancellationToken ct);
}
