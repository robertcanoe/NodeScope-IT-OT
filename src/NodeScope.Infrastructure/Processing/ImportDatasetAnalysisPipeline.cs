using System.Diagnostics;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Application.Abstractions.Processing;
using NodeScope.Application.Configuration;
using NodeScope.Domain.Entities;
using NodeScope.Domain.Enums;
using NodeScope.Infrastructure.Data;

namespace NodeScope.Infrastructure.Processing;

/// <summary>
/// Leases queued imports, executes python analytics, then atomically persists derived projections.
/// </summary>
public sealed class ImportDatasetAnalysisPipeline(
    AppDbContext dbContext,
    IImportFileStorage fileStorage,
    IHostEnvironment environment,
    IOptionsMonitor<ProcessingSettings> optionsMonitor,
    ILogger<ImportDatasetAnalysisPipeline> logger)
    : IImportAnalysisPipeline
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public async Task<bool> TryProcessNextPendingAsync(CancellationToken cancellationToken)
    {
        Guid jobId;

        await using (var leaseTransaction = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
        {
            var candidateId = await dbContext.ImportJobs
                .Where(j => j.Status == ImportJobStatus.Pending)
                .OrderBy(j => j.Id)
                .Select(j => j.Id)
                .FirstOrDefaultAsync(cancellationToken)
                .ConfigureAwait(false);

            if (candidateId == Guid.Empty)
            {
                await leaseTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return false;
            }

            var startedAtUtc = DateTimeOffset.UtcNow;
            var updated = await dbContext.ImportJobs
                .Where(j => j.Id == candidateId && j.Status == ImportJobStatus.Pending)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(j => j.Status, ImportJobStatus.Processing)
                        .SetProperty(j => j.StartedAt, startedAtUtc),
                    cancellationToken)
                .ConfigureAwait(false);

            if (updated == 0)
            {
                await leaseTransaction.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return true;
            }

            jobId = candidateId;
            await leaseTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
        }

        try
        {
            var job = await dbContext.ImportJobs
                .Include(j => j.Project)
                .SingleAsync(j => j.Id == jobId, cancellationToken)
                .ConfigureAwait(false);

            if (job.Project is null)
            {
                throw new InvalidOperationException($"Import job {jobId} is missing an attached project aggregate.");
            }

            var settings = optionsMonitor.CurrentValue;
            var root = ResolvePhysicalStorageRoot(settings);
            var inputFullPath = Path.GetFullPath(CombineRelative(root, job.StoredFilePath));

            if (!File.Exists(inputFullPath))
            {
                logger.LogError("Uploaded payload missing under {PhysicalPath}", inputFullPath);
                await PersistFailureAsync(
                        jobId,
                        $"Uploaded payload not found at '{inputFullPath}'.",
                        cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }

            var artifactDir = Path.GetFullPath(fileStorage.GetArtifactsDirectory(job.ProjectId, job.Id));
            Directory.CreateDirectory(artifactDir);
            var requestPath = Path.Combine(artifactDir, "pipeline-request.json");
            await PersistPythonRequestPayloadAsync(job, inputFullPath, artifactDir, requestPath).ConfigureAwait(false);

            var scriptPhysicalPath = ResolveScriptPhysicalPath(settings);
            if (!File.Exists(scriptPhysicalPath))
            {
                logger.LogError("Python script missing under {PhysicalPath}", scriptPhysicalPath);
                await PersistFailureAsync(
                        jobId,
                        $"Python processor script not found at '{scriptPhysicalPath}'.",
                        cancellationToken)
                    .ConfigureAwait(false);
                return true;
            }
            using var subprocess = LaunchPythonInterpreter(settings.PythonExecutable.Trim(), scriptPhysicalPath, requestPath);

            var stdoutReader = subprocess.StandardOutput.ReadToEndAsync(cancellationToken);
            var stderrReader = subprocess.StandardError.ReadToEndAsync(cancellationToken);

            await subprocess.WaitForExitAsync(cancellationToken).ConfigureAwait(false);
            await Task.WhenAll(stdoutReader, stderrReader).ConfigureAwait(false);

            logger.LogInformation("Python exited with code {Code} stdoutLength={StdoutLen}", subprocess.ExitCode, stdoutReader.Result.Length);

            var stderrDiagnostics = stderrReader.Result;
            if (!string.IsNullOrWhiteSpace(stderrDiagnostics))
            {
                logger.LogWarning("Python emitted diagnostics ({JobId}): {Diagnostics}", jobId, stderrDiagnostics);
            }

            var pipelineResultsPath = Path.Combine(artifactDir, "pipeline-result.json");
            PythonJobResultEnvelope? decoded = null;
            if (File.Exists(pipelineResultsPath))
            {
                await using var resultStream = File.OpenRead(pipelineResultsPath);
                decoded = await JsonSerializer
                    .DeserializeAsync<PythonJobResultEnvelope>(resultStream, SerializerOptions, cancellationToken)
                    .ConfigureAwait(false);
            }

            if (subprocess.ExitCode != 0 || decoded is null || !decoded.Success)
            {
                var failureSummary = ComposePythonFailureSummary(subprocess.ExitCode, stderrDiagnostics, decoded);
                logger.LogError("Python ingestion failed ({JobId}): {Reason}", jobId, failureSummary);
                await PersistFailureAsync(jobId, failureSummary, cancellationToken).ConfigureAwait(false);
                return true;
            }

            await using (
                var workTransaction =
                    await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false))
            {
                var trackedJob = await dbContext.ImportJobs
                    .SingleAsync(j => j.Id == jobId, cancellationToken)
                    .ConfigureAwait(false);

                await PurgeDerivedDataAsync(trackedJob.Id, cancellationToken).ConfigureAwait(false);
                HydrateArtifacts(trackedJob, decoded, artifactDir, cancellationToken);
                await ApplyCompletionAsync(trackedJob, decoded, artifactDir, cancellationToken).ConfigureAwait(false);

                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                await workTransaction.CommitAsync(cancellationToken).ConfigureAwait(false);
            }

            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception while analysing import {JobId}", jobId);
            await PersistFailureAsync(jobId, ex.ToString(), CancellationToken.None).ConfigureAwait(false);
            return true;
        }
    }

    private Task PersistPythonRequestPayloadAsync(ImportJob job, string intakePhysicalPath, string outputDir, string requestPath)
    {
        var payload =
            new PythonJobRequest(job.Id.ToString("D"), job.ProjectId.ToString("D"), intakePhysicalPath, outputDir, "opcua-default");
        var serialized = JsonSerializer.Serialize(payload, SerializerOptions);
        return File.WriteAllTextAsync(requestPath, serialized);
    }

    private Process LaunchPythonInterpreter(string pythonExecutable, string scriptPath, string requestPath)
    {
        var psi = new ProcessStartInfo
        {
            FileName = pythonExecutable,
            ArgumentList = { scriptPath, requestPath },
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(scriptPath) ?? Directory.GetCurrentDirectory(),
        };

        var process = new Process { StartInfo = psi };

        process.Start();

        return process;
    }

    private async Task PurgeDerivedDataAsync(Guid jobId, CancellationToken cancellationToken)
    {
        _ =
            await dbContext.DatasetRecords
                .Where(r => r.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        _ =
            await dbContext.DatasetColumns
                .Where(c => c.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        _ =
            await dbContext.ValidationIssues
                .Where(i => i.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);
        _ =
            await dbContext.GeneratedArtifacts
                .Where(a => a.ImportJobId == jobId)
                .ExecuteDeleteAsync(cancellationToken)
                .ConfigureAwait(false);

        await Task.CompletedTask.ConfigureAwait(false);
    }

    private void HydrateArtifacts(ImportJob job, PythonJobResultEnvelope envelope, string artifactDirectory, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        foreach (var profile in envelope.Columns ?? Array.Empty<PythonColumnStatsEnvelope>())
        {
            dbContext.DatasetColumns.Add(
                new DatasetColumn(job.Id, profile.Name, profile.NormalizedName, profile.DataTypeDetected, profile.DistinctCount, profile.NullCount));
        }

        var recordIndexCursor = 0;
        foreach (var row in envelope.RecordsSample ?? Array.Empty<IReadOnlyDictionary<string, JsonElement>>())
        {
            var payloadJson = JsonSerializer.Serialize(row, SerializerOptions);
            dbContext.DatasetRecords.Add(new DatasetRecord(job.Id, recordIndexCursor, payloadJson));
            recordIndexCursor++;
        }

        var detectedAtUtc = DateTimeOffset.UtcNow;
        foreach (var issue in envelope.Issues ?? Array.Empty<PythonIssueEnvelope>())
        {
            dbContext.ValidationIssues.Add(
                new ValidationIssue(
                    job.Id,
                    ParseSeverityLabel(issue.Severity),
                    issue.Code,
                    issue.Message,
                    issue.ColumnName,
                    issue.RowIndex,
                    issue.RawValue,
                    detectedAtUtc));
        }

        PersistGeneratedArtifacts(job.Id, artifactDirectory, envelope, detectedAtUtc);
    }

    private Task ApplyCompletionAsync(ImportJob job, PythonJobResultEnvelope envelope, string artifactDirectory, CancellationToken cancellationToken)
    {
        _ = cancellationToken;

        var serializedSummaryBlob =
            JsonSerializer.Serialize(new { envelope.Metrics, totalColumns = envelope.TotalColumns }, SerializerOptions);

        var issueTotal = envelope.Issues?.Count ?? 0;
        var htmlPathNormalized = NormalizeArtifactPath(envelope.ReportHtmlPath, artifactDirectory);
        var jsonPathNormalized = NormalizeArtifactPath(envelope.NormalizedJsonPath, artifactDirectory);

        job.MarkCompleted(
            processorVersion: "python-analytics:v1",
            rowCount: envelope.TotalRows,
            issueCount: issueTotal,
            reportHtmlPath: htmlPathNormalized,
            normalizedJsonPath: jsonPathNormalized,
            summaryJson: serializedSummaryBlob);

        return Task.CompletedTask;
    }

    private void PersistGeneratedArtifacts(Guid jobId, string artifactDirectory, PythonJobResultEnvelope envelope, DateTimeOffset createdAtUtc)
    {
        void Track(string? pathFragment, GeneratedArtifactType type, string mime)
        {
            if (string.IsNullOrWhiteSpace(pathFragment))
            {
                return;
            }

            var fullPath = NormalizeArtifactPhysicalPath(pathFragment, artifactDirectory);
            var probe = new FileInfo(fullPath);
            if (!probe.Exists)
            {
                return;
            }

            dbContext.GeneratedArtifacts.Add(
                new GeneratedArtifact(jobId, type, fullPath.Replace('\\', '/'), mime, probe.Length, createdAtUtc));
        }

        Track(envelope.ReportHtmlPath, GeneratedArtifactType.ReportHtml, "text/html");
        Track(envelope.NormalizedJsonPath, GeneratedArtifactType.NormalizedJson, "application/json");
        Track(envelope.IssuesCsvPath, GeneratedArtifactType.IssuesCsv, "text/csv");
    }

    private static string NormalizeArtifactPhysicalPath(string pathFragment, string artifactDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pathFragment);

        if (Path.IsPathFullyQualified(pathFragment))
        {
            return Path.GetFullPath(pathFragment);
        }

        var combined =
            Path.GetFullPath(
                Path.Combine(artifactDirectory, pathFragment.TrimStart('/', '\\')));
        return combined;
    }

    private static string? NormalizeArtifactPath(string? pathFragment, string artifactDirectory)
    {
        return string.IsNullOrWhiteSpace(pathFragment)
            ? null
            : NormalizeArtifactPhysicalPath(pathFragment!, artifactDirectory).Replace('\\', '/');
    }

    private static string ComposePythonFailureSummary(int exitCode, string stderr, PythonJobResultEnvelope? decoded)
    {
        StringBuilder sb = new();
        sb.Append("Python ingestion failed (exit code ").Append(exitCode).Append(')');
        if (!string.IsNullOrWhiteSpace(decoded?.Detail))
        {
            sb.Append(". ").Append(decoded.Detail.Trim());
        }
        else if (decoded is { Success: false })
        {
            sb.Append(". The pipeline reported success=false.");
        }

        if (decoded is null)
        {
            sb.Append(". pipeline-result.json was missing or invalid.");
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            var clip = stderr.Trim();
            const int max = 4000;
            if (clip.Length > max)
            {
                clip = string.Concat(clip.AsSpan(0, max), "…");
            }

            sb.Append(" Stderr: ").Append(clip);
        }

        return sb.ToString();
    }

    private async Task PersistFailureAsync(Guid jobId, string? failureMessage, CancellationToken cancellationToken)
    {
        await using var trx = await dbContext.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var tracked = await dbContext.ImportJobs
                .SingleOrDefaultAsync(j => j.Id == jobId && j.Status == ImportJobStatus.Processing, cancellationToken)
                .ConfigureAwait(false);

            if (tracked is null)
            {
                await trx.RollbackAsync(cancellationToken).ConfigureAwait(false);
                return;
            }

            tracked.MarkFailed(failureMessage);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            await trx.CommitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await trx.RollbackAsync(cancellationToken).ConfigureAwait(false);
            throw;
        }
    }

    private string ResolvePhysicalStorageRoot(ProcessingSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.StorageRoot);

        return Path.IsPathFullyQualified(settings.StorageRoot.Trim())
            ? settings.StorageRoot.Trim()
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath.Trim(), settings.StorageRoot.Trim()));
    }

    private string ResolveScriptPhysicalPath(ProcessingSettings settings)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(settings.ProcessorScriptPath);

        return Path.IsPathFullyQualified(settings.ProcessorScriptPath.Trim())
            ? Path.GetFullPath(settings.ProcessorScriptPath.Trim())
            : Path.GetFullPath(Path.Combine(environment.ContentRootPath.Trim(), settings.ProcessorScriptPath.Trim()));
    }

    private static string CombineRelative(string root, string relativeLogicalPathStored)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativeLogicalPathStored);

        return Path.GetFullPath(
            Path.Combine(root, relativeLogicalPathStored.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static ValidationSeverity ParseSeverityLabel(string label)
    {
        return Enum.TryParse<ValidationSeverity>(label, ignoreCase: true, out var parsed) ? parsed : ValidationSeverity.Warning;
    }
}
