using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NodeScope.Api.Extensions;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Application.Contracts.Common;
using NodeScope.Application.Contracts.Imports;
using NodeScope.Application.Features.Imports.Commands.CreateImport;
using NodeScope.Application.Features.Imports.Commands.ReprocessImport;
using NodeScope.Application.Features.Imports.Queries.CompareImports;
using NodeScope.Application.Features.Imports.Queries.DownloadImportArtifact;
using NodeScope.Application.Features.Imports.Queries.GetImportSummary;
using NodeScope.Application.Features.Imports.Queries.ListDatasetRecords;
using NodeScope.Application.Features.Imports.Queries.ListImports;
using NodeScope.Application.Features.Imports.Queries.ListValidationIssues;
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
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(file);

        if (file.Length == 0)
        {
            return BadRequest("Uploaded payload is empty.");
        }

        if (string.IsNullOrWhiteSpace(file.ContentType))
        {
            return BadRequest("Uploaded payload has an unsupported content type.");
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

    /// <summary>Requeues a completed or failed import so the hosted processor can rerun against the stored file.</summary>
    [HttpPost("/api/projects/{projectId:guid}/imports/{importId:guid}/reprocess")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReprocessAsync(
        [FromRoute] Guid projectId,
        [FromRoute] Guid importId,
        CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var outcome = await mediator
            .Send(new ReprocessImportJobCommand(ownerId, projectId, importId), cancellationToken)
            .ConfigureAwait(false);

        return outcome switch
        {
            ReprocessImportJobResult.Requeued => NoContent(),
            ReprocessImportJobResult.NotFound => NotFound(),
            ReprocessImportJobResult.Conflict => Conflict(),
            _ => throw new InvalidOperationException($"Unexpected reprocess outcome: {outcome}."),
        };
    }

    /// <summary>Returns a lightweight diff summary between two ingestions belonging to this workspace.</summary>
    [HttpGet("/api/projects/{projectId:guid}/imports/compare")]
    [ProducesResponseType(typeof(CompareImportsResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CompareImportsResponseDto>> CompareImportsAsync(
        [FromRoute] Guid projectId,
        [FromQuery] Guid leftImportId,
        [FromQuery] Guid rightImportId,
        CancellationToken cancellationToken)
    {
        var ownerId = User.GetRequiredUserId();
        var dto = await mediator
            .Send(new CompareImportsQuery(ownerId, projectId, leftImportId, rightImportId), cancellationToken)
            .ConfigureAwait(false);
        return dto is null ? NotFound() : Ok(dto);
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

    /// <summary>Paged validation anomalies for SPA drill grids.</summary>
    [HttpGet("{importId:guid}/validation-issues")]
    [ProducesResponseType(typeof(PagedResultDto<ValidationIssueRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<ValidationIssueRowDto>>> ListValidationIssuesAsync(
        [FromRoute] Guid importId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? severity = null,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = User.GetRequiredUserId();
        var result = await mediator
            .Send(
                new ListValidationIssuesForImportQuery(ownerId, importId, page, pageSize, severity, search),
                cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
    }

    /// <summary>Paged sampled row payloads persisted for heterogeneous schemas.</summary>
    [HttpGet("{importId:guid}/dataset-records")]
    [ProducesResponseType(typeof(PagedResultDto<DatasetRecordRowDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResultDto<DatasetRecordRowDto>>> ListDatasetRecordsAsync(
        [FromRoute] Guid importId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        [FromQuery] string? search = null,
        CancellationToken cancellationToken = default)
    {
        var ownerId = User.GetRequiredUserId();
        var result = await mediator
            .Send(new ListDatasetRecordsForImportQuery(ownerId, importId, page, pageSize, search), cancellationToken)
            .ConfigureAwait(false);
        return Ok(result);
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
