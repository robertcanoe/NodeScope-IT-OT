using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeScope.Api.Extensions;
using NodeScope.Application.Contracts.Projects;
using NodeScope.Application.Features.Projects.Commands.CreateProject;
using NodeScope.Application.Features.Projects.Commands.DeleteProject;
using NodeScope.Application.Features.Projects.Commands.UpdateProject;
using NodeScope.Application.Features.Projects.Queries.GetProject;
using NodeScope.Application.Features.Projects.Queries.ListProjects;

namespace NodeScope.Api.Controllers;

/// <summary>
/// Tenant-scoped project workspace endpoints consumed by SPA multi-step ingestion flows.
/// </summary>
/// <param name="mediator">MediatR façade converting HTTP verbs into deterministic CQRS executions.</param>
[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ProjectsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Provisions analysis workspace metadata bound to JWT subject tenancy claims.
    /// </summary>
    /// <param name="payload">Validated inbound model aligned with FluentValidation aggregates.</param>
    /// <param name="cancellationToken">HTTP propagation token bridging Kestrel to EF Core workloads.</param>
    /// <returns>Persisted project projection surfaced to Angular routing layers.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ProjectResponseDto>> CreateProjectAsync(
        [FromBody] CreateProjectDto payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var ownerId = User.GetRequiredUserId();
        var dto = await mediator.Send(new CreateProjectCommand(ownerId, payload), cancellationToken).ConfigureAwait(false);

        return Ok(dto);
    }

    /// <summary>
    /// Lists all analysis workspaces attributed to authenticated operators ordered by freshest mutation timestamps.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectResponseDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ProjectResponseDto>>> ListProjectsAsync(CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var items = await mediator.Send(new ListProjectsQuery(ownerId), cancellationToken).ConfigureAwait(false);
        return Ok(items);
    }

    /// <summary>
    /// Returns a deterministic workspace envelope when tenancy claims align with persisted aggregates.
    /// </summary>
    [HttpGet("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponseDto>> GetProjectAsync([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var dto = await mediator.Send(new GetProjectQuery(ownerId, projectId), cancellationToken).ConfigureAwait(false);
        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Mutates persisted workspace envelopes when operators reconcile naming/metadata drift during audits.
    /// </summary>
    [HttpPut("{projectId:guid}")]
    [ProducesResponseType(typeof(ProjectResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProjectResponseDto>> UpdateProjectAsync(
        [FromRoute] Guid projectId,
        [FromBody] UpdateProjectDto payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);

        var ownerId = User.GetRequiredUserId();
        var dto =
            await mediator.Send(new UpdateProjectCommand(ownerId, projectId, payload), cancellationToken).ConfigureAwait(false);

        return dto is null ? NotFound() : Ok(dto);
    }

    /// <summary>
    /// Removes an entire workspace lineage when operators retire obsolete datasets aligned with cascading persistence rules.
    /// </summary>
    [HttpDelete("{projectId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProjectAsync([FromRoute] Guid projectId, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var removed = await mediator.Send(new DeleteProjectCommand(ownerId, projectId), cancellationToken).ConfigureAwait(false);
        return removed ? NoContent() : NotFound();
    }
}
