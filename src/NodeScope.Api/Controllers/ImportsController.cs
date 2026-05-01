using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeScope.Api.Extensions;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Application.Contracts.Imports;
using NodeScope.Application.Features.Imports.Commands.CreateImport;
using NodeScope.Application.Features.Imports.Queries.DownloadImportArtifact;
using NodeScope.Application.Features.Imports.Queries.GetImportSummary;
using NodeScope.Application.Features.Imports.Queries.ListImports;
using NodeScope.Application.Features.Projects.Queries.GetProject;

namespace NodeScope.Api.Controllers;

/// <summary>
/// Project-scoped import registration plus ingestion poll surfaces for Angular upload flows.
/// </summary>
[ApiController]
[Authorize]
public sealed class ProjectImportsController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Lists historical ingestions for owned analysis workspaces aligned with SPA inspection tables.
    /// </summary>
    [HttpGet("/api/projects/{projectId:guid}/imports")]
    [ProducesResponseType(typeof(IReadOnlyList<ImportJobSummaryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ImportJobSummaryDto>>> ListAsync(
        [FromRoute] Guid projectId,
        CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();

        var project = await mediator
            .Send(new GetProjectQuery(ownerId, projectId), cancellationToken)
            .ConfigureAwait(false);

        if (project is null)
        {
            return NotFound();
        }

        var summaries =
            await mediator.Send(new ListImportsForProjectQuery(ownerId, projectId), cancellationToken).ConfigureAwait(false);
        return Ok(summaries);
    }

    /// <summary>
    /// Streams raw technical datasets onto storage roots and persists pending processing tuples for python workers.
    /// </summary>
    [HttpPost("/api/projects/{projectId:guid}/imports")]
    [ProducesResponseType(typeof(ImportJobQueuedDto), StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportJobQueuedDto>> UploadAsync(
        [FromRoute] Guid projectId,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            return BadRequest("Uploaded payload is empty.");
        }

        var ownerId = User.GetRequiredUserId();
        await using var stream = file.OpenReadStream();
        var created = await mediator
            .Send(
                new CreateImportJobCommand(
                    ownerId,
                    projectId,
                    file.FileName,
                    file.ContentType,
                    file.Length,
                    stream),
                cancellationToken)
            .ConfigureAwait(false);

        return created is null ? NotFound() : Accepted($"/api/imports/{created.ImportId}/summary", created);
    }
}

/// <summary>
/// Cross-project import inspection helpers (summary polling, future chart surfaces).
/// </summary>
[ApiController]
[Authorize]
[Route("api/imports")]
public sealed class ImportInspectionController(ISender mediator) : ControllerBase
{
    /// <summary>
    /// Returns merged relational + persisted JSON summary payloads for SPA drill-down UX.
    /// </summary>
    [HttpGet("{importId:guid}/summary")]
    [ProducesResponseType(typeof(ImportJobSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ImportJobSummaryDto>> GetSummaryAsync(
        [FromRoute] Guid importId,
        CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var summary = await mediator.Send(new GetImportSummaryQuery(ownerId, importId), cancellationToken).ConfigureAwait(false);
        return summary is null ? NotFound() : Ok(summary);
    }

    /// <summary>Streams the HTML report produced for a completed import.</summary>
    [HttpGet("{importId:guid}/artifacts/report")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportArtifactAsync(
        [FromRoute] Guid importId,
        CancellationToken cancellationToken)
    {
        return await StreamArtifactAsync(importId, ImportArtifactKind.ReportHtml, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Streams normalized JSON rows produced for a completed import.</summary>
    [HttpGet("{importId:guid}/artifacts/normalized-json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetNormalizedJsonArtifactAsync(
        [FromRoute] Guid importId,
        CancellationToken cancellationToken) =>
        StreamArtifactAsync(importId, ImportArtifactKind.NormalizedJson, cancellationToken);

    /// <summary>Streams issues.csv produced for a completed import.</summary>
    [HttpGet("{importId:guid}/artifacts/issues-csv")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetIssuesCsvArtifactAsync(
        [FromRoute] Guid importId,
        CancellationToken cancellationToken) =>
        StreamArtifactAsync(importId, ImportArtifactKind.IssuesCsv, cancellationToken);

    private async Task<IActionResult> StreamArtifactAsync(Guid importId, ImportArtifactKind kind, CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var resolved =
            await mediator.Send(new ImportArtifactDownloadQuery(ownerId, importId, kind), cancellationToken).ConfigureAwait(false);

        return resolved is null ? NotFound() : PhysicalFile(resolved.AbsolutePhysicalPath, resolved.ContentType, resolved.DownloadFileName);
    }
}
