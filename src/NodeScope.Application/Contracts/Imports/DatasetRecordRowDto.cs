namespace NodeScope.Application.Contracts.Imports;

/// <summary>
/// Sampled dataset row with JSON payload preserved verbatim.
/// </summary>
public sealed record DatasetRecordRowDto(int RecordIndex, string PayloadJson);
