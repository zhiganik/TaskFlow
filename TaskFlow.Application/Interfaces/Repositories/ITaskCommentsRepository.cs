using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface ITaskCommentsRepository
{
    Task<PagedResult<TaskComment>> GetByTaskIdAsync(Guid taskId, string? cursor, int limit, CancellationToken ct = default);
    Task<TaskComment?>             GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<TaskComment>              AddAsync(TaskComment comment, CancellationToken ct = default);
    Task                           UpdateAsync(TaskComment comment, CancellationToken ct = default);
    Task                           DeleteAsync(Guid id, CancellationToken ct = default);
}
