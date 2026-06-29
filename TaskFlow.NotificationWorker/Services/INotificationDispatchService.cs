using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.NotificationWorker.Services;

public interface INotificationDispatchService
{
    Task CreateAndPushAsync(Notification notification, CancellationToken ct = default);
}
