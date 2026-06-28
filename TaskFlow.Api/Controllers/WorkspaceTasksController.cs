using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/tasks")]
public class WorkspaceTasksController(IWorkspaceTasksService tasksService) : ControllerBase
{
    /// <summary>
    /// List tasks. When columnId is supplied returns a paginated page (PagedResult) for that column.
    /// Otherwise returns the full flat list filtered by search/assignee/priority.
    /// </summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(PagedResult<WorkspaceTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(
        Guid workspaceId,
        [FromQuery] Guid?           columnId,
        [FromQuery] string?         cursor,
        [FromQuery] int             limit      = 20,
        [FromQuery] string?         search      = null,
        [FromQuery] string[]?       assigneeIds = null,
        [FromQuery] TaskPriority[]? priorities  = null,
        [FromQuery] Guid[]?         labelIds    = null,
        CancellationToken ct = default)
    {
        var filter = new TaskFilterQuery(search, assigneeIds, priorities, labelIds);

        if (columnId.HasValue)
        {
            var page = await tasksService.GetPagedByColumnAsync(workspaceId, columnId.Value, filter, cursor, limit, ct);
            return Ok(page);
        }

        var result = await tasksService.GetByWorkspaceAsync(workspaceId, filter, ct);
        return Ok(result);
    }

    /// <summary>Get a single task by id.</summary>
    [HttpGet("{taskId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var result = await tasksService.GetByIdAsync(workspaceId, taskId, ct);
        return Ok(result);
    }

    /// <summary>Create a new task in a column.</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(Guid workspaceId, CreateTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.CreateAsync(workspaceId, User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { workspaceId, taskId = result.Id }, result);
    }

    /// <summary>Update a task's title, description, priority, assignee, and due date.</summary>
    [HttpPut("{taskId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid workspaceId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.UpdateAsync(workspaceId, taskId, request, ct);
        return Ok(result);
    }

    /// <summary>Move a task to a different column or position within the same column.</summary>
    [HttpPut("{taskId:guid}/move")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(Guid workspaceId, Guid taskId, MoveTaskRequest request, CancellationToken ct)
    {
        var result = await tasksService.MoveAsync(workspaceId, taskId, request, ct);
        return Ok(result);
    }

    /// <summary>Set all labels on a task (replaces existing).</summary>
    [HttpPut("{taskId:guid}/labels")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetLabels(Guid workspaceId, Guid taskId, SetTaskLabelsRequest request, CancellationToken ct)
    {
        var result = await tasksService.SetLabelsAsync(workspaceId, taskId, request, ct);
        return Ok(result);
    }

    /// <summary>Close a task (immediately moves it to the archive).</summary>
    [HttpPut("{taskId:guid}/close")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Close(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var result = await tasksService.CloseAsync(workspaceId, taskId, ct);
        return Ok(result);
    }

    /// <summary>Reopen a closed or deleted task, returning it to its column.</summary>
    [HttpPut("{taskId:guid}/reopen")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceTaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reopen(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var result = await tasksService.ReopenAsync(workspaceId, taskId, ct);
        return Ok(result);
    }

    /// <summary>Delete a task.</summary>
    [HttpDelete("{taskId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        await tasksService.DeleteAsync(workspaceId, taskId, ct);
        return NoContent();
    }
}
