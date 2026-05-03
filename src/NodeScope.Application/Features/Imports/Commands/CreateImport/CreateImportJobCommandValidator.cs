using FluentValidation;
using Microsoft.Extensions.Options;
using NodeScope.Application.Configuration;

namespace NodeScope.Application.Features.Imports.Commands.CreateImport;

/// <summary>
/// FluentValidation façade covering MIME-adjacent heuristics and upload ceilings originating from ASP.NET-bound <see cref="ProcessingSettings"/>.
/// </summary>
public sealed class CreateImportJobCommandValidator : AbstractValidator<CreateImportJobCommand>
{
    public CreateImportJobCommandValidator(IOptions<ProcessingSettings> options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ceiling = options.Value.MaxUploadBytes;

        RuleFor(x => x.ProjectId).NotEmpty();
        RuleFor(x => x.OwnerUserId).NotEmpty();

        RuleFor(x => x.OriginalFileName)
            .NotEmpty()
            .MaximumLength(512)
            .Must(BeKnownExtension).WithMessage("Only .csv, .xlsx, .xls or .json files are permitted.");

        RuleFor(x => x.ContentLength).GreaterThan(0).LessThanOrEqualTo(ceiling);
        RuleFor(x => x.ContentType)
            .NotEmpty()
            .WithMessage("ContentType must be provided for upload validation.");

        RuleFor(x => x)
            .Must(HasSupportedContentType)
            .WithMessage("Only CSV, Excel or JSON content types are permitted.");
    }

    private static bool BeKnownExtension(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension is ".csv" or ".xlsx" or ".xls" or ".json";
    }

    private static bool HasSupportedContentType(CreateImportJobCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.ContentType))
        {
            return false;
        }

        var normalized = command.ContentType.Trim().ToLowerInvariant();
        var extension = Path.GetExtension(command.OriginalFileName).ToLowerInvariant();
        return normalized switch
        {
            "text/csv" => extension == ".csv",
            "application/vnd.ms-excel" => extension is ".xls" or ".csv",
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" => extension == ".xlsx",
            "application/json" or "text/json" => extension == ".json",
            _ => false,
        };
    }
}
