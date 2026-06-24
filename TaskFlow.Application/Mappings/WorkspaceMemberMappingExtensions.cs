using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public static class WorkspaceMemberMappingExtensions
{
    public static MemberDto ToDto(this WorkspaceMember member, AppUser user) =>
        new(
            member.UserId,
            user.DisplayName,
            user.Email ?? throw new InvalidOperationException("User has no email."),
            member.Role,
            member.JoinedAt);
}
