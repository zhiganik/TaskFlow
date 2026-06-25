using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/tasks")]
public class WorkspaceTasksController(IWorkspaceTasksService tasksService) : ControllerBase
{
    /// <summary>List all tasks in the workspace grouped by column order.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(Guid workspaceId, CancellationToken ct)
    {
        var result = await tasksService.GetByWorkspaceAsync(workspaceId, ct);
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
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Move(Guid workspaceId, Guid taskId, MoveTaskRequest request, CancellationToken ct)
    {
        await tasksService.MoveAsync(workspaceId, taskId, request, ct);
        return NoContent();
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
