namespace TaskFlow.Application.DTOs;

public record UpdateProfileRequest(string DisplayName, string Email);
public record ChangePasswordRequest(string CurrentPassword, string NewPassword);
