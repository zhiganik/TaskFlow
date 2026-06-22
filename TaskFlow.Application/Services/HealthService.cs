using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Application.Services;

public class HealthService : IHealthService
{
    public Task<HealthDto> GetHealthAsync(CancellationToken ct = default)
        => Task.FromResult(new HealthDto("Healthy", DateTime.UtcNow));
}
