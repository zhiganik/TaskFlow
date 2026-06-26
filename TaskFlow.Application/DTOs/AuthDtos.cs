namespace TaskFlow.Application.DTOs;

public record RegisterRequest(string Email, string DisplayName, string Password);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);

public record UserDto(string UserId, string Email, string DisplayName, string AvatarColor);
public record AuthResponseDto(string AccessToken, DateTime ExpiresAt, string RefreshToken, UserDto User);
