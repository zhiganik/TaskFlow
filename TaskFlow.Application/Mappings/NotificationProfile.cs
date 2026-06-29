using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class NotificationProfile : Profile
{
    public NotificationProfile()
    {
        CreateMap<Notification, NotificationDto>()
            .ConstructUsing(src => new NotificationDto(
                src.Id,
                src.Type,
                src.Title,
                src.Body,
                src.IsRead,
                src.CreatedAt,
                src.WorkspaceId,
                src.TaskId,
                src.CommentId));
    }
}
