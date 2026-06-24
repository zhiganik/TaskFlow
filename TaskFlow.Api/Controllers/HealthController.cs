using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController(IHealthService healthService) : ControllerBase
{
    /// <summary>Check whether the API is up and responding.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var result = await healthService.GetHealthAsync(ct);
        return Ok(result);
    }
}
