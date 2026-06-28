using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Repositories;

public class TaskAttachmentRepository(AppDbContext db) : ITaskAttachmentRepository
{
    public async Task<TaskAttachment?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.TaskAttachments
            .AsNoTracking()
            .Include(a => a.UploadedBy)
            .FirstOrDefaultAsync(a => a.Id == id, ct);

    public async Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(
        Guid taskId, CancellationToken ct = default) =>
        await db.TaskAttachments
            .AsNoTracking()
            .Include(a => a.UploadedBy)
            .Where(a => a.TaskId == taskId)
            .OrderBy(a => a.UploadedAt)
            .ToListAsync(ct);

    public async Task<TaskAttachment> AddAsync(TaskAttachment attachment, CancellationToken ct = default)
    {
        db.TaskAttachments.Add(attachment);
        await db.SaveChangesAsync(ct);
        return attachment;
    }

    public async Task UpdateAsync(TaskAttachment attachment, CancellationToken ct = default)
    {
        db.Entry(attachment).State = EntityState.Modified;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var attachment = await db.TaskAttachments.FindAsync([id], ct);
        if (attachment is null) return;

        db.TaskAttachments.Remove(attachment);
        await db.SaveChangesAsync(ct);
    }
}
