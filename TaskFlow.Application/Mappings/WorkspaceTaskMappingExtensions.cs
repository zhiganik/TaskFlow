using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public static class WorkspaceTaskMappingExtensions
{
    public static WorkspaceTaskDto ToDto(this WorkspaceTask t) =>
        new(t.Id, t.WorkspaceId, t.ColumnId, t.Column.Name,
            t.Title, t.Description, t.Order, t.Priority,
            t.AssigneeId, t.Assignee?.DisplayName,
            t.DueDate, t.CreatedById, t.CreatedAt);
}
