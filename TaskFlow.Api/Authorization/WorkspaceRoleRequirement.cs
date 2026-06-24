using Microsoft.AspNetCore.Authorization;
using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Api.Authorization;

public class WorkspaceRoleRequirement(WorkspaceRole minimumRole) : IAuthorizationRequirement
{
    public WorkspaceRole MinimumRole { get; } = minimumRole;
}
