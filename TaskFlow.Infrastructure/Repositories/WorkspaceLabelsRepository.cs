using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspaceLabelsRepository(AppDbContext db) : IWorkspaceLabelsRepository
{
    public async Task<IReadOnlyList<WorkspaceLabel>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await db.WorkspaceLabels
            .AsNoTracking()
            .Where(l => l.WorkspaceId == workspaceId)
            .OrderBy(l => l.Name)
            .ToListAsync(ct);

    public async Task<WorkspaceLabel?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.WorkspaceLabels
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task<WorkspaceLabel> AddAsync(WorkspaceLabel label, CancellationToken ct = default)
    {
        db.WorkspaceLabels.Add(label);
        await db.SaveChangesAsync(ct);
        return label;
    }

    public async Task UpdateAsync(WorkspaceLabel label, CancellationToken ct = default)
    {
        db.Entry(label).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var label = await db.WorkspaceLabels.FindAsync([id], ct);
        if (label is null) return false;

        db.WorkspaceLabels.Remove(label);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
