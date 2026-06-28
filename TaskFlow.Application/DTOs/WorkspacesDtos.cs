using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record WorkspaceDto(Guid Id, string Name, string OwnerId, DateTime CreatedAt, WorkspaceRole MyRole, int ArchiveAfterDays);
public record CreateWorkspaceRequest(string Name);
public record UpdateWorkspaceRequest(string Name);
public record UpdateArchiveSettingsRequest(int ArchiveAfterDays);
