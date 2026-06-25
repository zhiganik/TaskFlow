using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/columns")]
public class WorkspaceColumnsController(IWorkspaceColumnsService columnsService) : ControllerBase
{
    /// <summary>List all columns in the workspace ordered by position.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceColumnDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(Guid workspaceId, CancellationToken ct)
    {
        var result = await columnsService.GetByWorkspaceAsync(workspaceId, ct);
        return Ok(result);
    }

    /// <summary>Create a new column in the workspace (max 7).</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(WorkspaceColumnDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(Guid workspaceId, CreateColumnRequest request, CancellationToken ct)
    {
        var result = await columnsService.CreateAsync(workspaceId, request, ct);
        return CreatedAtAction(nameof(GetAll), new { workspaceId }, result);
    }

    /// <summary>Rename a column.</summary>
    [HttpPut("{columnId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(typeof(WorkspaceColumnDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Rename(Guid workspaceId, Guid columnId, UpdateColumnRequest request, CancellationToken ct)
    {
        var result = await columnsService.RenameAsync(workspaceId, columnId, request, ct);
        return Ok(result);
    }

    /// <summary>Reorder all columns by supplying the full ordered list of column IDs.</summary>
    [HttpPut("reorder")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Reorder(Guid workspaceId, ReorderColumnsRequest request, CancellationToken ct)
    {
        await columnsService.ReorderAsync(workspaceId, request, ct);
        return NoContent();
    }

    /// <summary>Delete a column.</summary>
    [HttpDelete("{columnId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid columnId, CancellationToken ct)
    {
        await columnsService.DeleteAsync(workspaceId, columnId, ct);
        return NoContent();
    }
}
