using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class NotificationRepository(AppDbContext db) : INotificationRepository
{
    public async Task<PagedResult<Notification>> GetPagedForUserAsync(
        string userId, string? cursor, int limit,
        bool unreadOnly = false, NotificationType? type = null,
        CancellationToken ct = default)
    {
        var query = db.Notifications
            .AsNoTracking()
            .Where(n => n.RecipientId == userId);

        if (unreadOnly)
            query = query.Where(n => !n.IsRead);

        if (type is not null)
            query = query.Where(n => n.Type == type);

        if (cursor is not null)
        {
            var (cursorDt, cursorId) = DecodeCursor(cursor);
            query = query.Where(n =>
                n.CreatedAt < cursorDt ||
                (n.CreatedAt == cursorDt && n.Id.CompareTo(cursorId) < 0));
        }

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .ThenByDescending(n => n.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore ? EncodeCursor(items[^1].CreatedAt, items[^1].Id) : null;

        return new PagedResult<Notification>(items.AsReadOnly(), nextCursor, hasMore);
    }

    public async Task<int> GetUnreadCountAsync(string userId, CancellationToken ct = default) =>
        await db.Notifications
            .AsNoTracking()
            .CountAsync(n => n.RecipientId == userId && !n.IsRead, ct);

    public async Task<Notification> AddAsync(Notification notification, CancellationToken ct = default)
    {
        db.Notifications.Add(notification);
        await db.SaveChangesAsync(ct);
        return notification;
    }

    public async Task<bool> MarkReadAsync(Guid notificationId, string userId, CancellationToken ct = default)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == userId, ct);

        if (notification is null) return false;

        notification.IsRead = true;
        db.Entry(notification).State = Microsoft.EntityFrameworkCore.EntityState.Modified;
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task MarkAllReadAsync(string userId, CancellationToken ct = default) =>
        await db.Notifications
            .Where(n => n.RecipientId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

    private static string EncodeCursor(DateTime createdAt, Guid id)
    {
        var raw = $"{createdAt.Ticks}_{id}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private static (DateTime createdAt, Guid id) DecodeCursor(string cursor)
    {
        var raw   = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var parts = raw.Split('_', 2);
        return (new DateTime(long.Parse(parts[0]), DateTimeKind.Utc), Guid.Parse(parts[1]));
    }
}
