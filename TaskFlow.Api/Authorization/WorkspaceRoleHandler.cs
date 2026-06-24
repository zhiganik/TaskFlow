using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using TaskFlow.Application.Interfaces.Repositories;

namespace TaskFlow.Api.Authorization;

public class WorkspaceRoleHandler(IWorkspaceMembersRepository membersRepository)
    : AuthorizationHandler<WorkspaceRoleRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext ctx,
        WorkspaceRoleRequirement requirement)
    {
        if (ctx.Resource is not HttpContext httpContext)
            return;

        var routeValues = httpContext.GetRouteData().Values;

        if (!routeValues.TryGetValue("workspaceId", out var wsIdObj)
            || !Guid.TryParse(wsIdObj?.ToString(), out var workspaceId))
            return;

        var userId = ctx.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId is null) return;

        // Uses composite (WorkspaceId, UserId) index — O(1) lookup
        var member = await membersRepository.GetMemberAsync(workspaceId, userId);
        if (member is null) return;

        // Role hierarchy: Owner(0) > Admin(1) > Member(2) — lower value is more privileged
        if (member.Role <= requirement.MinimumRole)
            ctx.Succeed(requirement);
    }
}
