using MassTransit;
using Microsoft.EntityFrameworkCore;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using StackExchange.Redis;
using TaskFlow.Application.Caching;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Contracts.Messages;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.AvatarWorker.Consumers;

public class AvatarUploadConsumer(
    ITemporaryFileStore tempStore,
    IBlobService blobService,
    AppDbContext db,
    IConnectionMultiplexer redis,
    ILogger<AvatarUploadConsumer> logger) : IConsumer<AvatarUploadMessage>
{
    public async Task Consume(ConsumeContext<AvatarUploadMessage> context)
    {
        var msg = context.Message;
        var ct  = context.CancellationToken;

        var user = await db.Users.FindAsync([msg.UserId], ct);
        if (user is null)
        {
            logger.LogWarning("Avatar worker: user {UserId} not found", msg.UserId);
            return;
        }

        try
        {
            var bytes = await tempStore.GetAsync(msg.RedisKey, ct);
            if (bytes is null)
            {
                logger.LogWarning("Avatar temp data expired for user {UserId}", msg.UserId);
                user.AvatarStatus = AvatarStatus.Failed;
                db.Entry(user).State = EntityState.Modified;
                await db.SaveChangesAsync(ct);
                return;
            }

            using var input = new MemoryStream(bytes);
            using var image = await Image.LoadAsync(input, ct);

            var side = Math.Min(image.Width, image.Height);
            image.Mutate(x => x
                .Crop(new Rectangle((image.Width - side) / 2, (image.Height - side) / 2, side, side))
                .Resize(256, 256));

            using var output = new MemoryStream();
            await image.SaveAsJpegAsync(output, new JpegEncoder { Quality = 85 }, ct);
            output.Position = 0;

            await blobService.SaveAsync(output, msg.PermanentPath, ct);
            await tempStore.DeleteAsync(msg.RedisKey, ct);

            user.AvatarStatus = AvatarStatus.Ready;
            db.Entry(user).State = EntityState.Modified;
            await db.SaveChangesAsync(ct);

            await BustMemberCachesAsync(msg.UserId, ct);

            logger.LogInformation("Avatar processed for user {UserId} → {Path}", msg.UserId, msg.PermanentPath);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Avatar processing failed for user {UserId}", msg.UserId);
            user.AvatarStatus = AvatarStatus.Failed;
            db.Entry(user).State = EntityState.Modified;
            try { await db.SaveChangesAsync(ct); } catch { /* best effort */ }
        }
    }

    private async Task BustMemberCachesAsync(string userId, CancellationToken ct)
    {
        var workspaceIds = await db.WorkspaceMembers
            .Where(m => m.UserId == userId)
            .Select(m => m.WorkspaceId)
            .ToListAsync(ct);

        if (workspaceIds.Count == 0) return;

        var redisDb = redis.GetDatabase();
        var keys = workspaceIds.Select(id => (RedisKey)CacheKeys.WorkspaceMembers(id)).ToArray();
        await redisDb.KeyDeleteAsync(keys);
    }
}
