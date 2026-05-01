namespace NodeScope.Application.Contracts.Imports;

/// <summary>
/// Response payload sent immediately after an upload survives validation and persists as pending ingestion.
/// </summary>
/// <param name="ImportId">Future polling key for SPA progress surfaces.</param>
/// <param name="StatusText">Operational human-readable sentinel mirroring transactional commit state.</param>
public sealed record ImportJobQueuedDto(Guid ImportId, string StatusText);
