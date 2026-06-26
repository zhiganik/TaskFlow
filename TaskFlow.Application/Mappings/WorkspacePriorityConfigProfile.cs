using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class WorkspacePriorityConfigProfile : Profile
{
    public WorkspacePriorityConfigProfile()
    {
        CreateMap<WorkspacePriorityConfig, PriorityConfigDto>()
            .ConstructUsing(src => new PriorityConfigDto(src.Priority, src.DisplayName, src.Color));
    }
}
