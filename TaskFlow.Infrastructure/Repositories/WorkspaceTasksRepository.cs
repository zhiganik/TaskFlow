using System.Text;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspaceTasksRepository(AppDbContext db) : IWorkspaceTasksRepository
{
    public async Task<IReadOnlyList<WorkspaceTask>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await db.WorkspaceTasks
            .AsNoTracking()
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Where(t => t.WorkspaceId == workspaceId)
            .OrderBy(t => t.ColumnId)
            .ThenBy(t => t.Order)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkspaceTask>> GetByWorkspaceIdAsync(
        Guid workspaceId, TaskFilterQuery filter, CancellationToken ct = default)
    {
        var q = db.WorkspaceTasks
            .AsNoTracking()
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Where(t => t.WorkspaceId == workspaceId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            if (int.TryParse(s, out var num))
                q = q.Where(t => t.Number == num || t.Title.ToLower().Contains(s.ToLower()));
            else
                q = q.Where(t => t.Title.ToLower().Contains(s.ToLower()));
        }

        if (filter.AssigneeId == "unassigned")
            q = q.Where(t => t.AssigneeId == null);
        else if (!string.IsNullOrEmpty(filter.AssigneeId))
            q = q.Where(t => t.AssigneeId == filter.AssigneeId);

        if (filter.Priorities is { Count: > 0 })
            q = q.Where(t => filter.Priorities.Contains(t.Priority));

        return await q
            .OrderBy(t => t.ColumnId)
            .ThenBy(t => t.Order)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<WorkspaceTask>> GetByColumnIdAsync(Guid columnId, CancellationToken ct = default) =>
        await db.WorkspaceTasks
            .AsNoTracking()
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Where(t => t.ColumnId == columnId)
            .OrderBy(t => t.Order)
            .ToListAsync(ct);

    public async Task<WorkspaceTask?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.WorkspaceTasks
            .AsNoTracking()
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task<int> CountByColumnIdAsync(Guid columnId, CancellationToken ct = default) =>
        await db.WorkspaceTasks.CountAsync(t => t.ColumnId == columnId, ct);

    public async Task<int> GetNextNumberAsync(Guid workspaceId, CancellationToken ct = default) =>
        (await db.WorkspaceTasks
            .Where(t => t.WorkspaceId == workspaceId)
            .MaxAsync(t => (int?)t.Number, ct) ?? 0) + 1;

    public async Task<WorkspaceTask> AddAsync(WorkspaceTask task, CancellationToken ct = default)
    {
        db.WorkspaceTasks.Add(task);
        await db.SaveChangesAsync(ct);
        return task;
    }

    public async Task UpdateAsync(WorkspaceTask task, CancellationToken ct = default)
    {
        db.WorkspaceTasks.Update(task);
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<WorkspaceTask> tasks, CancellationToken ct = default)
    {
        db.WorkspaceTasks.UpdateRange(tasks);
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<WorkspaceTask>> GetPagedByColumnAsync(
        Guid workspaceId, Guid columnId, TaskFilterQuery filter, string? cursor, int limit, CancellationToken ct = default)
    {
        var q = db.WorkspaceTasks
            .AsNoTracking()
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Where(t => t.WorkspaceId == workspaceId && t.ColumnId == columnId);

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var s = filter.Search.Trim();
            if (int.TryParse(s, out var num))
                q = q.Where(t => t.Number == num || t.Title.ToLower().Contains(s.ToLower()));
            else
                q = q.Where(t => t.Title.ToLower().Contains(s.ToLower()));
        }

        if (filter.AssigneeId == "unassigned")
            q = q.Where(t => t.AssigneeId == null);
        else if (!string.IsNullOrEmpty(filter.AssigneeId))
            q = q.Where(t => t.AssigneeId == filter.AssigneeId);

        if (filter.Priorities is { Count: > 0 })
            q = q.Where(t => filter.Priorities.Contains(t.Priority));

        if (cursor is not null)
        {
            var (curOrder, curId) = DecodeCursor(cursor);
            q = q.Where(t => t.Order > curOrder || (t.Order == curOrder && t.Id.CompareTo(curId) > 0));
        }

        var items = await q
            .OrderBy(t => t.Order)
            .ThenBy(t => t.Id)
            .Take(limit + 1)
            .ToListAsync(ct);

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        var nextCursor = hasMore ? EncodeCursor(items[^1].Order, items[^1].Id) : null;
        return new PagedResult<WorkspaceTask>(items, nextCursor, hasMore);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var task = await db.WorkspaceTasks.FindAsync([id], ct);
        if (task is null) return false;

        db.WorkspaceTasks.Remove(task);
        await db.SaveChangesAsync(ct);
        return true;
    }

    private static string EncodeCursor(int order, Guid id) =>
        Convert.ToBase64String(Encoding.UTF8.GetBytes($"{order}_{id}"));

    private static (int Order, Guid Id) DecodeCursor(string cursor)
    {
        var raw = Encoding.UTF8.GetString(Convert.FromBase64String(cursor));
        var sep = raw.LastIndexOf('_');
        return (int.Parse(raw[..sep]), Guid.Parse(raw[(sep + 1)..]));
    }
}
