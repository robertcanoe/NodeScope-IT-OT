using FluentValidation;
using MediatR;

namespace NodeScope.Application.Common.Behaviors;

/// <summary>
/// Ensures FluentValidation executes before invoking MediatR request handlers.
/// </summary>
/// <typeparam name="TRequest">MediatR request envelope.</typeparam>
/// <typeparam name="TResponse">Handler response projection.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    /// <inheritdoc />
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(next);

        if (!validators.Any())
        {
            return await next(cancellationToken).ConfigureAwait(false);
        }

        var context = new ValidationContext<TRequest>(request);

        var validationResults =
            await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, cancellationToken)))
                .ConfigureAwait(false);

        var failures = validationResults.Where(r => !r.IsValid).SelectMany(r => r.Errors).ToList();
        if (failures.Count > 0)
        {
            throw new ValidationException(failures);
        }

        return await next(cancellationToken).ConfigureAwait(false);
    }
}
