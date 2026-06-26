using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Caching;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class WorkspaceColumnsService(
    IWorkspaceColumnsRepository repository,
    IWorkspaceTasksRepository tasksRepository,
    ICacheService cache,
    IMapper mapper,
    ILogger<WorkspaceColumnsService> logger) : IWorkspaceColumnsService
{
    private const int MaxColumns = 7;

    private static readonly string[] ColorPalette =
    [
        "#6366F1", "#F59E0B", "#10B981", "#EF4444",
        "#3B82F6", "#8B5CF6", "#F97316", "#06B6D4",
        "#84CC16", "#EC4899"
    ];

    private static string RandomColor() => ColorPalette[Random.Shared.Next(ColorPalette.Length)];

    public async Task<IReadOnlyList<WorkspaceColumnDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct)
    {
        var key    = CacheKeys.WorkspaceColumns(workspaceId);
        var cached = await cache.GetAsync<List<WorkspaceColumnDto>>(key, CacheKeys.Category.Columns, ct);
        if (cached is not null) return cached;

        var columns = await repository.GetByWorkspaceIdAsync(workspaceId, ct);
        var dtos    = mapper.Map<List<WorkspaceColumnDto>>(columns);

        await cache.SetAsync(key, dtos, CacheKeys.Ttl.Columns, ct);
        return dtos;
    }

    public async Task<WorkspaceColumnDto> CreateAsync(Guid workspaceId, CreateColumnRequest request, CancellationToken ct)
    {
        var count = await repository.CountByWorkspaceIdAsync(workspaceId, ct);
        if (count >= MaxColumns)
            throw new ConflictException($"Workspace column limit ({MaxColumns}) reached.");

        var column = new WorkspaceColumn
        {
            WorkspaceId = workspaceId,
            Name        = request.Name,
            Color       = request.Color ?? RandomColor(),
            Order       = count
        };

        await repository.AddAsync(column, ct);
        await cache.InvalidateAsync(CacheKeys.WorkspaceColumns(workspaceId), ct);

        logger.LogInformation("Column {ColumnId} created in workspace {WorkspaceId}", column.Id, workspaceId);

        return mapper.Map<WorkspaceColumnDto>(column);
    }

    public async Task<WorkspaceColumnDto> RenameAsync(Guid workspaceId, Guid columnId, UpdateColumnRequest request, CancellationToken ct)
    {
        var column = await GetOwnedColumnAsync(workspaceId, columnId, ct);

        column.Name = request.Name;
        if (request.Color is not null)
            column.Color = request.Color;
        await repository.UpdateAsync(column, ct);
        await cache.InvalidateAsync(CacheKeys.WorkspaceColumns(workspaceId), ct);

        logger.LogInformation("Column {ColumnId} renamed in workspace {WorkspaceId}", columnId, workspaceId);

        return mapper.Map<WorkspaceColumnDto>(column);
    }

    public async Task ReorderAsync(Guid workspaceId, ReorderColumnsRequest request, CancellationToken ct)
    {
        var existing   = await repository.GetByWorkspaceIdAsync(workspaceId, ct);
        var existingIds = existing.Select(c => c.Id).ToHashSet();

        if (request.ColumnIds.Count != existingIds.Count
            || request.ColumnIds.Any(id => !existingIds.Contains(id)))
            throw new ConflictException("ColumnIds must contain exactly the workspace's current column IDs.");

        var lookup  = existing.ToDictionary(c => c.Id);
        var updated = request.ColumnIds
            .Select((id, index) =>
            {
                var col = lookup[id];
                col.Order = index;
                return col;
            })
            .ToList();

        await repository.UpdateRangeAsync(updated, ct);
        await cache.InvalidateAsync(CacheKeys.WorkspaceColumns(workspaceId), ct);

        logger.LogInformation("Columns reordered in workspace {WorkspaceId}", workspaceId);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid columnId, CancellationToken ct)
    {
        await GetOwnedColumnAsync(workspaceId, columnId, ct);

        var taskCount = await tasksRepository.CountByColumnIdAsync(columnId, ct);
        if (taskCount > 0)
            throw new ConflictException($"Column still has {taskCount} task(s). Move or delete them first.");

        await repository.DeleteAsync(columnId, ct);
        await cache.InvalidateAsync(CacheKeys.WorkspaceColumns(workspaceId), ct);

        logger.LogInformation("Column {ColumnId} deleted from workspace {WorkspaceId}", columnId, workspaceId);
    }

    private async Task<WorkspaceColumn> GetOwnedColumnAsync(Guid workspaceId, Guid columnId, CancellationToken ct)
    {
        var column = await repository.GetByIdAsync(columnId, ct)
            ?? throw new NotFoundException($"Column {columnId} was not found.");

        if (column.WorkspaceId != workspaceId)
            throw new NotFoundException($"Column {columnId} was not found.");

        return column;
    }
}
