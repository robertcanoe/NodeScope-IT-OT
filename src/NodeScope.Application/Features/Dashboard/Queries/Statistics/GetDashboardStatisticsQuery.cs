using MediatR;
using NodeScope.Application.Contracts.Dashboard;

namespace NodeScope.Application.Features.Dashboard.Queries.Statistics;

public sealed record GetDashboardStatisticsQuery(Guid OwnerUserId) : IRequest<DashboardStatisticsDto>;
