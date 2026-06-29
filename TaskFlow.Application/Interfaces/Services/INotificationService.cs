using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface INotificationService
{
    Task<PagedResult<NotificationDto>> GetPagedAsync(string userId, string? cursor, int limit, bool unreadOnly = false, NotificationType? type = null, CancellationToken ct = default);
    Task<UnreadCountDto>               GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<bool>                         MarkReadAsync(Guid notificationId, string userId, CancellationToken ct = default);
    Task                               MarkAllReadAsync(string userId, CancellationToken ct = default);
}
