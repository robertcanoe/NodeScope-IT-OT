using MediatR;
using Microsoft.EntityFrameworkCore;
using NodeScope.Application.Abstractions.Data;
using NodeScope.Application.Contracts.Auth;

namespace NodeScope.Application.Features.Auth.Queries.CurrentUser;

public sealed class CurrentUserQueryHandler(INodeScopeDbContext dbContext)
    : IRequestHandler<CurrentUserQuery, AuthenticatedUserSummaryDto?>
{
    public async Task<AuthenticatedUserSummaryDto?> Handle(CurrentUserQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var aggregate = await dbContext.Users.AsNoTracking()
            .Where(u => u.Id == request.UserId)
            .Select(u => new AuthenticatedUserSummaryDto(u.Id, u.Email, u.DisplayName, u.Role))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        return aggregate;
    }
}
