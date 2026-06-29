using MassTransit;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Contracts.Messages;
using TaskFlow.NotificationWorker.Services;

namespace TaskFlow.NotificationWorker.Consumers;

public class TaskStatusChangedConsumer(
    INotificationDispatchService dispatch,
    ILogger<TaskStatusChangedConsumer> logger) : IConsumer<TaskStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<TaskStatusChangedEvent> context)
    {
        var msg = context.Message;

        if (string.IsNullOrEmpty(msg.AssigneeId) || msg.AssigneeId == msg.ChangedById)
            return;

        await dispatch.CreateAndPushAsync(new Notification
        {
            RecipientId = msg.AssigneeId,
            Type        = NotificationType.TaskStatusChanged,
            Title       = $"{msg.ChangedByDisplayName} updated a task status",
            Body        = $"{msg.TaskTitle}: {msg.OldStatus} → {msg.NewStatus}",
            WorkspaceId = msg.WorkspaceId,
            TaskId      = msg.TaskId
        }, context.CancellationToken);

        logger.LogInformation("TaskStatusChanged processed: {TaskId} {OldStatus}→{NewStatus}", msg.TaskId, msg.OldStatus, msg.NewStatus);
    }
}
