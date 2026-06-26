namespace TaskFlow.Application.DTOs;

public record UpdateProfileRequest(string DisplayName, string Email, string AvatarColor);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
