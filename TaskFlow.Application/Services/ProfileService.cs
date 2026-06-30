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

public class ProfileService(
    UserManager<AppUser> userManager,
    IBlobService blobService,
    ITemporaryFileStore tempStore,
    IMessagePublisher publisher,
    IWorkspaceMembersRepository membersRepository,
    ICacheService cache,
    IMapper mapper,
    ILogger<ProfileService> logger) : IProfileService
{
    public async Task<UserDto> GetAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        if (!string.Equals(user.Email, request.Email, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await userManager.FindByEmailAsync(request.Email);
            if (existing is not null)
                throw new ConflictException("Email is already in use.");

            user.Email              = request.Email;
            user.NormalizedEmail    = userManager.NormalizeEmail(request.Email);
            user.UserName           = request.Email;
            user.NormalizedUserName = userManager.NormalizeName(request.Email);
        }

        user.DisplayName = request.DisplayName;

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        logger.LogInformation("User {UserId} updated profile", userId);

        return mapper.Map<UserDto>(user);
    }

    public async Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        var result = await userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
        if (!result.Succeeded)
            throw new BadRequestException(result.Errors.First().Description);

        logger.LogInformation("User {UserId} changed password", userId);
    }

    public async Task<UserDto> UploadAvatarAsync(string userId, UploadFileRequest file, CancellationToken ct = default)
    {
        if (!file.ContentType.StartsWith("image/"))
            throw new BadRequestException("Only image files are allowed for avatars.");

        if (file.SizeBytes > 10_485_760)
            throw new BadRequestException("Avatar image must be smaller than 10 MB.");

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        if (user.AvatarPath is not null && user.AvatarStatus == AvatarStatus.Ready)
            await blobService.DeleteAsync($"avatars/{user.AvatarPath}", ct);

        var storedFileName = $"{Guid.NewGuid()}.jpg";
        var redisKey       = CacheKeys.TempAvatar(userId);

        await tempStore.StoreAsync(redisKey, file.Stream, CacheKeys.Ttl.TempAvatar, ct);

        user.AvatarPath   = storedFileName;
        user.AvatarStatus = AvatarStatus.Pending;

        var identityResult = await userManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
            throw new BadRequestException(identityResult.Errors.First().Description);

        await publisher.PublishAsync(new AvatarUploadMessage
        {
            UserId        = userId,
            RedisKey      = redisKey,
            PermanentPath = $"avatars/{storedFileName}",
        }, ct);

        await BustMemberCachesAsync(userId, ct);

        logger.LogInformation("User {UserId} queued avatar upload", userId);
        return mapper.Map<UserDto>(user);
    }

    public async Task<UserDto> RemoveAvatarAsync(string userId, CancellationToken ct = default)
    {
        var user = await userManager.FindByIdAsync(userId)
            ?? throw new NotFoundException($"User {userId} not found.");

        if (user.AvatarPath is not null && user.AvatarStatus == AvatarStatus.Ready)
            await blobService.DeleteAsync($"avatars/{user.AvatarPath}", ct);

        user.AvatarPath   = null;
        user.AvatarStatus = AvatarStatus.None;

        var identityResult = await userManager.UpdateAsync(user);
        if (!identityResult.Succeeded)
            throw new BadRequestException(identityResult.Errors.First().Description);

        await BustMemberCachesAsync(userId, ct);

        logger.LogInformation("User {UserId} removed avatar", userId);
        return mapper.Map<UserDto>(user);
    }

    private async Task BustMemberCachesAsync(string userId, CancellationToken ct)
    {
        var memberships = await membersRepository.GetMembershipsForUserAsync(userId, ct);
        if (memberships.Count == 0) return;
        var keys = memberships.Select(m => CacheKeys.WorkspaceMembers(m.WorkspaceId));
        await cache.InvalidateManyAsync(keys, ct);
    }
}
