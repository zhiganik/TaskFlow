using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/invitations")]
public class WorkspaceInvitationsController(IWorkspaceInvitationService invitationService) : ControllerBase
{
    /// <summary>Send an invitation email to join a workspace.</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(InvitationDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid workspaceId, CreateInvitationRequest request, CancellationToken ct)
    {
        var result = await invitationService.CreateAsync(workspaceId, request, User.GetUserId(), ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>List pending invitations for a workspace.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(IReadOnlyList<InvitationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> List(Guid workspaceId, CancellationToken ct)
    {
        var result = await invitationService.ListAsync(workspaceId, ct);
        return Ok(result);
    }

    /// <summary>Cancel a pending invitation.</summary>
    [HttpDelete("{invitationId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid workspaceId, Guid invitationId, CancellationToken ct)
    {
        await invitationService.CancelAsync(workspaceId, invitationId, ct);
        return NoContent();
    }
}
