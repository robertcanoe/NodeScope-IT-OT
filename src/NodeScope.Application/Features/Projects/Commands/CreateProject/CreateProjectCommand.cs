using MediatR;
using NodeScope.Application.Contracts.Projects;
using NodeScope.Domain.Enums;

namespace NodeScope.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Creates a persisted analysis workspace anchored to an authenticated owner's identity.
/// </summary>
/// <param name="OwnerUserId">Subject identifier originating from bearer claims (<c>sub</c>).</param>
/// <param name="Payload">Validated client payload mirrored from SPA forms.</param>
public sealed record CreateProjectCommand(Guid OwnerUserId, CreateProjectDto Payload)
    : IRequest<ProjectResponseDto>;
