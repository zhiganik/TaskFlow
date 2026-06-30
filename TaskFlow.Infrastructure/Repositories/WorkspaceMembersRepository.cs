using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspaceMembersRepository(AppDbContext db) : IWorkspaceMembersRepository
{
    public async Task<WorkspaceMember?> GetMemberAsync(Guid workspaceId, string userId, CancellationToken ct = default)
        => await db.WorkspaceMembers
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId, ct);

    public async Task<IReadOnlyList<WorkspaceMember>> GetMembersAsync(Guid workspaceId, CancellationToken ct = default)
        => await db.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.User)
            .Where(m => m.WorkspaceId == workspaceId)
            .OrderBy(m => m.JoinedAt)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<WorkspaceMember>> GetMembershipsForUserAsync(string userId, CancellationToken ct = default)
        => await db.WorkspaceMembers
            .AsNoTracking()
            .Include(m => m.Workspace)
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.JoinedAt)
            .ToListAsync(ct);

    public async Task<WorkspaceMember> AddAsync(WorkspaceMember member, CancellationToken ct = default)
    {
        db.WorkspaceMembers.Add(member);
        await db.SaveChangesAsync(ct);
        return member;
    }

    public async Task UpdateAsync(WorkspaceMember member, CancellationToken ct = default)
    {
        db.Entry(member).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task<bool> DeleteAsync(Guid workspaceId, string userId, CancellationToken ct = default)
    {
        var member = await db.WorkspaceMembers.FindAsync([workspaceId, userId], ct);
        if (member is null) return false;

        db.WorkspaceMembers.Remove(member);
        await db.SaveChangesAsync(ct);
        return true;
    }
}
