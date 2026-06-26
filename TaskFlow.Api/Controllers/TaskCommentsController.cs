using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/tasks/{taskId:guid}/comments")]
public class TaskCommentsController(ITaskCommentsService commentsService) : ControllerBase
{
    /// <summary>List comments on a task with cursor-based pagination.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(PagedResult<TaskCommentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(
        Guid workspaceId, Guid taskId,
        [FromQuery] string? cursor, [FromQuery] int limit = 20,
        CancellationToken ct = default)
    {
        var result = await commentsService.GetByTaskIdAsync(workspaceId, taskId, cursor, limit, ct);
        return Ok(result);
    }

    /// <summary>Add a comment to a task.</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(TaskCommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(
        Guid workspaceId, Guid taskId, CreateCommentRequest request, CancellationToken ct)
    {
        var result = await commentsService.CreateAsync(workspaceId, taskId, User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetAll), new { workspaceId, taskId }, result);
    }

    /// <summary>Edit a comment (author only).</summary>
    [HttpPut("{commentId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(TaskCommentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid workspaceId, Guid taskId, Guid commentId, UpdateCommentRequest request, CancellationToken ct)
    {
        var result = await commentsService.UpdateAsync(workspaceId, taskId, commentId, User.GetUserId(), request, ct);
        return Ok(result);
    }

    /// <summary>Delete a comment (author only).</summary>
    [HttpDelete("{commentId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid workspaceId, Guid taskId, Guid commentId, CancellationToken ct)
    {
        await commentsService.DeleteAsync(workspaceId, taskId, commentId, User.GetUserId(), ct);
        return NoContent();
    }
}
