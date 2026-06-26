using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public partial class TaskCommentsService(
    ITaskCommentsRepository commentsRepository,
    IWorkspaceTasksRepository tasksRepository,
    IMapper mapper,
    ILogger<TaskCommentsService> logger) : ITaskCommentsService
{
    [GeneratedRegex(@"@\[[^\]]+\]\(([^)]+)\)")]
    private static partial Regex MentionRegex();

    public async Task<PagedResult<TaskCommentDto>> GetByTaskIdAsync(
        Guid workspaceId, Guid taskId, string? cursor, int limit, CancellationToken ct = default)
    {
        await GetOwnedTaskAsync(workspaceId, taskId, ct);

        var paged = await commentsRepository.GetByTaskIdAsync(taskId, cursor, limit, ct);

        return new PagedResult<TaskCommentDto>(
            paged.Items.Select(mapper.Map<TaskCommentDto>).ToList().AsReadOnly(),
            paged.NextCursor,
            paged.HasMore);
    }

    public async Task<TaskCommentDto> CreateAsync(
        Guid workspaceId, Guid taskId, string createdById, CreateCommentRequest request, CancellationToken ct = default)
    {
        await GetOwnedTaskAsync(workspaceId, taskId, ct);

        var comment = new TaskComment
        {
            TaskId      = taskId,
            Content     = request.Content,
            CreatedById = createdById,
        };

        comment.Mentions = ExtractMentions(comment.Id, request.Content);

        await commentsRepository.AddAsync(comment, ct);

        var created = await commentsRepository.GetByIdAsync(comment.Id, ct) ?? comment;

        logger.LogInformation("Comment {CommentId} created on task {TaskId} by {UserId}", comment.Id, taskId, createdById);

        return mapper.Map<TaskCommentDto>(created);
    }

    public async Task<TaskCommentDto> UpdateAsync(
        Guid workspaceId, Guid taskId, Guid commentId, string userId, UpdateCommentRequest request, CancellationToken ct = default)
    {
        await GetOwnedTaskAsync(workspaceId, taskId, ct);

        var comment = await commentsRepository.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException($"Comment {commentId} was not found.");

        if (comment.TaskId != taskId)
            throw new NotFoundException($"Comment {commentId} was not found.");

        if (comment.CreatedById != userId)
            throw new ForbiddenException("You can only edit your own comments.");

        comment.Content   = request.Content;
        comment.UpdatedAt = DateTime.UtcNow;
        comment.Mentions  = ExtractMentions(commentId, request.Content);

        await commentsRepository.UpdateAsync(comment, ct);

        logger.LogInformation("Comment {CommentId} updated by {UserId}", commentId, userId);

        var updated = await commentsRepository.GetByIdAsync(commentId, ct) ?? comment;
        return mapper.Map<TaskCommentDto>(updated);
    }

    public async Task DeleteAsync(
        Guid workspaceId, Guid taskId, Guid commentId, string userId, CancellationToken ct = default)
    {
        await GetOwnedTaskAsync(workspaceId, taskId, ct);

        var comment = await commentsRepository.GetByIdAsync(commentId, ct)
            ?? throw new NotFoundException($"Comment {commentId} was not found.");

        if (comment.TaskId != taskId)
            throw new NotFoundException($"Comment {commentId} was not found.");

        if (comment.CreatedById != userId)
            throw new ForbiddenException("You can only delete your own comments.");

        await commentsRepository.DeleteAsync(commentId, ct);

        logger.LogInformation("Comment {CommentId} deleted by {UserId}", commentId, userId);
    }

    private async Task<WorkspaceTask> GetOwnedTaskAsync(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var task = await tasksRepository.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException($"Task {taskId} was not found.");

        if (task.WorkspaceId != workspaceId)
            throw new NotFoundException($"Task {taskId} was not found.");

        return task;
    }

    private static ICollection<TaskCommentMention> ExtractMentions(Guid commentId, string content)
    {
        return MentionRegex()
            .Matches(content)
            .Select(m => new TaskCommentMention { CommentId = commentId, UserId = m.Groups[1].Value })
            .DistinctBy(m => m.UserId)
            .ToList();
    }
}
