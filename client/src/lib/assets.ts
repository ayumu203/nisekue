export function resolvePublicAssetPath(path: string): string {
  if (!path) {
    return path
  }

  if (/^https?:\/\//i.test(path)) {
    return path
  }

  const normalizedPath = path.replace(/^\/+/, '')
  const baseUrl = (import.meta.env.BASE_URL ?? '/').replace(/\/+$/, '')
  return `${baseUrl}/${normalizedPath}`.replace(/\/{2,}/g, '/')
}
