using TaskFlow.Application.Domain.Enums;
using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspacePriorityConfigService
{
    Task<IReadOnlyList<PriorityConfigDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<PriorityConfigDto>                UpdateAsync(Guid workspaceId, TaskPriority priority, UpdatePriorityConfigRequest request, CancellationToken ct = default);
}
