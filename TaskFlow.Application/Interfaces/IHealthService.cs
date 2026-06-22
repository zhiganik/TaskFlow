using TaskFlow.Application.DTOs;

namespace TaskFlow.Application.Interfaces;

public interface IHealthService
{
    Task<HealthDto> GetHealthAsync(CancellationToken ct = default);
}
