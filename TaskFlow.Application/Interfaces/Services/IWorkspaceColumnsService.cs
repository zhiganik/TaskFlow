using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceColumnsService
{
    Task<IReadOnlyList<WorkspaceColumnDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct);
    Task<WorkspaceColumnDto>                CreateAsync(Guid workspaceId, CreateColumnRequest request, CancellationToken ct);
    Task<WorkspaceColumnDto>                RenameAsync(Guid workspaceId, Guid columnId, UpdateColumnRequest request, CancellationToken ct);
    Task<WorkspaceColumnDto>                UpdateAsync(Guid workspaceId, Guid columnId, UpdateColumnRequest request, CancellationToken ct);
    Task                                    ReorderAsync(Guid workspaceId, ReorderColumnsRequest request, CancellationToken ct);
    Task                                    DeleteAsync(Guid workspaceId, Guid columnId, CancellationToken ct);
}
