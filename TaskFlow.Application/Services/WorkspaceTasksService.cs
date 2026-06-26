using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class WorkspaceTasksService(
    IWorkspaceTasksRepository repository,
    IWorkspaceColumnsRepository columnsRepository,
    IMapper mapper,
    ILogger<WorkspaceTasksService> logger) : IWorkspaceTasksService
{
    public async Task<IReadOnlyList<WorkspaceTaskDto>> GetByWorkspaceAsync(Guid workspaceId, TaskFilterQuery filter, CancellationToken ct)
    {
        var tasks = await repository.GetByWorkspaceIdAsync(workspaceId, filter, ct);
        return mapper.Map<IReadOnlyList<WorkspaceTaskDto>>(tasks);
    }

    public async Task<PagedResult<WorkspaceTaskDto>> GetPagedByColumnAsync(
        Guid workspaceId, Guid columnId, TaskFilterQuery filter, string? cursor, int limit, CancellationToken ct)
    {
        var result = await repository.GetPagedByColumnAsync(workspaceId, columnId, filter, cursor, limit, ct);
        var dtos = mapper.Map<IReadOnlyList<WorkspaceTaskDto>>(result.Items);
        return new PagedResult<WorkspaceTaskDto>(dtos, result.NextCursor, result.HasMore);
    }

    public async Task<WorkspaceTaskDto> GetByIdAsync(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);
        return mapper.Map<WorkspaceTaskDto>(task);
    }

    public async Task<WorkspaceTaskDto> CreateAsync(Guid workspaceId, string createdById, CreateTaskRequest request, CancellationToken ct)
    {
        var column = await columnsRepository.GetByIdAsync(request.ColumnId, ct);
        if (column is null || column.WorkspaceId != workspaceId)
            throw new NotFoundException($"Column {request.ColumnId} was not found in this workspace.");

        var order  = await repository.CountByColumnIdAsync(request.ColumnId, ct);
        var number = await repository.GetNextNumberAsync(workspaceId, ct);

        var task = new WorkspaceTask
        {
            Number      = number,
            WorkspaceId = workspaceId,
            ColumnId    = request.ColumnId,
            Title       = request.Title,
            Description = request.Description,
            Priority    = request.Priority,
            AssigneeId  = request.AssigneeId,
            DueDate     = request.DueDate,
            CreatedById = createdById,
            Order       = order,
            UpdatedAt   = DateTime.UtcNow
        };

        await repository.AddAsync(task, ct);

        var created = await repository.GetByIdAsync(task.Id, ct) ?? task;

        logger.LogInformation("Task {TaskId} created in column {ColumnId} by {UserId}", task.Id, request.ColumnId, createdById);

        return mapper.Map<WorkspaceTaskDto>(created);
    }

    public async Task<WorkspaceTaskDto> UpdateAsync(Guid workspaceId, Guid taskId, UpdateTaskRequest request, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        task.Title       = request.Title;
        task.Description = request.Description;
        task.Priority    = request.Priority;
        task.AssigneeId  = request.AssigneeId;
        task.Assignee    = null;
        task.DueDate     = request.DueDate;
        task.UpdatedAt   = DateTime.UtcNow;

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} updated in workspace {WorkspaceId}", taskId, workspaceId);

        var updated = await repository.GetByIdAsync(taskId, ct) ?? task;
        return mapper.Map<WorkspaceTaskDto>(updated);
    }

    public async Task<WorkspaceTaskDto> MoveAsync(Guid workspaceId, Guid taskId, MoveTaskRequest request, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        if (task.ColumnId == request.ColumnId)
            throw new BadRequestException("Task is already in the target column.");

        var targetColumn = await columnsRepository.GetByIdAsync(request.ColumnId, ct);
        if (targetColumn is null || targetColumn.WorkspaceId != workspaceId)
            throw new NotFoundException($"Column {request.ColumnId} was not found in this workspace.");

        task.ColumnId  = request.ColumnId;
        task.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} moved to column {ColumnId}", taskId, request.ColumnId);

        var updated = await repository.GetByIdAsync(taskId, ct) ?? task;
        return mapper.Map<WorkspaceTaskDto>(updated);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        await GetOwnedTaskAsync(workspaceId, taskId, ct);

        await repository.DeleteAsync(taskId, ct);

        logger.LogInformation("Task {TaskId} deleted from workspace {WorkspaceId}", taskId, workspaceId);
    }

    private async Task<WorkspaceTask> GetOwnedTaskAsync(Guid workspaceId, Guid taskId, CancellationToken ct)
    {
        var task = await repository.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException($"Task {taskId} was not found.");

        if (task.WorkspaceId != workspaceId)
            throw new NotFoundException($"Task {taskId} was not found.");

        return task;
    }
}
