using FluentValidation;

namespace NodeScope.Application.Features.Auth.Commands.Login;

/// <summary>
/// Validation rules guarding login payloads before hitting persistence or cryptography workloads.
/// </summary>
public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LoginCommandValidator"/> class.
    /// </summary>
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request).NotNull().DependentRules(() =>
        {
            RuleFor(x => x.Request!.Email).NotEmpty().EmailAddress().MaximumLength(320);
            RuleFor(x => x.Request!.Password).NotEmpty().MinimumLength(8).MaximumLength(256);
        });
    }
}
