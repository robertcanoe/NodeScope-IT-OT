using Microsoft.Extensions.DependencyInjection;
using NodeScope.Application.Abstractions.Authentication;
using NodeScope.Application.Abstractions.Files;
using NodeScope.Application.Abstractions.Processing;
using NodeScope.Infrastructure.Files;
using NodeScope.Infrastructure.Identity;
using NodeScope.Infrastructure.Processing;

namespace NodeScope.Infrastructure;

/// <summary>
/// Registers reusable infrastructure primitives (cryptography adapters, eventual messaging, etc.).
/// </summary>
public static class InfrastructureDependencyInjection
{
    /// <summary>
    /// Adds NodeScope Infrastructure services to the application's DI container.
    /// </summary>
    /// <param name="services">Host-managed service registrations.</param>
    /// <param name="includeProcessingHostedService">
    /// When <see langword="true"/>, registers the background ingestion dispatcher hosted service.
    /// </param>
    /// <returns>The same registry for fluent composition.</returns>
    public static IServiceCollection AddNodeScopeInfrastructureServices(
        this IServiceCollection services,
        bool includeProcessingHostedService = true)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IUserPasswordHasher, AspNetCompatibleUserPasswordHasher>();
        services.AddSingleton<IImportFileStorage, LocalImportFileStorage>();
        services.AddScoped<IImportArtifactPathResolver, ImportArtifactPathResolver>();
        services.AddScoped<IImportAnalysisPipeline, ImportDatasetAnalysisPipeline>();
        if (includeProcessingHostedService)
        {
            services.AddHostedService<PendingImportOrchestrationHostedService>();
        }
        return services;
    }
}
