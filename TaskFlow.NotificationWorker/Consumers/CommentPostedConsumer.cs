using System.Text.RegularExpressions;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Contracts.Messages;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.NotificationWorker.Services;

namespace TaskFlow.NotificationWorker.Consumers;

public partial class CommentPostedConsumer(
    AppDbContext db,
    INotificationDispatchService dispatch,
    ILogger<CommentPostedConsumer> logger) : IConsumer<CommentPostedEvent>
{
    [GeneratedRegex(@"@\[[^\]]+\]\(([^)]+)\)")]
    private static partial Regex MentionRegex();

    public async Task Consume(ConsumeContext<CommentPostedEvent> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var mentions = MentionRegex()
            .Matches(msg.Content)
            .Select(m => new TaskCommentMention { CommentId = msg.CommentId, UserId = m.Groups[1].Value })
            .DistinctBy(m => m.UserId)
            .ToList();

        if (mentions.Count > 0)
        {
            // Skip rows that already exist so re-delivered messages don't hit the composite PK constraint.
            var existingUserIds = await db.Set<TaskCommentMention>()
                .Where(m => m.CommentId == msg.CommentId)
                .Select(m => m.UserId)
                .ToListAsync(ct);

            var toInsert = mentions.Where(m => !existingUserIds.Contains(m.UserId)).ToList();
            if (toInsert.Count > 0)
            {
                db.Set<TaskCommentMention>().AddRange(toInsert);
                await db.SaveChangesAsync(ct);
            }
        }

        foreach (var mention in mentions.Where(m => m.UserId != msg.AuthorId))
        {
            await dispatch.CreateAndPushAsync(new Notification
            {
                RecipientId = mention.UserId,
                Type        = NotificationType.MentionedInComment,
                Title       = $"{msg.AuthorDisplayName} mentioned you",
                Body        = TruncateBody(msg.Content),
                WorkspaceId = msg.WorkspaceId == Guid.Empty ? null : msg.WorkspaceId,
                TaskId      = msg.TaskId,
                CommentId   = msg.CommentId
            }, ct);
        }

        logger.LogInformation("CommentPosted processed: {CommentId}, {MentionCount} mentions", msg.CommentId, mentions.Count);
    }

    private static string TruncateBody(string content) =>
        content.Length <= 120 ? content : string.Concat(content.AsSpan(0, 117), "...");
}
