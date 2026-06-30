using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;
using TaskFlow.Application.Domain.Enums;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspaceColumnsRepository(AppDbContext db) : IWorkspaceColumnsRepository
{
    public async Task<IReadOnlyList<WorkspaceColumn>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await db.WorkspaceColumns
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.Order)
            .ToListAsync(ct);

    public async Task<WorkspaceColumn?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.WorkspaceColumns
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task<int> CountByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await db.WorkspaceColumns
            .CountAsync(c => c.WorkspaceId == workspaceId, ct);

    public async Task<WorkspaceColumn> AddAsync(WorkspaceColumn column, CancellationToken ct = default)
    {
        db.WorkspaceColumns.Add(column);
        await db.SaveChangesAsync(ct);
        return column;
    }

    public async Task UpdateAsync(WorkspaceColumn column, CancellationToken ct = default)
    {
        db.Entry(column).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task UpdateRangeAsync(IEnumerable<WorkspaceColumn> columns, CancellationToken ct = default)
    {
        foreach (var column in columns)
            db.Entry(column).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task ClearDoneColumnAsync(Guid workspaceId, Guid exceptColumnId, CancellationToken ct = default) =>
        await db.WorkspaceColumns
            .Where(c => c.WorkspaceId == workspaceId && c.Id != exceptColumnId && c.IsDoneColumn)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDoneColumn, false), ct);

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var column = await db.WorkspaceColumns.FindAsync([id], ct);
        if (column is null) return false;

        db.WorkspaceColumns.Remove(column);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
