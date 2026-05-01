using MediatR;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Projects;
using NodeScope.Domain.Entities;

namespace NodeScope.Application.Features.Projects.Commands.CreateProject;

/// <summary>
/// Materializes aggregate roots for multi-tenant ingestion workspaces scoped to JWT subjects.
/// </summary>
/// <param name="dbContext">Infrastructure resolved EF Core façade.</param>
public sealed class CreateProjectCommandHandler(INodeScopeDbContext dbContext) : IRequestHandler<CreateProjectCommand, ProjectResponseDto>
{
    /// <inheritdoc />
    public async Task<ProjectResponseDto> Handle(CreateProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(request.Payload);

        var aggregate = new Project(
            request.OwnerUserId,
            request.Payload.Name,
            request.Payload.Description,
            request.Payload.SourceType);

        dbContext.Projects.Add(aggregate);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new ProjectResponseDto(
            aggregate.Id,
            aggregate.OwnerUserId,
            aggregate.Name,
            aggregate.Description,
            aggregate.SourceType,
            aggregate.CreatedAt,
            aggregate.UpdatedAt);
    }
}
