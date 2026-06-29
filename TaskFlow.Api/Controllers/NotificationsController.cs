using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Api.Extensions;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/notifications")]
public class NotificationsController(INotificationService notificationService) : ControllerBase
{
    /// <summary>Get paginated notifications for the current user (newest first).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<NotificationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? cursor,
        [FromQuery] int limit = 20,
        [FromQuery] bool unreadOnly = false,
        [FromQuery] NotificationType? type = null,
        CancellationToken ct = default)
    {
        var result = await notificationService.GetPagedAsync(User.GetUserId(), cursor, limit, unreadOnly, type, ct);
        return Ok(result);
    }

    /// <summary>Get the count of unread notifications for the current user.</summary>
    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(UnreadCountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct = default)
    {
        var result = await notificationService.GetUnreadCountAsync(User.GetUserId(), ct);
        return Ok(result);
    }

    /// <summary>Mark a single notification as read.</summary>
    [HttpPut("{notificationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkRead(Guid notificationId, CancellationToken ct = default)
    {
        var found = await notificationService.MarkReadAsync(notificationId, User.GetUserId(), ct);
        if (!found) return NotFound();
        return NoContent();
    }

    /// <summary>Mark all notifications as read for the current user.</summary>
    [HttpPut("read-all")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct = default)
    {
        await notificationService.MarkAllReadAsync(User.GetUserId(), ct);
        return NoContent();
    }
}
