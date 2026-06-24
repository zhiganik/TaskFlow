using TaskFlow.Application.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspacesRepository
{
    Task<Workspace?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Workspace>  AddAsync(Workspace workspace, CancellationToken ct = default);
    Task             UpdateAsync(Workspace workspace, CancellationToken ct = default);
    Task<bool>       DeleteAsync(Guid id, CancellationToken ct = default);
}
