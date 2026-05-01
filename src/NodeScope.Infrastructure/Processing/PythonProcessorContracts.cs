using System.Text.Json;
using System.Text.Json.Serialization;

namespace NodeScope.Infrastructure.Processing;

internal sealed record PythonJobRequest(
    string ImportId,
    string ProjectId,
    string InputPath,
    string OutputDir,
    string Profile);

internal sealed record PythonJobResultEnvelope(
    bool Success,
    int TotalRows,
    int TotalColumns,
    string? ReportHtmlPath,
    string? NormalizedJsonPath,
    string? IssuesCsvPath,
    PythonJobMetricsEnvelope? Metrics,
    IReadOnlyList<PythonColumnStatsEnvelope>? Columns,
    IReadOnlyList<IReadOnlyDictionary<string, JsonElement>>? RecordsSample,
    IReadOnlyList<PythonIssueEnvelope>? Issues,
    [property: JsonPropertyName("detail")] string? Detail = null);

internal sealed record PythonJobMetricsEnvelope(
    int Duplicates,
    int NullNodeIds,
    string? DominantType,
    string? DominantNamespace);

internal sealed record PythonColumnStatsEnvelope(
    string Name,
    string NormalizedName,
    string DataTypeDetected,
    int DistinctCount,
    int NullCount);

internal sealed record PythonIssueEnvelope(
    string Severity,
    string Code,
    string Message,
    string? ColumnName,
    int? RowIndex,
    string? RawValue);
