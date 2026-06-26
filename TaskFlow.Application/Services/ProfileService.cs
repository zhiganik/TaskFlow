using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class ProfileService(
    UserManager<AppUser> userManager,
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

            user.Email           = request.Email;
            user.NormalizedEmail = userManager.NormalizeEmail(request.Email);
            user.UserName        = request.Email;
            user.NormalizedUserName = userManager.NormalizeName(request.Email);
        }

        user.DisplayName = request.DisplayName;
        user.AvatarColor = request.AvatarColor;

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
}
