using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Application.DTOs;

public record WorkspaceDto(Guid Id, string Name, string OwnerId, DateTime CreatedAt, WorkspaceRole MyRole);
public record CreateWorkspaceRequest(string Name);
public record UpdateWorkspaceRequest(string Name);
