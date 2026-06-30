using MassTransit;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Contracts.Messages;
using TaskFlow.NotificationWorker.Services;

namespace TaskFlow.NotificationWorker.Consumers;

public class MemberRemovedConsumer(
    INotificationDispatchService dispatch,
    ILogger<MemberRemovedConsumer> logger) : IConsumer<MemberRemovedEvent>
{
    public async Task Consume(ConsumeContext<MemberRemovedEvent> context)
    {
        var msg = context.Message;

        await dispatch.CreateAndPushAsync(new Notification
        {
            RecipientId = msg.RecipientId,
            Type        = NotificationType.MemberRemoved,
            Title       = $"You were removed from {msg.WorkspaceName}",
            Body        = $"{msg.RemovedByDisplayName} removed you from the workspace \"{msg.WorkspaceName}\"",
            WorkspaceId = Guid.Parse(msg.WorkspaceId),
        }, context.CancellationToken);

        logger.LogInformation(
            "MemberRemoved processed: {RecipientId} removed from workspace {WorkspaceId} by {RemovedBy}",
            msg.RecipientId, msg.WorkspaceId, msg.RemovedByDisplayName);
    }
}
