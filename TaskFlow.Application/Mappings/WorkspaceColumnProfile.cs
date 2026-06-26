using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class WorkspaceColumnProfile : Profile
{
    public WorkspaceColumnProfile()
    {
        CreateMap<WorkspaceColumn, WorkspaceColumnDto>()
            .ConstructUsing((src, ctx) => new WorkspaceColumnDto(
                src.Id, src.WorkspaceId, src.Name, src.Color, src.Order, src.CreatedAt));
    }
}
