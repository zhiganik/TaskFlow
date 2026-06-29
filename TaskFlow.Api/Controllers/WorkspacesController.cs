using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Authorization;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces")]
public class WorkspacesController(
    IWorkspacesService workspacesService,
    IArchiveService archiveService) : ControllerBase
{
    /// <summary>Create a new workspace.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Create(CreateWorkspaceRequest request, CancellationToken ct)
    {
        var result = await workspacesService.CreateAsync(User.GetUserId(), request, ct);
        return CreatedAtAction(nameof(GetById), new { workspaceId = result.Id }, result);
    }

    /// <summary>List workspaces the current user is a member of.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var result = await workspacesService.GetForUserAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Get a single workspace by id.</summary>
    [HttpGet("{workspaceId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid workspaceId, CancellationToken ct)
    {
        var result = await workspacesService.GetByIdAsync(workspaceId, User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Update a workspace's name.</summary>
    [HttpPut("{workspaceId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Owner)]
    [ProducesResponseType(typeof(WorkspaceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid workspaceId, UpdateWorkspaceRequest request, CancellationToken ct)
    {
        var result = await workspacesService.UpdateAsync(workspaceId, request, ct);
        return Ok(result);
    }

    /// <summary>List closed and deleted tasks in the workspace archive.</summary>
    [HttpGet("{workspaceId:guid}/archive")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<WorkspaceTaskDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetArchive(
        Guid workspaceId,
        [FromQuery] string?         search      = null,
        [FromQuery] string[]?       assigneeIds = null,
        [FromQuery] TaskPriority[]? priorities  = null,
        [FromQuery] Guid[]?         labelIds    = null,
        CancellationToken ct = default)
    {
        var filter = new TaskFilterQuery(search, assigneeIds, priorities, labelIds);
        var result = await archiveService.GetAsync(workspaceId, filter, ct);
        return Ok(result);
    }

    /// <summary>Delete a workspace.</summary>
    [HttpDelete("{workspaceId:guid}")]
    [Authorize(Policy = WorkspacePolicies.Owner)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid workspaceId, CancellationToken ct)
    {
        await workspacesService.DeleteAsync(workspaceId, ct);
        return NoContent();
    }
}
