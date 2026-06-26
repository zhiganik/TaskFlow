using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IWorkspaceLabelsService
{
    Task<IReadOnlyList<LabelDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default);
    Task<LabelDto>                CreateAsync(Guid workspaceId, CreateLabelRequest request, CancellationToken ct = default);
    Task<LabelDto>                UpdateAsync(Guid workspaceId, Guid labelId, UpdateLabelRequest request, CancellationToken ct = default);
    Task                          DeleteAsync(Guid workspaceId, Guid labelId, CancellationToken ct = default);
}
