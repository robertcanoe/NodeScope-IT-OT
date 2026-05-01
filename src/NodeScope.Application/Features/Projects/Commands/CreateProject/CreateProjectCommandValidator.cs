using FluentValidation;

namespace NodeScope.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// FluentValidation blueprint for guarding project creation payloads.
/// </summary>
public sealed class CreateProjectCommandValidator : AbstractValidator<CreateProjectCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CreateProjectCommandValidator"/> class.
    /// </summary>
    public CreateProjectCommandValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();

        RuleFor(x => x.Payload!).NotNull();
        RuleFor(x => x.Payload!.Name).NotEmpty().MaximumLength(256);
        RuleFor(x => x.Payload!.Description!)
            .MaximumLength(4000)
            .When(x => x.Payload?.Description is not null);
        RuleFor(x => x.Payload!.SourceType).IsInEnum();
    }
}
