using AutoMapper;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Mappings;

public class InvitationProfile : Profile
{
    public InvitationProfile()
    {
        CreateMap<WorkspaceInvitation, InvitationDto>()
            .ConstructUsing((src, _) => new InvitationDto(
                src.Id,
                src.Email,
                src.Role,
                src.Workspace.Name,
                src.InvitedBy.DisplayName,
                src.ExpiresAt));
    }
}
