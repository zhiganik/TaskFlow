using Microsoft.AspNetCore.SignalR;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.NotificationWorker.Hubs;

namespace TaskFlow.NotificationWorker.Services;

public class NotificationDispatchService(
    INotificationRepository repository,
    IHubContext<NotificationHub> hubContext) : INotificationDispatchService
{
    public async Task CreateAndPushAsync(Notification notification, CancellationToken ct = default)
    {
        var dto = new NotificationDto(
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Body,
            notification.IsRead,
            notification.CreatedAt,
            notification.WorkspaceId,
            notification.TaskId,
            notification.CommentId);

        // Push before persisting: if SendAsync throws the consumer retries without a committed row,
        // so a retry produces at most one extra real-time push rather than a duplicate DB row.
        await hubContext.Clients.User(notification.RecipientId)
            .SendAsync("ReceiveNotification", dto, ct);

        await repository.AddAsync(notification, ct);
    }
}
