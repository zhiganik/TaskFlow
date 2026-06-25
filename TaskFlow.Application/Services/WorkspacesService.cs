using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Mappings;

namespace TaskFlow.Application.Services;

public class WorkspacesService(
    IWorkspacesRepository repository,
    IWorkspaceMembersRepository membersRepository,
    ILogger<WorkspacesService> logger) : IWorkspacesService
{
    public async Task<WorkspaceDto> CreateAsync(string ownerId, CreateWorkspaceRequest request, CancellationToken ct)
    {
        var workspace = new Workspace
        {
            Name = request.Name,
            OwnerId = ownerId
        };

        workspace.Members.Add(new WorkspaceMember
        {
            UserId = ownerId,
            Role = WorkspaceRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        workspace.Columns.Add(new WorkspaceColumn { Name = "Todo",        Color = "#6366F1", Order = 0 });
        workspace.Columns.Add(new WorkspaceColumn { Name = "In Progress", Color = "#F59E0B", Order = 1 });
        workspace.Columns.Add(new WorkspaceColumn { Name = "Done",        Color = "#10B981", Order = 2 });

        await repository.AddAsync(workspace, ct);

        logger.LogInformation("Workspace {WorkspaceId} created by {OwnerId}", workspace.Id, ownerId);

        return workspace.ToDto(WorkspaceRole.Owner);
    }

    public async Task<IReadOnlyList<WorkspaceDto>> GetForUserAsync(string userId, CancellationToken ct)
    {
        var memberships = await membersRepository.GetMembershipsForUserAsync(userId, ct);
        return memberships.Select(m => m.Workspace.ToDto(m.Role)).ToList();
    }

    public async Task<WorkspaceDto> GetByIdAsync(Guid id, string userId, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        // The WorkspaceMember policy already guarantees a membership row exists at this point.
        var member = await membersRepository.GetMemberAsync(id, userId, ct)
            ?? throw new ForbiddenException("You do not have access to this workspace.");

        return workspace.ToDto(member.Role);
    }

    public async Task<WorkspaceDto> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        workspace.Name = request.Name;
        await repository.UpdateAsync(workspace, ct);

        logger.LogInformation("Workspace {WorkspaceId} updated by {OwnerId}", workspace.Id, workspace.OwnerId);

        // Only the Owner can reach this action (WorkspaceOwner policy).
        return workspace.ToDto(WorkspaceRole.Owner);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        await repository.DeleteAsync(id, ct);

        logger.LogInformation("Workspace {WorkspaceId} deleted by {OwnerId}", id, workspace.OwnerId);
    }
}
