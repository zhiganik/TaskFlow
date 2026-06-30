using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IAuthService
{
    Task<UserDto> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthResponseDto> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponseDto> RefreshAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task LogoutAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<AuthResponseDto> IssueTokensForUserAsync(AppUser user, CancellationToken ct = default);
}
