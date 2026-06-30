using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record MemberDto(string UserId, string DisplayName, string Email, string AvatarColor, string? AvatarPath, string AvatarStatus, WorkspaceRole Role, DateTime JoinedAt);
public record UpdateMemberRoleRequest(WorkspaceRole Role);
