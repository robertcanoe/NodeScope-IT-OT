/**
 * Normalizes outbound HTTP origins by trimming dangling slashes enforced by infra guidance.
 *
 * @param baseUrl Raw API host root.
 */
export function trimTrailingSlash(baseUrl: string): string {
  return baseUrl.replace(/\/+$/, '');
}
