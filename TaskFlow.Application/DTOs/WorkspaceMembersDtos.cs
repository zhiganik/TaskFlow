using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record MemberDto(string UserId, string DisplayName, string Email, string AvatarColor, WorkspaceRole Role, DateTime JoinedAt);
public record InviteMemberRequest(string Email, WorkspaceRole Role);
public record UpdateMemberRoleRequest(WorkspaceRole Role);
