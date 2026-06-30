using MassTransit;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Contracts.Messages;
using TaskFlow.NotificationWorker.Services;

namespace TaskFlow.NotificationWorker.Consumers;

public class MemberRoleChangedConsumer(
    INotificationDispatchService dispatch,
    ILogger<MemberRoleChangedConsumer> logger) : IConsumer<MemberRoleChangedEvent>
{
    public async Task Consume(ConsumeContext<MemberRoleChangedEvent> context)
    {
        var msg = context.Message;

        await dispatch.CreateAndPushAsync(new Notification
        {
            RecipientId = msg.RecipientId,
            Type        = NotificationType.MemberRoleChanged,
            Title       = $"Your role in {msg.WorkspaceName} changed to {msg.NewRole}",
            Body        = $"{msg.ChangedByDisplayName} changed your role to {msg.NewRole} in \"{msg.WorkspaceName}\"",
            WorkspaceId = Guid.Parse(msg.WorkspaceId),
        }, context.CancellationToken);

        logger.LogInformation(
            "MemberRoleChanged processed: {RecipientId} role changed to {NewRole} in workspace {WorkspaceId} by {ChangedBy}",
            msg.RecipientId, msg.NewRole, msg.WorkspaceId, msg.ChangedByDisplayName);
    }
}
