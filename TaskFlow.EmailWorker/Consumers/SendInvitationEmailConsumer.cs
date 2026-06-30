using FluentEmail.Core;
using MassTransit;
using Microsoft.Extensions.Logging;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.EmailWorker.Consumers;

public class SendInvitationEmailConsumer(
    IFluentEmail fluentEmail,
    ILogger<SendInvitationEmailConsumer> logger) : IConsumer<SendInvitationEmailMessage>
{
    public async Task Consume(ConsumeContext<SendInvitationEmailMessage> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var body = $"""
            <div style="font-family:sans-serif;max-width:480px;margin:0 auto">
              <h2 style="color:#3730a3">You're invited to join a workspace</h2>
              <p>You've been invited to <strong>{msg.WorkspaceName}</strong> as <strong>{msg.Role}</strong>.</p>
              <p style="margin:24px 0">
                <a href="{msg.InviteLink}"
                   style="background:#4f46e5;color:#fff;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:600">
                  Accept Invitation
                </a>
              </p>
              <p style="color:#6b7280;font-size:13px">This link expires in 7 days. If you didn't expect this invitation, you can safely ignore it.</p>
            </div>
            """;

        var result = await fluentEmail
            .To(msg.ToEmail)
            .Subject($"You're invited to {msg.WorkspaceName} on TaskFlow")
            .Body(body, isHtml: true)
            .SendAsync(ct);

        if (!result.Successful)
            logger.LogWarning("Failed to send invitation email to {Email}: {Errors}",
                msg.ToEmail, string.Join(", ", result.ErrorMessages));
        else
            logger.LogInformation("Invitation email sent to {Email} for workspace {WorkspaceName}",
                msg.ToEmail, msg.WorkspaceName);
    }
}
