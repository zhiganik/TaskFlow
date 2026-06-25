using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public static class WorkspaceTaskMappingExtensions
{
    public static WorkspaceTaskDto ToDto(this WorkspaceTask t) =>
        new(t.Id, t.Number, t.WorkspaceId, t.ColumnId, t.Column.Name,
            t.Title, t.Description, t.Order, t.Priority,
            t.AssigneeId, t.Assignee?.DisplayName,
            t.DueDate,
            t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow,
            t.CreatedById, t.CreatedBy?.DisplayName ?? t.CreatedById,
            t.CreatedAt, t.UpdatedAt);
}
