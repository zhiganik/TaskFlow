using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class NotificationService(
    INotificationRepository repository,
    IMapper mapper,
    ILogger<NotificationService> logger) : INotificationService
{
    public async Task<PagedResult<NotificationDto>> GetPagedAsync(
        string userId, string? cursor, int limit,
        bool unreadOnly = false, NotificationType? type = null,
        CancellationToken ct = default)
    {
        var paged = await repository.GetPagedForUserAsync(userId, cursor, limit, unreadOnly, type, ct);
        var dtos  = paged.Items.Select(mapper.Map<NotificationDto>).ToList().AsReadOnly();
        return new PagedResult<NotificationDto>(dtos, paged.NextCursor, paged.HasMore);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(string userId, CancellationToken ct = default)
    {
        var count = await repository.GetUnreadCountAsync(userId, ct);
        return new UnreadCountDto(count);
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, string userId, CancellationToken ct = default)
    {
        var updated = await repository.MarkReadAsync(notificationId, userId, ct);
        if (updated)
            logger.LogInformation("Notification {NotificationId} marked as read by {UserId}", notificationId, userId);
        return updated;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default)
    {
        await repository.MarkAllReadAsync(userId, ct);
        logger.LogInformation("All notifications marked as read for {UserId}", userId);
    }
}
