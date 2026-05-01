using System.Text.Json;
using System.Text.Json.Serialization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Queries.CompareImports;

public sealed class CompareImportsQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<CompareImportsQuery, CompareImportsResponseDto?>
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<CompareImportsResponseDto?> Handle(CompareImportsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var left = await LoadSideAsync(request.OwnerUserId, request.ProjectId, request.LeftImportId, cancellationToken)
            .ConfigureAwait(false);
        var right = await LoadSideAsync(request.OwnerUserId, request.ProjectId, request.RightImportId, cancellationToken)
            .ConfigureAwait(false);

        if (left is null || right is null)
        {
            return null;
        }

        var leftSet = left.IssueCodesDistinct.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var rightSet = right.IssueCodesDistinct.ToHashSet(StringComparer.OrdinalIgnoreCase);

        var onlyLeft = leftSet.Except(rightSet, StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
        var onlyRight = rightSet.Except(leftSet, StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();

        int? rowDelta = left.RowCount is { } l && right.RowCount is { } r ? l - r : null;
        int? issueDelta = left.IssueCount is { } li && right.IssueCount is { } ri ? li - ri : null;

        return new CompareImportsResponseDto(left, right, rowDelta, issueDelta, onlyLeft, onlyRight);
    }

    private async Task<ImportComparisonSideDto?> LoadSideAsync(
        Guid ownerUserId,
        Guid projectId,
        Guid importId,
        CancellationToken cancellationToken)
    {
        var row = await dbContext.ImportJobs.AsNoTracking()
            .Join(
                dbContext.Projects.Where(p => p.OwnerUserId == ownerUserId && p.Id == projectId),
                j => j.ProjectId,
                p => p.Id,
                (j, _) => j)
            .Where(j => j.Id == importId)
            .Select(
                j => new
                {
                    j.Id,
                    j.OriginalFileName,
                    j.Status,
                    j.RowCount,
                    j.IssueCount,
                    j.SummaryJson,
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

        var codes = await dbContext.ValidationIssues.AsNoTracking()
            .Where(i => i.ImportJobId == importId)
            .Select(i => i.Code)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        return new ImportComparisonSideDto(
            row.Id,
            row.OriginalFileName,
            row.Status.ToString(),
            row.RowCount,
            row.IssueCount,
            dominantType,
            dominantNamespace,
            codes);
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
        }
    }

    private sealed record ImportStoredSummaryEnvelope(
        [property: JsonPropertyName("metrics")] ImportStoredMetrics? Metrics);

    private sealed record ImportStoredMetrics(
        [property: JsonPropertyName("dominantType")] string? DominantType,
        [property: JsonPropertyName("dominantNamespace")] string? DominantNamespace);
}
