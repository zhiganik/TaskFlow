using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ITaskAttachmentRepository
{
    Task<TaskAttachment?>               GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<TaskAttachment>> GetByTaskIdAsync(Guid taskId, CancellationToken ct = default);
    Task<TaskAttachment>                AddAsync(TaskAttachment attachment, CancellationToken ct = default);
    Task                                UpdateAsync(TaskAttachment attachment, CancellationToken ct = default);
    Task                                DeleteAsync(Guid id, CancellationToken ct = default);
}
