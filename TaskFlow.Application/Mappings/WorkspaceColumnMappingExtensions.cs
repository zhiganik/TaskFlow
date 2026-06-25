using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public static class WorkspaceColumnMappingExtensions
{
    public static WorkspaceColumnDto ToDto(this WorkspaceColumn column) =>
        new(column.Id, column.WorkspaceId, column.Name, column.Color, column.Order, column.CreatedAt);
}
