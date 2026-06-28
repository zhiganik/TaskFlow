using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Constants;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/workspaces/{workspaceId:guid}/tasks/{taskId:guid}/attachments")]
public class TaskAttachmentsController(ITaskAttachmentService attachmentService) : ControllerBase
{
    /// <summary>List all attachments for a task.</summary>
    [HttpGet]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(IReadOnlyList<AttachmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var result = await attachmentService.GetByTaskIdAsync(workspaceId, taskId, ct);
        return Ok(result);
    }

    /// <summary>Get a single attachment by ID.</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid workspaceId, Guid taskId, Guid id, CancellationToken ct)
    {
        var result = await attachmentService.GetByIdAsync(workspaceId, taskId, id, ct);
        return Ok(result);
    }

    /// <summary>Upload a file to a task. File is processed asynchronously — status starts as Pending.</summary>
    [HttpPost]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(20 * 1024 * 1024)]
    [ProducesResponseType(typeof(AttachmentDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Upload(
        Guid workspaceId, Guid taskId, IFormFile file,
        [FromQuery] Guid? commentId,
        CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        var dto = await attachmentService.UploadAsync(
            workspaceId, taskId, User.GetUserId(),
            new UploadFileRequest(stream, file.FileName, file.ContentType, file.Length),
            commentId,
            ct);

        return AcceptedAtAction(nameof(GetById), new { workspaceId, taskId, id = dto.Id }, dto);
    }

    /// <summary>
    /// Download a file. Forces Content-Disposition: attachment — browser always downloads,
    /// never renders inline. Only available when Status is Ready.
    /// </summary>
    [HttpGet("{id:guid}/download")]
    [Authorize(Policy = WorkspacePolicies.Member)]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Download(Guid workspaceId, Guid taskId, Guid id, CancellationToken ct)
    {
        var (stream, fileName, contentType) =
            await attachmentService.DownloadAsync(workspaceId, taskId, id, ct);

        // fileDownloadName sets Content-Disposition: attachment — prevents inline execution
        return File(stream, contentType, fileDownloadName: fileName, enableRangeProcessing: true);
    }

    /// <summary>Delete an attachment (Admin or above).</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Policy = WorkspacePolicies.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid workspaceId, Guid taskId, Guid id, CancellationToken ct)
    {
        await attachmentService.DeleteAsync(workspaceId, taskId, id, ct);
        return NoContent();
    }
}
