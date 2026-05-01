using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeScope.Api.Extensions;
using NodeScope.Application.Contracts.Dashboard;
using NodeScope.Application.Features.Dashboard.Queries.Statistics;

namespace NodeScope.Api.Controllers;

/// <summary>
/// Aggregated workspace analytics backing dashboard tiles in the Angular shell.
/// </summary>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class DashboardController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Returns tenant-scoped KPIs and recent ingestion attempts for operator situational awareness.
    /// </summary>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(DashboardStatisticsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<DashboardStatisticsDto>> GetStatisticsAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var dto = await mediator.Send(new GetDashboardStatisticsQuery(ownerId), cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
