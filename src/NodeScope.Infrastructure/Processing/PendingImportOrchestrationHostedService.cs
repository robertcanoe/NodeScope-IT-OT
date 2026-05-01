using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NodeScope.Application.Abstractions.Processing;

namespace NodeScope.Infrastructure.Processing;

/// <summary>
/// Background lease loop bridging hosted services to deterministic scoped ingestion pipelines suitable for workstation installations.
/// </summary>
/// <remarks>
/// Revisit with distributed locks when migrating to clustered orchestrators.
/// </remarks>
/// <param name="scopeFactory">Scoped factory spawning isolated relational contexts per ingestion attempt.</param>
/// <param name="logger">Structured observability façade.</param>
public sealed class PendingImportOrchestrationHostedService(IServiceScopeFactory scopeFactory, ILogger<PendingImportOrchestrationHostedService> logger)
    : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();

                var pipeline = scope.ServiceProvider.GetRequiredService<IImportAnalysisPipeline>();
                var processed = await pipeline.TryProcessNextPendingAsync(stoppingToken).ConfigureAwait(false);
                await Task.Delay(processed ? 100 : 900, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Pending import dispatcher cancelled during host shutdown orchestration.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pending import dispatcher loop faulted unexpectedly.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken).ConfigureAwait(false);
            }
        }
    }
}
