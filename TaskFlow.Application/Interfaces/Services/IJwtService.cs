using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Services;

public interface IJwtService
{
    string GenerateAccessToken(AppUser user);
    string GenerateRefreshToken();
}
