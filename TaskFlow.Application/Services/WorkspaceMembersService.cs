using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Caching;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.Application.Services;

public class WorkspaceMembersService(
    IWorkspaceMembersRepository membersRepository,
    IWorkspacesRepository workspacesRepository,
    ICacheService cache,
    UserManager<AppUser> userManager,
    IMessagePublisher publisher,
    IMapper mapper,
    ILogger<WorkspaceMembersService> logger) : IWorkspaceMembersService
{
    public async Task<IReadOnlyList<MemberDto>> GetMembersAsync(Guid workspaceId, CancellationToken ct)
    {
        var key    = CacheKeys.WorkspaceMembers(workspaceId);
        var cached = await cache.GetAsync<List<MemberDto>>(key, CacheKeys.Category.Members, ct);
        if (cached is not null) return cached;

        var members = await membersRepository.GetMembersAsync(workspaceId, ct);
        var dtos    = mapper.Map<List<MemberDto>>(members);

        await cache.SetAsync(key, dtos, CacheKeys.Ttl.Members, ct);
        return dtos;
    }

    public async Task<MemberDto> UpdateRoleAsync(Guid workspaceId, string userId, UpdateMemberRoleRequest request, string changedById, CancellationToken ct)
    {
        var member = await membersRepository.GetMemberAsync(workspaceId, userId, ct)
            ?? throw new NotFoundException($"User {userId} is not a member of workspace {workspaceId}.");

        if (member.Role == WorkspaceRole.Owner)
            throw new ConflictException("The workspace owner's role cannot be changed.");

        member.Role = request.Role;
        await membersRepository.UpdateAsync(member, ct);

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} was not found.");

        member.User = user;

        await cache.InvalidateAsync(CacheKeys.WorkspaceMembers(workspaceId), ct);

        var workspace = await workspacesRepository.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} was not found.");

        var changedBy = await userManager.FindByIdAsync(changedById);

        await publisher.PublishAsync(new MemberRoleChangedEvent(
            userId,
            changedBy?.DisplayName ?? "An admin",
            workspaceId.ToString(),
            workspace.Name,
            request.Role.ToString()), ct);

        logger.LogInformation("Role for user {UserId} in workspace {WorkspaceId} changed to {Role} by {ChangedById}",
            userId, workspaceId, request.Role, changedById);

        return mapper.Map<MemberDto>(member);
    }

    public async Task RemoveAsync(Guid workspaceId, string userId, string removedById, CancellationToken ct)
    {
        var member = await membersRepository.GetMemberAsync(workspaceId, userId, ct)
            ?? throw new NotFoundException($"User {userId} is not a member of workspace {workspaceId}.");

        if (member.Role == WorkspaceRole.Owner)
            throw new ConflictException("The workspace owner cannot be removed from the workspace.");

        var workspace = await workspacesRepository.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} was not found.");

        var removedBy = await userManager.FindByIdAsync(removedById);

        await membersRepository.DeleteAsync(workspaceId, userId, ct);

        await cache.InvalidateManyAsync(
        [
            CacheKeys.WorkspaceMembers(workspaceId),
            CacheKeys.UserWorkspaces(userId),
        ], ct);

        await publisher.PublishAsync(new MemberRemovedEvent(
            userId,
            removedBy?.DisplayName ?? "An admin",
            workspaceId.ToString(),
            workspace.Name), ct);

        logger.LogInformation("User {UserId} removed from workspace {WorkspaceId} by {RemovedById}",
            userId, workspaceId, removedById);
    }
}
