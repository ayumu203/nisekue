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

export function resolveCharacterAssetPath(imagePath: string | null | undefined): string | null {
  if (!imagePath) {
    return null
  }

  const normalizedPath = imagePath.replace(/^\/+/, '')
  const characterPath = normalizedPath.startsWith('image/') ? normalizedPath : `image/character/${normalizedPath}`

  return resolvePublicAssetPath(characterPath)
}

export function resolveStatusAssetPath(fileName: string): string {
  const normalizedPath = fileName.replace(/^\/+/, '')
  const statusPath = normalizedPath.startsWith('image/') ? normalizedPath : `image/status/${normalizedPath}`

  return resolvePublicAssetPath(statusPath)
}
