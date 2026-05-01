using FluentValidation;

namespace NodeScope.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectCommandValidator()
    {
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();
    }
}
