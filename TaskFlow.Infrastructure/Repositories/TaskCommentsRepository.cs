using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskCommentsRepository(AppDbContext db) : ITaskCommentsRepository
{
    public async Task<PagedResult<TaskComment>> GetByTaskIdAsync(
        Guid taskId, string? cursor, int limit, CancellationToken ct = default)
    {
        var query = db.TaskComments
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Include(c => c.Mentions)
            .Include(c => c.Attachments).ThenInclude(a => a.UploadedBy)
            .Where(c => c.TaskId == taskId);

        if (cursor is not null)
        {
            var (cursorDt, cursorId) = DecodeCursor(cursor);
            query = query.Where(c =>
                c.CreatedAt > cursorDt ||
                (c.CreatedAt == cursorDt && c.Id.CompareTo(cursorId) > 0));
        }

        var items = await query
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore ? EncodeCursor(items[^1].CreatedAt, items[^1].Id) : null;

        return new PagedResult<TaskComment>(items.AsReadOnly(), nextCursor, hasMore);
    }

    public async Task<TaskComment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.TaskComments
            .AsNoTracking()
            .Include(c => c.CreatedBy)
            .Include(c => c.Mentions)
            .Include(c => c.Attachments).ThenInclude(a => a.UploadedBy)
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<TaskComment> AddAsync(TaskComment comment, CancellationToken ct = default)
    {
        db.TaskComments.Add(comment);
        await db.SaveChangesAsync(ct);
        return comment;
    }

    public async Task UpdateAsync(TaskComment comment, CancellationToken ct = default)
    {
        db.TaskComments.Update(comment);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var comment = await db.TaskComments.FindAsync([id], ct);
        if (comment is null) return;

        db.TaskComments.Remove(comment);
        await db.SaveChangesAsync(ct);
    }

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
