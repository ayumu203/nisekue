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

export function normalizeCharacterPath(imagePath: string): string {
  const normalizedPath = imagePath.replace(/^\/+/, '')
  return normalizedPath.startsWith('image/') ? normalizedPath : `image/character/${normalizedPath}`
}

export function resolveCharacterAssetPath(imagePath: string | null | undefined): string | null {
  if (!imagePath) {
    return null
  }

  return resolvePublicAssetPath(normalizeCharacterPath(imagePath))
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
  OniWarrior: '06_oniwarrior.png',
  SwordMaster: '07_swordmaster.png',
  Trickster: '08_trickster.png',
  Crusader: '09_crusader.png',
  FireMage: '10_firemage.png',
  WaterMage: '11_watermage.png',
  WindMage: '12_windmage.png',
  HighPriest: '13_highpriest.png',
  Necromancer: '14_necromancer.png',
  Sniper: '15_sniper.png',
  TrapMaster: '16_trapmaster.png',
  GrandWarrior: '17_grandwarrior.png',
  GrandGuard: '18_grandguard.png',
  GrandCaster: '19_grandcaster.png',
  GrandPriest: '20_grandpriest.png',
  GrandRanger: '21_grandranger.png',
  Shogun: '22_shogun.png',
  Archmage: '23_archmage.png',
  GreatThief: '24_greatthief.png',
  Bushin: '25_bushin.png',
  Seikaiou: '26_seikaiou.png',
  Matouou: '27_matouou.png',
  Shugoshin: '28_shugoshin.png',
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
