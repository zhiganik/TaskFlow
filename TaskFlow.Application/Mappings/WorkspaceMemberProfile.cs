using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class WorkspaceMemberProfile : Profile
{
    public WorkspaceMemberProfile()
    {
        CreateMap<WorkspaceMember, MemberDto>()
            .ConstructUsing((src, ctx) => new MemberDto(
                src.UserId,
                src.User.DisplayName,
                src.User.Email ?? throw new InvalidOperationException("User has no email."),
                src.User.AvatarColor,
                src.User.AvatarPath,
                src.User.AvatarStatus.ToString(),
                src.Role,
                src.JoinedAt));
    }
}
