using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Route("api/v1/health")]
public class HealthController(IHealthService healthService) : ControllerBase
{
    /// <summary>Check whether the API is up and responding.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Health check", Tags = new[] { "Health" })]
    [ProducesResponseType(typeof(HealthDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetHealth(CancellationToken ct)
    {
        var result = await healthService.GetHealthAsync(ct);
        return Ok(result);
    }
}
