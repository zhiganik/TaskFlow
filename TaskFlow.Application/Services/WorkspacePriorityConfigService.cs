using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class WorkspacePriorityConfigService(
    IWorkspacePriorityConfigRepository repository,
    IMapper mapper,
    ILogger<WorkspacePriorityConfigService> logger) : IWorkspacePriorityConfigService
{
    public async Task<IReadOnlyList<PriorityConfigDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var configs = await repository.GetByWorkspaceIdAsync(workspaceId, ct);
        return mapper.Map<IReadOnlyList<PriorityConfigDto>>(configs);
    }

    public async Task<PriorityConfigDto> UpdateAsync(Guid workspaceId, TaskPriority priority, UpdatePriorityConfigRequest request, CancellationToken ct = default)
    {
        var config = await repository.GetByPriorityAsync(workspaceId, priority, ct)
            ?? throw new NotFoundException($"Priority config for {priority} was not found in this workspace.");

        config.DisplayName = request.DisplayName.Trim();
        config.Color       = request.Color;

        await repository.UpdateAsync(config, ct);

        logger.LogInformation("Priority config {Priority} updated in workspace {WorkspaceId}", priority, workspaceId);

        return mapper.Map<PriorityConfigDto>(config);
    }
}
