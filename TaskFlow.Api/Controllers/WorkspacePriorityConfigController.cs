using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/priority-configs")]
public class WorkspacePriorityConfigController(IWorkspacePriorityConfigService configService) : ControllerBase
{
    /// <summary>List all priority display configs for a workspace.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<PriorityConfigDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(Guid workspaceId, CancellationToken ct)
    {
        var result = await configService.GetByWorkspaceAsync(workspaceId, ct);
        return Ok(result);
    }

    /// <summary>Update the display name and color for a priority level.</summary>
    [HttpPut("{priority}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(PriorityConfigDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid workspaceId, TaskPriority priority, UpdatePriorityConfigRequest request, CancellationToken ct)
    {
        var result = await configService.UpdateAsync(workspaceId, priority, request, ct);
        return Ok(result);
    }
}
