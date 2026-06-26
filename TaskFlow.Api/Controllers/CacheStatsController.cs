using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.DTOs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/v1/admin/cache-stats")]
public class CacheStatsController(ICacheService cache) : ControllerBase
{
    /// <summary>Gets Redis cache hit/miss statistics per category.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(CacheStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStats(CancellationToken ct)
        => Ok(await cache.GetStatsAsync(ct));
}
