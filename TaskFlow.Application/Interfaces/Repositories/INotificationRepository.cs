using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface INotificationRepository
{
    Task<PagedResult<Notification>> GetPagedForUserAsync(string userId, string? cursor, int limit, bool unreadOnly = false, NotificationType? type = null, CancellationToken ct = default);
    Task<int>                       GetUnreadCountAsync(string userId, CancellationToken ct = default);
    Task<Notification>              AddAsync(Notification notification, CancellationToken ct = default);
    Task<bool>                      MarkReadAsync(Guid notificationId, string userId, CancellationToken ct = default);
    Task                            MarkAllReadAsync(string userId, CancellationToken ct = default);
}
