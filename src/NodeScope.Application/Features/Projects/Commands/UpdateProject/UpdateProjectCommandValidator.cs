using FluentValidation;
using NodeScope.Application.Contracts.Projects;

namespace NodeScope.Application.Features.Projects.Commands.UpdateProject;

public sealed class UpdateProjectCommandValidator : AbstractValidator<UpdateProjectCommand>
{
    public UpdateProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();

        RuleFor(x => x.Payload).NotNull();

        When(x => x.Payload is not null, () =>
        {
            RuleFor(x => x.Payload!.Name)
                .NotEmpty()
                .MaximumLength(200);

            RuleFor(x => x.Payload!.Description)
                .MaximumLength(4000)
                .When(static x => !string.IsNullOrWhiteSpace(x.Payload!.Description));
        });
    }
}
