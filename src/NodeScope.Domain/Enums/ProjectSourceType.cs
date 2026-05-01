namespace NodeScope.Domain.Enums;

/// <summary>
/// Describes the primary technical source associated with a project.
/// </summary>
public enum ProjectSourceType
{
    /// <summary>
    /// OPC UA node or address-space export.
    /// </summary>
    OpcUa = 0,

    /// <summary>
    /// Excel workbook describing signals or variables.
    /// </summary>
    ExcelSignals = 1,

    /// <summary>
    /// Generic delimited text data.
    /// </summary>
    GenericCsv = 2,

    /// <summary>
    /// Telemetry or log-oriented ingest.
    /// </summary>
    Logs = 3,
}
