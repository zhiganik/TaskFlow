using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Caching;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.Application.Services;

public class WorkspaceInvitationService(
    IWorkspaceInvitationRepository invitationRepository,
    IWorkspacesRepository workspacesRepository,
    IWorkspaceMembersRepository membersRepository,
    UserManager<AppUser> userManager,
    IMessagePublisher publisher,
    IAuthService authService,
    ICacheService cache,
    IOptions<AppOptions> appOptions,
    ILogger<WorkspaceInvitationService> logger) : IWorkspaceInvitationService
{
    private readonly AppOptions _app = appOptions.Value;

    public async Task<CreateInvitationResponseDto> CreateAsync(Guid workspaceId, CreateInvitationRequest request, string invitedById, CancellationToken ct = default)
    {
        var email = request.Email.ToLowerInvariant();

        var workspace = await workspacesRepository.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} was not found.");

        var invitedBy = await userManager.FindByIdAsync(invitedById)
            ?? throw new NotFoundException($"User {invitedById} was not found.");

        var existingUser = await userManager.FindByEmailAsync(email);

        // ── Path A: user already has an account → add directly, no email ────────
        if (existingUser is not null)
        {
            if (await membersRepository.GetMemberAsync(workspaceId, existingUser.Id, ct) is not null)
                throw new ConflictException("This user is already a member of the workspace.");

            var member = new WorkspaceMember
            {
                WorkspaceId = workspaceId,
                UserId      = existingUser.Id,
                Role        = request.Role,
                JoinedAt    = DateTime.UtcNow,
            };

            await membersRepository.AddAsync(member, ct);

            await cache.InvalidateManyAsync(
            [
                CacheKeys.WorkspaceMembers(workspaceId),
                CacheKeys.UserWorkspaces(existingUser.Id),
            ], ct);

            await publisher.PublishAsync(new MemberInvitedEvent(
                existingUser.Id,
                invitedBy.DisplayName,
                workspaceId.ToString(),
                workspace.Name), ct);

            logger.LogInformation(
                "Existing user {UserId} directly added to workspace {WorkspaceId} by {InvitedById}",
                existingUser.Id, workspaceId, invitedById);

            return new CreateInvitationResponseDto(
                DirectlyAdded:    true,
                Invitation:       null,
                AddedUserId:      existingUser.Id,
                AddedDisplayName: existingUser.DisplayName);
        }

        // ── Path B: no account yet → email invitation ────────────────────────────
        var pending = await invitationRepository.GetByWorkspaceAndEmailAsync(workspaceId, email, ct);
        if (pending is not null)
            throw new ConflictException("An invitation for this email address is already pending.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

        var invitation = new WorkspaceInvitation
        {
            Id          = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email       = email,
            Role        = request.Role,
            Token       = token,
            InvitedById = invitedById,
            CreatedAt   = DateTime.UtcNow,
            ExpiresAt   = DateTime.UtcNow.AddDays(7),
        };

        await invitationRepository.AddAsync(invitation, ct);

        var link = $"{_app.FrontendBaseUrl.TrimEnd('/')}/invite/{token}";
        await publisher.PublishAsync(new SendInvitationEmailMessage(
            email, workspace.Name, request.Role.ToString(), link), ct);

        logger.LogInformation("Invitation email queued for {Email} in workspace {WorkspaceId} by {InvitedById}",
            email, workspaceId, invitedById);

        return new CreateInvitationResponseDto(
            DirectlyAdded:    false,
            Invitation:       new InvitationDto(invitation.Id, email, request.Role, workspace.Name, invitedBy.DisplayName, invitation.ExpiresAt),
            AddedUserId:      null,
            AddedDisplayName: null);
    }

    public async Task<IReadOnlyList<InvitationDto>> ListAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var workspace = await workspacesRepository.GetByIdAsync(workspaceId, ct)
            ?? throw new NotFoundException($"Workspace {workspaceId} was not found.");

        var invitations = await invitationRepository.GetByWorkspaceAsync(workspaceId, ct);
        return invitations
            .Select(i => new InvitationDto(
                i.Id,
                i.Email,
                i.Role,
                workspace.Name,
                i.InvitedBy.DisplayName,
                i.ExpiresAt))
            .ToList();
    }

    public async Task CancelAsync(Guid workspaceId, Guid invitationId, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.GetByWorkspaceAsync(workspaceId, ct);
        var target = invitation.FirstOrDefault(i => i.Id == invitationId)
            ?? throw new NotFoundException($"Invitation {invitationId} was not found.");

        await invitationRepository.DeleteAsync(target.Id, ct);

        logger.LogInformation("Invitation {InvitationId} for workspace {WorkspaceId} cancelled", invitationId, workspaceId);
    }

    public async Task<InvitationInfoDto> GetInfoAsync(string token, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token, ct)
            ?? throw new NotFoundException("Invitation not found or has expired.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new NotFoundException("Invitation not found or has expired.");

        var userExists = await userManager.FindByEmailAsync(invitation.Email) is not null;

        return new InvitationInfoDto(invitation.Email, invitation.Workspace.Name, invitation.Role, userExists);
    }

    public async Task<AcceptInvitationResultDto> AcceptAsync(string token, AcceptInvitationRequest request, CancellationToken ct = default)
    {
        var invitation = await invitationRepository.GetByTokenAsync(token, ct)
            ?? throw new NotFoundException("Invitation not found or has expired.");

        if (invitation.ExpiresAt < DateTime.UtcNow)
            throw new NotFoundException("Invitation not found or has expired.");

        var user = await userManager.FindByEmailAsync(invitation.Email);

        if (user is null)
        {
            if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password))
                throw new BadRequestException("Display name and password are required to create a new account.");

            user = new AppUser
            {
                UserName    = invitation.Email,
                Email       = invitation.Email,
                DisplayName = request.DisplayName,
                CreatedAt   = DateTime.UtcNow,
            };

            var result = await userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
                throw new ConflictException(string.Join(' ', result.Errors.Select(e => e.Description)));

            logger.LogInformation("New user {UserId} created via invitation for workspace {WorkspaceId}",
                user.Id, invitation.WorkspaceId);
        }

        if (await membersRepository.GetMemberAsync(invitation.WorkspaceId, user.Id, ct) is not null)
        {
            await invitationRepository.DeleteAsync(invitation.Id, ct);
            return new AcceptInvitationResultDto(invitation.WorkspaceId, null);
        }

        var member = new WorkspaceMember
        {
            WorkspaceId = invitation.WorkspaceId,
            UserId      = user.Id,
            Role        = invitation.Role,
            JoinedAt    = DateTime.UtcNow,
        };

        await membersRepository.AddAsync(member, ct);

        await cache.InvalidateManyAsync(
        [
            CacheKeys.WorkspaceMembers(invitation.WorkspaceId),
            CacheKeys.UserWorkspaces(user.Id),
        ], ct);

        await invitationRepository.DeleteAsync(invitation.Id, ct);

        logger.LogInformation("User {UserId} joined workspace {WorkspaceId} via invitation", user.Id, invitation.WorkspaceId);

        var wasNewUser = string.IsNullOrWhiteSpace(request.DisplayName) is false && request.Password is not null;
        var auth = wasNewUser ? await authService.IssueTokensForUserAsync(user, ct) : null;

        return new AcceptInvitationResultDto(invitation.WorkspaceId, auth);
    }
}
