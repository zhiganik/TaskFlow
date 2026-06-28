using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class WorkspaceProfile : Profile
{
    public WorkspaceProfile()
    {
        CreateMap<Workspace, WorkspaceDto>()
            .ConstructUsing((src, ctx) => new WorkspaceDto(
                src.Id,
                src.Name,
                src.OwnerId,
                src.CreatedAt,
                (WorkspaceRole)ctx.Items["myRole"],
                src.ArchiveAfterDays));
    }
}
