using FluentValidation;

namespace NodeScope.Application.Features.Imports.Queries.CompareImports;

public sealed class CompareImportsQueryValidator : AbstractValidator<CompareImportsQuery>
{
    public CompareImportsQueryValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.LeftImportId).NotEmpty();
        RuleFor(x => x.RightImportId).NotEmpty().NotEqual(x => x.LeftImportId).WithMessage("Select two different imports to compare.");
    }
}
