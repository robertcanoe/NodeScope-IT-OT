using MediatR;
using NodeScope.Application.Contracts.Imports;

namespace NodeScope.Application.Features.Imports.Commands.CreateImport;

public sealed record CreateImportJobCommand(
    Guid OwnerUserId,
    Guid ProjectId,
    string OriginalFileName,
    string? ContentType,
    long ContentLength,
    Stream FileStream) : IRequest<ImportJobQueuedDto?>;
