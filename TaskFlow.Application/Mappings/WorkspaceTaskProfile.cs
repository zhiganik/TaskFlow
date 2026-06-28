using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class WorkspaceTaskProfile : Profile
{
    public WorkspaceTaskProfile()
    {
        CreateMap<WorkspaceTask, WorkspaceTaskDto>()
            .ConstructUsing((src, ctx) => new WorkspaceTaskDto(
                src.Id,
                src.Number,
                src.WorkspaceId,
                src.ColumnId,
                src.Column.Name,
                src.Title,
                src.Description,
                src.Order,
                src.Priority,
                src.AssigneeId,
                src.Assignee?.DisplayName,
                src.Assignee?.AvatarColor,
                src.Assignee?.AvatarPath,
                src.Assignee?.AvatarStatus.ToString(),
                src.DueDate,
                src.DueDate.HasValue && src.DueDate.Value < DateTime.UtcNow,
                src.CreatedById,
                src.CreatedBy?.DisplayName ?? src.CreatedById,
                src.CreatedAt,
                src.UpdatedAt,
                src.Labels.Select(l => new LabelDto(l.Id, l.Name, l.Color)).ToList().AsReadOnly()));
    }
}
