using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspaceInvitationRepository(AppDbContext db) : IWorkspaceInvitationRepository
{
    public async Task<WorkspaceInvitation?> GetByTokenAsync(string token, CancellationToken ct = default)
        => await db.WorkspaceInvitations
            .AsNoTracking()
            .Include(i => i.Workspace)
            .Include(i => i.InvitedBy)
            .FirstOrDefaultAsync(i => i.Token == token, ct);

    public async Task<WorkspaceInvitation?> GetByWorkspaceAndEmailAsync(Guid workspaceId, string email, CancellationToken ct = default)
        => await db.WorkspaceInvitations
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.WorkspaceId == workspaceId && i.Email == email, ct);

    public async Task<IReadOnlyList<WorkspaceInvitation>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
        => await db.WorkspaceInvitations
            .AsNoTracking()
            .Include(i => i.InvitedBy)
            .Where(i => i.WorkspaceId == workspaceId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(ct);

    public async Task<WorkspaceInvitation> AddAsync(WorkspaceInvitation invitation, CancellationToken ct = default)
    {
        db.WorkspaceInvitations.Add(invitation);
        await db.SaveChangesAsync(ct);
        return invitation;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var invitation = await db.WorkspaceInvitations.FindAsync([id], ct);
        if (invitation is null) return;

        db.WorkspaceInvitations.Remove(invitation);
        await db.SaveChangesAsync(ct);
    }
}
