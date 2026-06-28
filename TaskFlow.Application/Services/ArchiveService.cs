using AutoMapper;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class ArchiveService(
    IArchiveRepository archiveRepository,
    IMapper mapper) : IArchiveService
{
    public async Task<IReadOnlyList<WorkspaceTaskDto>> GetAsync(Guid workspaceId, TaskFilterQuery filter, CancellationToken ct)
    {
        var tasks = await archiveRepository.GetByWorkspaceAsync(workspaceId, filter, ct);
        return mapper.Map<IReadOnlyList<WorkspaceTaskDto>>(tasks);
    }
}
