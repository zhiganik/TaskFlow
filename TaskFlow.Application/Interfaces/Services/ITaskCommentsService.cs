using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface ITaskCommentsService
{
    Task<PagedResult<TaskCommentDto>> GetByTaskIdAsync(Guid workspaceId, Guid taskId, string? cursor, int limit, CancellationToken ct = default);
    Task<TaskCommentDto>              CreateAsync(Guid workspaceId, Guid taskId, string createdById, CreateCommentRequest request, CancellationToken ct = default);
    Task<TaskCommentDto>              UpdateAsync(Guid workspaceId, Guid taskId, Guid commentId, string userId, UpdateCommentRequest request, CancellationToken ct = default);
    Task                              DeleteAsync(Guid workspaceId, Guid taskId, Guid commentId, string userId, CancellationToken ct = default);
}
