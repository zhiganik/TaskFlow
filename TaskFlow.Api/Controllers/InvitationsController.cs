using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/invitations")]
public class InvitationsController(IWorkspaceInvitationService invitationService) : ControllerBase
{
    /// <summary>Get invitation details by token (public).</summary>
    [HttpGet("{token}")]
    [ProducesResponseType(typeof(InvitationInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetInfo(string token, CancellationToken ct)
    {
        var result = await invitationService.GetInfoAsync(token, ct);
        return Ok(result);
    }

    /// <summary>Accept an invitation. Provide DisplayName+Password when creating a new account.</summary>
    [HttpPost("{token}/accept")]
    [ProducesResponseType(typeof(AcceptInvitationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Accept(string token, AcceptInvitationRequest request, CancellationToken ct)
    {
        var result = await invitationService.AcceptAsync(token, request, ct);
        return Ok(result);
    }
}
