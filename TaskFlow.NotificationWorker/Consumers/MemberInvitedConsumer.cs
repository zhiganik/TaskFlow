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
            RecipientId = msg.InvitedUserId,
            Type        = NotificationType.MemberInvited,
            Title       = $"{msg.InvitedByDisplayName} invited you to a workspace",
            Body        = $"You have been added to \"{msg.WorkspaceName}\" as {msg.Role}",
            WorkspaceId = msg.WorkspaceId
        }, context.CancellationToken);

        logger.LogInformation("MemberInvited processed: {WorkspaceId} → {InvitedUserId}", msg.WorkspaceId, msg.InvitedUserId);
    }
}
