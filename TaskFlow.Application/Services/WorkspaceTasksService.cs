using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Contracts.Messages;

namespace TaskFlow.Application.Services;

public class WorkspaceTasksService(
    IWorkspaceTasksRepository repository,
    IWorkspaceColumnsRepository columnsRepository,
    IWorkspaceLabelsRepository labelsRepository,
    IMessagePublisher publisher,
    UserManager<AppUser> userManager,
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

        if (request.LabelIds is { Count: > 0 })
        {
            await ValidateLabelIds(workspaceId, request.LabelIds, ct);
            await repository.AddLabelsAsync(task.Id, request.LabelIds, ct);
        }

        var created = await repository.GetByIdAsync(task.Id, ct) ?? task;

        logger.LogInformation("Task {TaskId} created in column {ColumnId} by {UserId}", task.Id, request.ColumnId, createdById);

        if (request.AssigneeId is not null && request.AssigneeId != createdById)
        {
            var creator = await userManager.FindByIdAsync(createdById);
            await publisher.PublishAsync(new TaskAssignedEvent(
                task.Id,
                task.Title,
                Guid.Empty,
                workspaceId,
                request.AssigneeId,
                createdById,
                creator?.DisplayName ?? string.Empty,
                DateTime.UtcNow), ct);
        }

        return mapper.Map<WorkspaceTaskDto>(created);
    }

    public async Task<WorkspaceTaskDto> UpdateAsync(Guid workspaceId, Guid taskId, UpdateTaskRequest request, string updatedById, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        var previousAssigneeId = task.AssigneeId;

        task.Title       = request.Title;
        task.Description = request.Description;
        task.Priority    = request.Priority;
        task.AssigneeId  = request.AssigneeId;
        task.Assignee    = null;
        task.DueDate     = request.DueDate;
        task.UpdatedAt   = DateTime.UtcNow;

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} updated in workspace {WorkspaceId}", taskId, workspaceId);

        if (request.AssigneeId is not null
            && request.AssigneeId != previousAssigneeId
            && request.AssigneeId != updatedById)
        {
            var updatedBy = await userManager.FindByIdAsync(updatedById);
            await publisher.PublishAsync(new TaskAssignedEvent(
                taskId,
                task.Title,
                Guid.Empty,
                workspaceId,
                request.AssigneeId,
                updatedById,
                updatedBy?.DisplayName ?? string.Empty,
                DateTime.UtcNow), ct);
        }

        var updated = await repository.GetByIdAsync(taskId, ct) ?? task;
        return mapper.Map<WorkspaceTaskDto>(updated);
    }

    public async Task<WorkspaceTaskDto> MoveAsync(Guid workspaceId, Guid taskId, MoveTaskRequest request, string movedById, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        if (task.Status is WorkspaceTaskStatus.Closed or WorkspaceTaskStatus.Deleted)
            throw new BadRequestException("Cannot move an archived task. Reopen it first.");

        if (task.ColumnId == request.ColumnId)
            throw new BadRequestException("Task is already in the target column.");

        var targetColumn = await columnsRepository.GetByIdAsync(request.ColumnId, ct);
        if (targetColumn is null || targetColumn.WorkspaceId != workspaceId)
            throw new NotFoundException($"Column {request.ColumnId} was not found in this workspace.");

        var oldStatus = task.Status;

        task.ColumnId  = request.ColumnId;
        task.UpdatedAt = DateTime.UtcNow;

        if (targetColumn.IsDoneColumn && task.Status != WorkspaceTaskStatus.Done)
        {
            task.Status      = WorkspaceTaskStatus.Done;
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (!targetColumn.IsDoneColumn && task.Status == WorkspaceTaskStatus.Done)
        {
            task.Status      = WorkspaceTaskStatus.Active;
            task.CompletedAt = null;
        }

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} moved to column {ColumnId}", taskId, request.ColumnId);

        if (task.Status != oldStatus)
            await PublishStatusChangedEventAsync(task, workspaceId, oldStatus, task.Status, movedById, ct);

        var updated = await repository.GetByIdAsync(taskId, ct) ?? task;
        return mapper.Map<WorkspaceTaskDto>(updated);
    }

    public async Task<WorkspaceTaskDto> CloseAsync(Guid workspaceId, Guid taskId, string closedById, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        if (task.Status == WorkspaceTaskStatus.Closed || task.Status == WorkspaceTaskStatus.Deleted)
            throw new BadRequestException("Task is already in the archive.");

        var oldStatus = task.Status;

        task.Status    = WorkspaceTaskStatus.Closed;
        task.ClosedAt  = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} closed in workspace {WorkspaceId}", taskId, workspaceId);

        await PublishStatusChangedEventAsync(task, workspaceId, oldStatus, WorkspaceTaskStatus.Closed, closedById, ct);

        return mapper.Map<WorkspaceTaskDto>(task);
    }

    public async Task<WorkspaceTaskDto> ReopenAsync(Guid workspaceId, Guid taskId, string reopenedById, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        if (task.Status is not WorkspaceTaskStatus.Closed and not WorkspaceTaskStatus.Deleted)
            throw new BadRequestException("Only closed or deleted tasks can be reopened.");

        var oldStatus = task.Status;
        var column = await columnsRepository.GetByIdAsync(task.ColumnId, ct);
        task.Status      = column?.IsDoneColumn == true ? WorkspaceTaskStatus.Done : WorkspaceTaskStatus.Active;
        task.CompletedAt = column?.IsDoneColumn == true ? task.CompletedAt : null;
        task.ClosedAt    = null;
        task.UpdatedAt   = DateTime.UtcNow;

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} reopened in workspace {WorkspaceId}", taskId, workspaceId);

        await PublishStatusChangedEventAsync(task, workspaceId, oldStatus, task.Status, reopenedById, ct);

        return mapper.Map<WorkspaceTaskDto>(task);
    }

    public async Task<WorkspaceTaskDto> SetLabelsAsync(Guid workspaceId, Guid taskId, SetTaskLabelsRequest request, CancellationToken ct)
    {
        await GetOwnedTaskAsync(workspaceId, taskId, ct);
        await ValidateLabelIds(workspaceId, request.LabelIds, ct);
        await repository.SetLabelsAsync(taskId, request.LabelIds, ct);

        logger.LogInformation("Labels updated on task {TaskId} in workspace {WorkspaceId}", taskId, workspaceId);

        var updated = await repository.GetByIdAsync(taskId, ct)
            ?? throw new NotFoundException($"Task {taskId} was not found.");
        return mapper.Map<WorkspaceTaskDto>(updated);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid taskId, string deletedById, CancellationToken ct)
    {
        var task = await GetOwnedTaskAsync(workspaceId, taskId, ct);

        if (task.Status == WorkspaceTaskStatus.Closed || task.Status == WorkspaceTaskStatus.Deleted)
        {
            await repository.DeleteAsync(taskId, ct);
            logger.LogInformation("Task {TaskId} hard-deleted from workspace {WorkspaceId}", taskId, workspaceId);
            return;
        }

        var oldStatus = task.Status;

        task.Status    = WorkspaceTaskStatus.Deleted;
        task.ClosedAt  = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(task, ct);

        logger.LogInformation("Task {TaskId} soft-deleted in workspace {WorkspaceId}", taskId, workspaceId);

        await PublishStatusChangedEventAsync(task, workspaceId, oldStatus, WorkspaceTaskStatus.Deleted, deletedById, ct);
    }

    private async Task PublishStatusChangedEventAsync(
        WorkspaceTask task,
        Guid workspaceId,
        WorkspaceTaskStatus oldStatus,
        WorkspaceTaskStatus newStatus,
        string changedById,
        CancellationToken ct)
    {
        var changedBy = await userManager.FindByIdAsync(changedById);
        await publisher.PublishAsync(new TaskStatusChangedEvent(
            task.Id,
            task.Title,
            Guid.Empty,
            workspaceId,
            changedById,
            changedBy?.DisplayName ?? string.Empty,
            oldStatus.ToString(),
            newStatus.ToString(),
            task.AssigneeId,
            DateTime.UtcNow), ct);
    }

    private async Task ValidateLabelIds(Guid workspaceId, IReadOnlyList<Guid> labelIds, CancellationToken ct)
    {
        var workspaceLabels = await labelsRepository.GetByWorkspaceIdAsync(workspaceId, ct);
        var validIds = workspaceLabels.Select(l => l.Id).ToHashSet();
        var invalid = labelIds.FirstOrDefault(id => !validIds.Contains(id));
        if (invalid != default)
            throw new NotFoundException($"Label {invalid} was not found in this workspace.");
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
