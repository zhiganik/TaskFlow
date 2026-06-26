using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspacePriorityConfigRepository(AppDbContext db) : IWorkspacePriorityConfigRepository
{
    public async Task<IReadOnlyList<WorkspacePriorityConfig>> GetByWorkspaceIdAsync(Guid workspaceId, CancellationToken ct = default) =>
        await db.WorkspacePriorityConfigs
            .AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderBy(c => c.Priority)
            .ToListAsync(ct);

    public async Task<WorkspacePriorityConfig?> GetByPriorityAsync(Guid workspaceId, TaskPriority priority, CancellationToken ct = default) =>
        await db.WorkspacePriorityConfigs
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.WorkspaceId == workspaceId && c.Priority == priority, ct);

    public async Task UpdateAsync(WorkspacePriorityConfig config, CancellationToken ct = default)
    {
        db.Entry(config).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }
}
