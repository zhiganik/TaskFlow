using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authorization;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/members")]
public class WorkspaceMembersController(IWorkspaceMembersService membersService) : ControllerBase
{
    /// <summary>List members of a workspace.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<MemberDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(Guid workspaceId, CancellationToken ct)
    {
        var result = await membersService.GetMembersAsync(workspaceId, ct);
        return Ok(result);
    }

    /// <summary>Add an existing registered user to a workspace.</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Add(Guid workspaceId, InviteMemberRequest request, CancellationToken ct)
    {
        var result = await membersService.AddAsync(workspaceId, request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>Change a member's role.</summary>
    [HttpPut("{userId}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(MemberDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateRole(Guid workspaceId, string userId, UpdateMemberRoleRequest request, CancellationToken ct)
    {
        var result = await membersService.UpdateRoleAsync(workspaceId, userId, request, ct);
        return Ok(result);
    }

    /// <summary>Remove a member from a workspace.</summary>
    [HttpDelete("{userId}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Remove(Guid workspaceId, string userId, CancellationToken ct)
    {
        await membersService.RemoveAsync(workspaceId, userId, ct);
        return NoContent();
    }
}
