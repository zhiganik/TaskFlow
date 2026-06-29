using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceTasksService
{
    Task<IReadOnlyList<WorkspaceTaskDto>> GetByWorkspaceAsync(Guid workspaceId, TaskFilterQuery filter, CancellationToken ct);
    Task<PagedResult<WorkspaceTaskDto>>   GetPagedByColumnAsync(Guid workspaceId, Guid columnId, TaskFilterQuery filter, string? cursor, int limit, CancellationToken ct);
    Task<WorkspaceTaskDto>                GetByIdAsync(Guid workspaceId, Guid taskId, CancellationToken ct);
    Task<WorkspaceTaskDto>                CreateAsync(Guid workspaceId, string createdById, CreateTaskRequest request, CancellationToken ct);
    Task<WorkspaceTaskDto>                UpdateAsync(Guid workspaceId, Guid taskId, UpdateTaskRequest request, string updatedById, CancellationToken ct);
    Task<WorkspaceTaskDto>                MoveAsync(Guid workspaceId, Guid taskId, MoveTaskRequest request, string movedById, CancellationToken ct);
    Task<WorkspaceTaskDto>                SetLabelsAsync(Guid workspaceId, Guid taskId, SetTaskLabelsRequest request, CancellationToken ct);
    Task                                  DeleteAsync(Guid workspaceId, Guid taskId, string deletedById, CancellationToken ct);
    Task<WorkspaceTaskDto>                CloseAsync(Guid workspaceId, Guid taskId, string closedById, CancellationToken ct);
    Task<WorkspaceTaskDto>                ReopenAsync(Guid workspaceId, Guid taskId, string reopenedById, CancellationToken ct);
}
