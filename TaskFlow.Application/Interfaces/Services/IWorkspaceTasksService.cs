using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceTasksService
{
    Task<IReadOnlyList<WorkspaceTaskDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct);
    Task<WorkspaceTaskDto>                GetByIdAsync(Guid workspaceId, Guid taskId, CancellationToken ct);
    Task<WorkspaceTaskDto>                CreateAsync(Guid workspaceId, string createdById, CreateTaskRequest request, CancellationToken ct);
    Task<WorkspaceTaskDto>                UpdateAsync(Guid workspaceId, Guid taskId, UpdateTaskRequest request, CancellationToken ct);
    Task<WorkspaceTaskDto>                MoveAsync(Guid workspaceId, Guid taskId, MoveTaskRequest request, CancellationToken ct);
    Task                                  DeleteAsync(Guid workspaceId, Guid taskId, CancellationToken ct);
}
