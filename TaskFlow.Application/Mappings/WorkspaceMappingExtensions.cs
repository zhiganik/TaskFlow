using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public static class WorkspaceMappingExtensions
{
    public static WorkspaceDto ToDto(this Workspace workspace) =>
        new(workspace.Id, workspace.Name, workspace.OwnerId, workspace.CreatedAt);
}
