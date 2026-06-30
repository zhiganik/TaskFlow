using MassTransit;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Contracts.Messages;
using TaskFlow.NotificationWorker.Services;

namespace TaskFlow.NotificationWorker.Consumers;

public class MemberInvitedConsumer(
    INotificationDispatchService dispatch,
    ILogger<MemberInvitedConsumer> logger) : IConsumer<MemberInvitedEvent>
{
    public async Task Consume(ConsumeContext<MemberInvitedEvent> context)
    {
        var msg = context.Message;

        await dispatch.CreateAndPushAsync(new Notification
        {
            RecipientId = msg.RecipientId,
            Type        = NotificationType.MemberInvited,
            Title       = $"{msg.InvitedByDisplayName} added you to {msg.WorkspaceName}",
            Body        = $"You've been added to the workspace \"{msg.WorkspaceName}\"",
            WorkspaceId = Guid.Parse(msg.WorkspaceId),
        }, context.CancellationToken);

        logger.LogInformation(
            "MemberInvited processed: {RecipientId} added to workspace {WorkspaceId} by {InvitedBy}",
            msg.RecipientId, msg.WorkspaceId, msg.InvitedByDisplayName);
    }
}
