using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Mappings;

namespace TaskFlow.Application.Services;

public class WorkspacesService(
    IWorkspacesRepository repository,
    ILogger<WorkspacesService> logger) : IWorkspacesService
{
    public async Task<WorkspaceDto> CreateAsync(string ownerId, CreateWorkspaceRequest request, CancellationToken ct)
    {
        var workspace = new Workspace
        {
            Name = request.Name,
            OwnerId = ownerId
        };

        await repository.AddAsync(workspace, ct);

        logger.LogInformation("Workspace {WorkspaceId} created by {OwnerId}", workspace.Id, ownerId);

        return workspace.ToDto();
    }

    public async Task<IReadOnlyList<WorkspaceDto>> GetForUserAsync(string userId, CancellationToken ct)
    {
        var workspaces = await repository.GetByOwnerIdAsync(userId, ct);
        return workspaces.Select(w => w.ToDto()).ToList();
    }

    public async Task<WorkspaceDto> GetByIdAsync(Guid id, string userId, CancellationToken ct)
    {
        var workspace = await GetOwnedWorkspaceAsync(id, userId, ct);
        return workspace.ToDto();
    }

    public async Task<WorkspaceDto> UpdateAsync(Guid id, string userId, UpdateWorkspaceRequest request, CancellationToken ct)
    {
        var workspace = await GetOwnedWorkspaceAsync(id, userId, ct);
        workspace.Name = request.Name;

        await repository.UpdateAsync(workspace, ct);

        logger.LogInformation("Workspace {WorkspaceId} updated by {OwnerId}", workspace.Id, userId);

        return workspace.ToDto();
    }

    public async Task DeleteAsync(Guid id, string userId, CancellationToken ct)
    {
        await GetOwnedWorkspaceAsync(id, userId, ct);
        await repository.DeleteAsync(id, ct);

        logger.LogInformation("Workspace {WorkspaceId} deleted by {OwnerId}", id, userId);
    }

    private async Task<Workspace> GetOwnedWorkspaceAsync(Guid id, string userId, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        if (workspace.OwnerId != userId)
            throw new ForbiddenException("You do not have access to this workspace.");

        return workspace;
    }
}
