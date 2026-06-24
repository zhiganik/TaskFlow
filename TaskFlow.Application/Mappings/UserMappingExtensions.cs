using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public static class UserMappingExtensions
{
    public static UserDto ToDto(this AppUser user) =>
        new(user.Id, user.Email ?? throw new InvalidOperationException("User has no email."), user.DisplayName);
}
