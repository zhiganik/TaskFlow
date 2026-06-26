using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class WorkspaceLabelProfile : Profile
{
    public WorkspaceLabelProfile()
    {
        CreateMap<WorkspaceLabel, LabelDto>()
            .ConstructUsing(src => new LabelDto(src.Id, src.Name, src.Color));
    }
}
