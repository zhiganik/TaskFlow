using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspacesRepository(AppDbContext db) : IWorkspacesRepository
{
    public async Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Workspaces
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, ct);

    public async Task<Workspace> AddAsync(Workspace workspace, CancellationToken ct = default)
    {
        db.Workspaces.Add(workspace);
        await db.SaveChangesAsync(ct);
        return workspace;
    }

    public async Task UpdateAsync(Workspace workspace, CancellationToken ct = default)
    {
        db.Entry(workspace).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var workspace = await db.Workspaces.FindAsync([id], ct);
        if (workspace is null) return false;

        db.Workspaces.Remove(workspace);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<IReadOnlyList<Workspace>> GetAllAsync(CancellationToken ct = default) =>
        await db.Workspaces.AsNoTracking().ToListAsync(ct);
}
