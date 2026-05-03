using System.ComponentModel.DataAnnotations;

namespace NodeScope.Application.Configuration;

/// <summary>
/// Mirrors the <see cref="SectionName"/> host configuration subtree governing ingestion ergonomics shared by validation and infra runners.
/// </summary>
public sealed class ProcessingSettings
{
    /// <summary>
    /// ASP.NET binding key for processor + filesystem orchestration knobs.
    /// </summary>
    public const string SectionName = "Processing";

    /// <summary>
    /// Executable used when spawning python workers (<c>python3</c> on Linux deployments).
    /// </summary>
    [Required]
    public string PythonExecutable { get; set; } = "python3";

    /// <summary>
    /// Path to the Python processor entrypoint (for example <c>python/processor/main.py</c>).
    /// </summary>
    [Required]
    public string ProcessorScriptPath { get; set; } = "../../python/processor/main.py";

    /// <summary>
    /// Root enclosing <c>uploads/</c> and sibling artifact directories.
    /// </summary>
    [Required]
    public string StorageRoot { get; set; } = "storage";

    /// <summary>
    /// Hard ceiling guarding API upload surfaces and FluentValidation parity.
    /// </summary>
    [Range(1, long.MaxValue)]
    public long MaxUploadBytes { get; set; } = 50L * 1024 * 1024;
}
