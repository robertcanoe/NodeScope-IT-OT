using FluentValidation;

namespace NodeScope.Application.Features.Imports.Commands.ReprocessImport;

public sealed class ReprocessImportJobCommandValidator : AbstractValidator<ReprocessImportJobCommand>
{
    public ReprocessImportJobCommandValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.ImportJobId).NotEmpty();
    }
}
