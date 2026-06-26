using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class TaskCommentProfile : Profile
{
    public TaskCommentProfile()
    {
        CreateMap<TaskComment, TaskCommentDto>()
            .ConstructUsing((src, ctx) => new TaskCommentDto(
                src.Id,
                src.TaskId,
                src.Content,
                src.CreatedById,
                src.CreatedBy.DisplayName,
                src.CreatedAt,
                src.UpdatedAt,
                src.UpdatedAt > src.CreatedAt.AddSeconds(1),
                src.Mentions.Select(m => m.UserId).ToList().AsReadOnly()));
    }
}
