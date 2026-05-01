using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NodeScope.Application.Common.Behaviors;
using NodeScope.Application.Configuration;

namespace NodeScope.Application;

/// <summary>
/// Composition helpers for registering application-layer services inside the host DI container.
/// </summary>
public static class DependencyInjection
{
    /// <summary>
    /// Registers CQRS primitives, FluentValidation scanners, and MediatR pipeline behaviors.
    /// </summary>
    /// <param name="services">The root service collection supplied by ASP.NET Core.</param>
    /// <returns>The same instance so calls can chain.</returns>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddOptions<ProcessingSettings>()
            .BindConfiguration(ProcessingSettings.SectionName)
            .Validate(settings => settings.MaxUploadBytes > 0, $"{ProcessingSettings.SectionName}: MaxUploadBytes must be positive.");

        var assembly = Assembly.GetExecutingAssembly();

        services.AddValidatorsFromAssembly(assembly);
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        return services;
    }
}
