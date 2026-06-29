using MassTransit;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Contracts.Messages;
using TaskFlow.NotificationWorker.Services;

namespace TaskFlow.NotificationWorker.Consumers;

public class TaskAssignedConsumer(
    INotificationDispatchService dispatch,
    ILogger<TaskAssignedConsumer> logger) : IConsumer<TaskAssignedEvent>
{
    public async Task Consume(ConsumeContext<TaskAssignedEvent> context)
    {
        var msg = context.Message;

        if (msg.AssignedToId == msg.AssignedById)
            return;

        await dispatch.CreateAndPushAsync(new Notification
        {
            RecipientId = msg.AssignedToId,
            Type        = NotificationType.TaskAssigned,
            Title       = $"{msg.AssignedByDisplayName} assigned you a task",
            Body        = msg.TaskTitle,
            WorkspaceId = msg.WorkspaceId,
            TaskId      = msg.TaskId
        }, context.CancellationToken);

        logger.LogInformation("TaskAssigned processed: {TaskId} → {AssignedToId}", msg.TaskId, msg.AssignedToId);
    }
}
