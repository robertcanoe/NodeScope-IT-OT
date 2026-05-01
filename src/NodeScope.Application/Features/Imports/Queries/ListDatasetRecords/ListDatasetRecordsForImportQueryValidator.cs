using FluentValidation;

namespace NodeScope.Application.Features.Imports.Queries.ListDatasetRecords;

public sealed class ListDatasetRecordsForImportQueryValidator : AbstractValidator<ListDatasetRecordsForImportQuery>
{
    public ListDatasetRecordsForImportQueryValidator()
    {
        RuleFor(x => x.OwnerUserId).NotEmpty();
        RuleFor(x => x.ImportJobId).NotEmpty();
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).LessThanOrEqualTo(50_000);
        RuleFor(x => x.PageSize).GreaterThanOrEqualTo(1).LessThanOrEqualTo(500);
    }
}
