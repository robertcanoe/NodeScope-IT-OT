using MediatR;

namespace NodeScope.Application.Features.Imports.Commands.ReprocessImport;

public sealed record ReprocessImportJobCommand(Guid OwnerUserId, Guid ProjectId, Guid ImportJobId)
    : IRequest<ReprocessImportJobResult>;
