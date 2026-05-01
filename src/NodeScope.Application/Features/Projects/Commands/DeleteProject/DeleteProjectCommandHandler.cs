using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;

namespace NodeScope.Application.Features.Projects.Commands.DeleteProject;

public sealed class DeleteProjectCommandHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<DeleteProjectCommand, bool>
{
    public async Task<bool> Handle(DeleteProjectCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aggregate = await dbContext.Projects
            .SingleOrDefaultAsync(p => p.Id == request.ProjectId && p.OwnerUserId == request.OwnerUserId, cancellationToken)
            .ConfigureAwait(false);

        if (aggregate is null)
        {
            return false;
        }

        dbContext.Projects.Remove(aggregate);
        _ = await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return true;
    }
}
