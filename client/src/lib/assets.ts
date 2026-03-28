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

const jobAssetFileNameByCode = {
  Apprentice: '00_apprentice.png',
  Warrior: '01_warior.png',
  Guardian: '02_guard.png',
  Mage: '03_mage.png',
  Ranger: '04_ranger.png',
  Priest: '05_priest.png',
} as const

export function resolveJobAssetPath(jobCode: string | null | undefined): string | null {
  if (!jobCode) {
    return null
  }

  const fileName = jobAssetFileNameByCode[jobCode as keyof typeof jobAssetFileNameByCode]
  if (!fileName) {
    return null
  }

  return resolvePublicAssetPath(`image/job/${fileName}`)
}
