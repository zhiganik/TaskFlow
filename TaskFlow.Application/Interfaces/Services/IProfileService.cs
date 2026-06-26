using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IProfileService
{
    Task<UserDto> GetAsync(string userId, CancellationToken ct = default);
    Task<UserDto> UpdateAsync(string userId, UpdateProfileRequest request, CancellationToken ct = default);
    Task ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken ct = default);
}
