using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/labels")]
public class WorkspaceLabelsController(IWorkspaceLabelsService labelsService) : ControllerBase
{
    /// <summary>List all labels for a workspace.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<LabelDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(Guid workspaceId, CancellationToken ct)
    {
        var result = await labelsService.GetByWorkspaceAsync(workspaceId, ct);
        return Ok(result);
    }

    /// <summary>Create a new label in the workspace.</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(LabelDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(Guid workspaceId, CreateLabelRequest request, CancellationToken ct)
    {
        var result = await labelsService.CreateAsync(workspaceId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { workspaceId }, result);
    }

    /// <summary>Update a label's name and color.</summary>
    [HttpPut("{labelId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(LabelDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid workspaceId, Guid labelId, UpdateLabelRequest request, CancellationToken ct)
    {
        var result = await labelsService.UpdateAsync(workspaceId, labelId, request, ct);
        return Ok(result);
    }

    /// <summary>Delete a label from the workspace.</summary>
    [HttpDelete("{labelId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid labelId, CancellationToken ct)
    {
        await labelsService.DeleteAsync(workspaceId, labelId, ct);
        return NoContent();
    }
}
