using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.GetImportSummary;

public sealed class GetImportSummaryQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<GetImportSummaryQuery, ImportJobSummaryDto?>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<ImportJobSummaryDto?> Handle(GetImportSummaryQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var row = await dbContext.ImportJobs.AsNoTracking()
            .Join(
                dbContext.Projects.Where(p => p.OwnerUserId == request.OwnerUserId),
                imp => imp.ProjectId,
                project => project.Id,
                (imp, _) => imp)
            .Where(imp => imp.Id == request.ImportId)
            .Select(
                imp => new
                {
                    imp.Id,
                    imp.ProjectId,
                    imp.OriginalFileName,
                    imp.Status,
                    imp.RowCount,
                    imp.IssueCount,
                    imp.CompletedAt,
                    imp.SummaryJson,
                })
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (row is null)
        {
            return null;
        }

        string? dominantType = null;
        string? dominantNamespace = null;

        if (!string.IsNullOrWhiteSpace(row.SummaryJson))
        {
            TryParseSummary(row.SummaryJson, ref dominantType, ref dominantNamespace);
        }

        return new ImportJobSummaryDto(
            row.Id,
            row.ProjectId,
            row.OriginalFileName,
            row.Status,
            row.RowCount,
            row.IssueCount,
            dominantType,
            dominantNamespace,
            row.CompletedAt);
    }

    private static void TryParseSummary(string json, ref string? dominantType, ref string? dominantNamespace)
    {
        try
        {
            var envelope = JsonSerializer.Deserialize<ImportStoredSummaryEnvelope>(json, SerializerOptions);
            dominantType = envelope?.Metrics?.DominantType;
            dominantNamespace = envelope?.Metrics?.DominantNamespace;
        }
        catch (JsonException)
        {
            // Non-fatal: summary remains enriched from relational columns alone.
        }
    }
}

internal sealed record ImportStoredSummaryEnvelope(
    [property: JsonPropertyName("metrics")] ImportStoredMetrics? Metrics);

internal sealed record ImportStoredMetrics(
    [property: JsonPropertyName("dominantType")] string? DominantType,
    [property: JsonPropertyName("dominantNamespace")] string? DominantNamespace);
