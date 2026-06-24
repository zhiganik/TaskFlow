namespace TaskFlow.Application.DTOs;

public record WorkspaceDto(Guid Id, string Name, string OwnerId, DateTime CreatedAt);
public record CreateWorkspaceRequest(string Name);
public record UpdateWorkspaceRequest(string Name);
