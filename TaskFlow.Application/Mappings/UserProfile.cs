using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<AppUser, UserDto>()
            .ConstructUsing((src, ctx) => new UserDto(
                src.Id,
                src.Email ?? throw new InvalidOperationException("User has no email."),
                src.DisplayName,
                src.AvatarColor));
    }
}
