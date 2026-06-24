using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Mappings;

namespace TaskFlow.Application.Services;

public class WorkspaceMembersService(
    IWorkspaceMembersRepository membersRepository,
    IWorkspacesRepository workspacesRepository,
    UserManager<AppUser> userManager,
    ILogger<WorkspaceMembersService> logger) : IWorkspaceMembersService
{
    public async Task<IReadOnlyList<MemberDto>> GetMembersAsync(Guid workspaceId, CancellationToken ct)
    {
        var members = await membersRepository.GetMembersAsync(workspaceId, ct);
        return members.Select(m => m.ToDto(m.User)).ToList();
    }

    public async Task<MemberDto> AddAsync(Guid workspaceId, InviteMemberRequest request, CancellationToken ct)
    {
        if (await workspacesRepository.GetByIdAsync(workspaceId, ct) is null)
            throw new NotFoundException($"Workspace {workspaceId} was not found.");

        var user = await userManager.FindByEmailAsync(request.Email)
            ?? throw new NotFoundException($"No user with email {request.Email} exists.");

        if (await membersRepository.GetMemberAsync(workspaceId, user.Id, ct) is not null)
            throw new ConflictException("User is already a member of this workspace.");

        var member = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = user.Id,
            Role = request.Role,
            JoinedAt = DateTime.UtcNow
        };

        await membersRepository.AddAsync(member, ct);

        logger.LogInformation("User {UserId} added to workspace {WorkspaceId} with role {Role}",
            user.Id, workspaceId, request.Role);

        return member.ToDto(user);
    }

    public async Task<MemberDto> UpdateRoleAsync(Guid workspaceId, string userId, UpdateMemberRoleRequest request, CancellationToken ct)
    {
        var member = await membersRepository.GetMemberAsync(workspaceId, userId, ct)
            ?? throw new NotFoundException($"User {userId} is not a member of workspace {workspaceId}.");

        if (member.Role == WorkspaceRole.Owner)
            throw new ConflictException("The workspace owner's role cannot be changed.");

        member.Role = request.Role;
        await membersRepository.UpdateAsync(member, ct);

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} was not found.");

        logger.LogInformation("Role for user {UserId} in workspace {WorkspaceId} changed to {Role}",
            userId, workspaceId, request.Role);

        return member.ToDto(user);
    }

    public async Task RemoveAsync(Guid workspaceId, string userId, CancellationToken ct)
    {
        var member = await membersRepository.GetMemberAsync(workspaceId, userId, ct)
            ?? throw new NotFoundException($"User {userId} is not a member of workspace {workspaceId}.");

        if (member.Role == WorkspaceRole.Owner)
            throw new ConflictException("The workspace owner cannot be removed from the workspace.");

        await membersRepository.DeleteAsync(workspaceId, userId, ct);

        logger.LogInformation("User {UserId} removed from workspace {WorkspaceId}", userId, workspaceId);
    }
}
