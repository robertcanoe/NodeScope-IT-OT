using System.ComponentModel.DataAnnotations;

namespace NodeScope.Application.Configuration;

/// <summary>
/// Settings for the background ingestion orchestration loop.
/// </summary>
public sealed class ProcessingOrchestrationSettings
{
    /// <summary>
    /// ASP.NET binding key for orchestration delay values.
    /// </summary>
    public const string SectionName = "ProcessingOrchestration";

    /// <summary>
    /// Delay (ms) between iterations when work was processed.
    /// </summary>
    [Range(50, 60000)]
    public int ActiveDelayMilliseconds { get; set; } = 100;

    /// <summary>
    /// Delay (ms) between iterations when idle.
    /// </summary>
    [Range(100, 600000)]
    public int IdleDelayMilliseconds { get; set; } = 900;

    /// <summary>
    /// Backoff delay (seconds) after unexpected failures.
    /// </summary>
    [Range(1, 300)]
    public int FaultDelaySeconds { get; set; } = 3;
}
