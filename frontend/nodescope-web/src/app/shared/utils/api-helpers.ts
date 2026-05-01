import type { PagedResult } from '../models/import.models';

/**
 * Normalizes outbound HTTP origins by trimming dangling slashes enforced by infra guidance.
 *
 * @param baseUrl Raw API host root.
 */
export function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, '');
}

/** Material paginator blows up when `totalCount`/shape is invalid or page index is stale after filters — normalize API payloads. */
export function coercePagedResult<T>(raw: unknown, fallbackPageSize: number): PagedResult<T> {
  const r = raw && typeof raw === 'object' ? (raw as Record<string, unknown>) : {};
  const itemsRaw = r['items'] ?? r['Items'];
  const items: T[] = Array.isArray(itemsRaw) ? (itemsRaw as T[]) : [];
  const totalRaw = r['totalCount'] ?? r['TotalCount'];
  const totalCount =
    typeof totalRaw === 'number' && Number.isFinite(totalRaw)
      ? totalRaw
      : Number(totalRaw);
  const pageSizeRaw = r['pageSize'] ?? r['PageSize'];
  const pageRaw = r['page'] ?? r['Page'];
  const pageSizeCandidate =
    typeof pageSizeRaw === 'number' && Number.isFinite(pageSizeRaw)
      ? pageSizeRaw
      : Number(pageSizeRaw);
  const pageSize =
    Number.isFinite(pageSizeCandidate) && pageSizeCandidate > 0 ? pageSizeCandidate : Math.max(1, fallbackPageSize || 25);
  const page =
    typeof pageRaw === 'number' && Number.isFinite(pageRaw) ? pageRaw : Number(pageRaw) || 1;
  const safeTotal = Number.isFinite(totalCount) ? Math.max(0, totalCount) : 0;
  return { items, totalCount: safeTotal, pageSize, page };
}
