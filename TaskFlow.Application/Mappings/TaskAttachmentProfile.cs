using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class TaskAttachmentProfile : Profile
{
    public TaskAttachmentProfile()
    {
        CreateMap<TaskAttachment, AttachmentDto>()
            .ConstructUsing((src, _) => new AttachmentDto(
                src.Id,
                src.TaskId,
                src.OriginalFileName,
                src.ContentType,
                src.FileSizeBytes,
                src.Status,
                src.ProcessingError,
                src.UploadedById,
                src.UploadedBy.DisplayName,
                src.UploadedBy.AvatarColor,
                src.UploadedAt,
                src.ProcessedAt));
    }
}
