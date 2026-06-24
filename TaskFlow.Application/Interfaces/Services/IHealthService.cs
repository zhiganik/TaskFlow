using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces.Services;

public interface IHealthService
{
    Task<HealthDto> GetHealthAsync(CancellationToken ct = default);
}
