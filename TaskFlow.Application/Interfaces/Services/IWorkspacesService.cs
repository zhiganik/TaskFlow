using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspacesService
{
    Task<WorkspaceDto>                CreateAsync(string ownerId, CreateWorkspaceRequest request, CancellationToken ct);
    Task<IReadOnlyList<WorkspaceDto>> GetForUserAsync(string userId, CancellationToken ct);
    Task<WorkspaceDto>                GetByIdAsync(Guid id, string userId, CancellationToken ct);
    Task<WorkspaceDto>                UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken ct);
    Task                              DeleteAsync(Guid id, CancellationToken ct);
    Task<WorkspaceDto>                UpdateArchiveSettingsAsync(Guid id, string callerId, UpdateArchiveSettingsRequest request, CancellationToken ct);
}
