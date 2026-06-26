using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Application.Options;

namespace TaskFlow.Application.Services;

public class AuthService(
    UserManager<AppUser> userManager,
    IJwtService jwtService,
    ICacheService cache,
    IOptions<JwtOptions> jwtOptions,
    IMapper mapper,
    ILogger<AuthService> logger) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var user = new AppUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            throw new ConflictException(string.Join(' ', result.Errors.Select(e => e.Description)));

        logger.LogInformation("User {UserId} registered with email {Email}", user.Id, user.Email);

        return mapper.Map<UserDto>(user);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await userManager.FindByEmailAsync(request.Email);

        if (user is null || await userManager.IsLockedOutAsync(user))
            throw new UnauthorizedException("Invalid email or password.");

        if (!await userManager.CheckPasswordAsync(user, request.Password))
        {
            await userManager.AccessFailedAsync(user);
            logger.LogWarning("Failed login attempt for {Email}", request.Email);
            throw new UnauthorizedException("Invalid email or password.");
        }

        await userManager.ResetAccessFailedCountAsync(user);

        logger.LogInformation("User {UserId} logged in", user.Id);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponseDto> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default)
    {
        var userId = await cache.GetAsync<string>(RefreshKey(request.RefreshToken), ct: ct)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        await cache.InvalidateAsync(RefreshKey(request.RefreshToken), ct);

        var user = await userManager.FindByIdAsync(userId)
            ?? throw new UnauthorizedException("Invalid or expired refresh token.");

        logger.LogInformation("Refreshed tokens for user {UserId}", user.Id);

        return await IssueTokensAsync(user, ct);
    }

    public async Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default)
        => await cache.InvalidateAsync(RefreshKey(request.RefreshToken), ct);

    private async Task<AuthResponseDto> IssueTokensAsync(AppUser user, CancellationToken ct)
    {
        var accessToken = jwtService.GenerateAccessToken(user);
        var refreshToken = jwtService.GenerateRefreshToken();

        await cache.SetAsync(
            RefreshKey(refreshToken), user.Id,
            TimeSpan.FromDays(_jwt.RefreshTokenExpiryDays), ct);

        return new AuthResponseDto(
            accessToken,
            DateTime.UtcNow.AddMinutes(_jwt.AccessTokenExpiryMinutes),
            refreshToken,
            mapper.Map<UserDto>(user));
    }

    private static string RefreshKey(string token) => $"refresh-token:{token}";
}
