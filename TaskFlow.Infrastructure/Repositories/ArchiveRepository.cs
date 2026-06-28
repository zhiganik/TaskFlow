using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class ArchiveRepository(AppDbContext db) : IArchiveRepository
{
    public async Task<IReadOnlyList<WorkspaceTask>> GetByWorkspaceAsync(
        Guid workspaceId, TaskFilterQuery filter, CancellationToken ct = default) =>
        await db.WorkspaceTasks
            .AsNoTracking()
            .Include(t => t.Column)
            .Include(t => t.Assignee)
            .Include(t => t.CreatedBy)
            .Include(t => t.Labels)
            .Where(t => t.WorkspaceId == workspaceId
                     && (t.Status == WorkspaceTaskStatus.Closed || t.Status == WorkspaceTaskStatus.Deleted))
            .ApplyFilters(filter)
            .OrderByDescending(t => t.ClosedAt)
            .ToListAsync(ct);

    public async Task<int> BulkCloseExpiredDoneTasksAsync(Guid workspaceId, DateTime cutoff, CancellationToken ct = default) =>
        await db.WorkspaceTasks
            .Where(t => t.WorkspaceId == workspaceId
                     && t.Status == WorkspaceTaskStatus.Done
                     && t.CompletedAt != null
                     && t.CompletedAt <= cutoff)
            .ExecuteUpdateAsync(s => s
                .SetProperty(t => t.Status, WorkspaceTaskStatus.Closed)
                .SetProperty(t => t.ClosedAt, DateTime.UtcNow)
                .SetProperty(t => t.UpdatedAt, DateTime.UtcNow), ct);
}
