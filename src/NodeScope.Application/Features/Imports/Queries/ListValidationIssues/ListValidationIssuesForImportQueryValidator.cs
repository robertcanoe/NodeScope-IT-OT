using FluentValidation;

namespace NodeScope.Application.Features.Imports.Queries.ListValidationIssues;

public sealed class ListValidationIssuesForImportQueryValidator : AbstractValidator<ListValidationIssuesForImportQuery>
{
    public ListValidationIssuesForImportQueryValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.ImportJobId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).LessThanOrEqualTo(50_000);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(500);
    }
}
