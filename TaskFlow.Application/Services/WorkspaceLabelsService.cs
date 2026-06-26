using AutoMapper;
using Microsoft.Extensions.Logging;
using TaskFlow.Application.Domain.Entities;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Exceptions;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class WorkspaceLabelsService(
    IWorkspaceLabelsRepository repository,
    IMapper mapper,
    ILogger<WorkspaceLabelsService> logger) : IWorkspaceLabelsService
{
    public async Task<IReadOnlyList<LabelDto>> GetByWorkspaceAsync(Guid workspaceId, CancellationToken ct = default)
    {
        var labels = await repository.GetByWorkspaceIdAsync(workspaceId, ct);
        return mapper.Map<IReadOnlyList<LabelDto>>(labels);
    }

    public async Task<LabelDto> CreateAsync(Guid workspaceId, CreateLabelRequest request, CancellationToken ct = default)
    {
        var label = new WorkspaceLabel
        {
            WorkspaceId = workspaceId,
            Name        = request.Name.Trim(),
            Color       = request.Color
        };

        await repository.AddAsync(label, ct);

        logger.LogInformation("Label {LabelId} created in workspace {WorkspaceId}", label.Id, workspaceId);

        return mapper.Map<LabelDto>(label);
    }

    public async Task<LabelDto> UpdateAsync(Guid workspaceId, Guid labelId, UpdateLabelRequest request, CancellationToken ct = default)
    {
        var label = await GetOwnedLabelAsync(workspaceId, labelId, ct);

        label.Name  = request.Name.Trim();
        label.Color = request.Color;

        await repository.UpdateAsync(label, ct);

        logger.LogInformation("Label {LabelId} updated in workspace {WorkspaceId}", labelId, workspaceId);

        return mapper.Map<LabelDto>(label);
    }

    public async Task DeleteAsync(Guid workspaceId, Guid labelId, CancellationToken ct = default)
    {
        await GetOwnedLabelAsync(workspaceId, labelId, ct);

        await repository.DeleteAsync(labelId, ct);

        logger.LogInformation("Label {LabelId} deleted from workspace {WorkspaceId}", labelId, workspaceId);
    }

    private async Task<WorkspaceLabel> GetOwnedLabelAsync(Guid workspaceId, Guid labelId, CancellationToken ct)
    {
        var label = await repository.GetByIdAsync(labelId, ct)
            ?? throw new NotFoundException($"Label {labelId} was not found.");

        if (label.WorkspaceId != workspaceId)
            throw new NotFoundException($"Label {labelId} was not found.");

        return label;
    }
}
