using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Caching;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class WorkspacesService(
    IWorkspacesRepository repository,
    IWorkspaceMembersRepository membersRepository,
    ICacheService cache,
    IMapper mapper,
    ILogger<WorkspacesService> logger) : IWorkspacesService
{
    public async Task<WorkspaceDto> CreateAsync(string ownerId, CreateWorkspaceRequest request, CancellationToken ct)
    {
        var workspace = new Workspace { Name = request.Name, OwnerId = ownerId };

        workspace.Members.Add(new WorkspaceMember
        {
            UserId = ownerId,
            Role   = WorkspaceRole.Owner,
            JoinedAt = DateTime.UtcNow
        });

        foreach (var col in WorkspaceDefaults.Columns)
            workspace.Columns.Add(col);

        foreach (var cfg in WorkspaceDefaults.PriorityConfigs)
            workspace.PriorityConfigs.Add(cfg);

        foreach (var label in WorkspaceDefaults.Labels)
            workspace.Labels.Add(label);

        await repository.AddAsync(workspace, ct);

        await cache.InvalidateAsync(CacheKeys.UserWorkspaces(ownerId), ct);

        logger.LogInformation("Workspace {WorkspaceId} created by {OwnerId}", workspace.Id, ownerId);

        return mapper.Map<WorkspaceDto>(workspace, opts => opts.Items["myRole"] = WorkspaceRole.Owner);
    }

    public async Task<IReadOnlyList<WorkspaceDto>> GetForUserAsync(string userId, CancellationToken ct)
    {
        var key    = CacheKeys.UserWorkspaces(userId);
        var cached = await cache.GetAsync<List<WorkspaceDto>>(key, CacheKeys.Category.UserWorkspaces, ct);
        if (cached is not null) return cached;

        var memberships = await membersRepository.GetMembershipsForUserAsync(userId, ct);
        var dtos = memberships
            .Select(m => mapper.Map<WorkspaceDto>(m.Workspace, opts => opts.Items["myRole"] = m.Role))
            .ToList();

        await cache.SetAsync(key, dtos, CacheKeys.Ttl.UserWorkspaces, ct);
        return dtos;
    }

    public async Task<WorkspaceDto> GetByIdAsync(Guid id, string userId, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        var member = await membersRepository.GetMemberAsync(id, userId, ct)
            ?? throw new ForbiddenException("You do not have access to this workspace.");

        return mapper.Map<WorkspaceDto>(workspace, opts => opts.Items["myRole"] = member.Role);
    }

    public async Task<WorkspaceDto> UpdateAsync(Guid id, UpdateWorkspaceRequest request, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        workspace.Name = request.Name;
        await repository.UpdateAsync(workspace, ct);

        await cache.InvalidateAsync(CacheKeys.UserWorkspaces(workspace.OwnerId), ct);

        logger.LogInformation("Workspace {WorkspaceId} renamed", workspace.Id);

        return mapper.Map<WorkspaceDto>(workspace, opts => opts.Items["myRole"] = WorkspaceRole.Owner);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct)
    {
        var workspace = await repository.GetByIdAsync(id, ct)
            ?? throw new NotFoundException($"Workspace {id} was not found.");

        await repository.DeleteAsync(id, ct);

        await cache.InvalidateAsync(CacheKeys.UserWorkspaces(workspace.OwnerId), ct);

        logger.LogInformation("Workspace {WorkspaceId} deleted", id);
    }
}
