namespace NodeScope.Application.Contracts.Common;

/// <summary>
/// Windowed page of items with total count for client-side pagination UX.
/// </summary>
public sealed record PagedResultDto<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
