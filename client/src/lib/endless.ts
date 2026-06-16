// エンドレスモードの表示ヘルパー（テーマ名解決・次ボスまでのフロア数）。

export function resolveEndlessThemeName(
  themeNo: number | null | undefined,
  themeNames: readonly string[],
): string | null {
  if (themeNo == null || themeNames.length === 0) {
    return null
  }

  // フロア50超はテーマ循環するため、テーマ番号もテーマ数で巡回させる。
  const index = (themeNo - 1) % themeNames.length
  return themeNames[index] ?? null
}

// テーマ帯ごとの戦場背景（フロア50超はテーマ循環に合わせて巡回）。
// 1:そよかぜ草原 2:ざわめき森 3:みぞれ峠 4:ふぶき山 5:あらしの果て
const ENDLESS_THEME_BATTLEFIELDS = [
  'image/quest/riverbank-battlefield.svg',
  'image/quest/enchanted-forest-battlefield.svg',
  'image/quest/crystal-cavern-battlefield.svg',
  'image/quest/floating-islands-battlefield.svg',
  'image/quest/hell-battlefield.svg',
] as const

export function resolveEndlessBattlefield(themeNo: number | null | undefined): string | null {
  if (themeNo == null) {
    return null
  }

  const index = (themeNo - 1) % ENDLESS_THEME_BATTLEFIELDS.length
  return ENDLESS_THEME_BATTLEFIELDS[index] ?? null
}

// 現在フロアから次のボスフロアまでの残りフロア数。現在がボスフロアなら 0。
export function resolveFloorsToNextBoss(
  currentFloorNo: number,
  bossInterval: number | null | undefined,
): number | null {
  if (bossInterval == null || bossInterval <= 0) {
    return null
  }

  const remainder = currentFloorNo % bossInterval
  return remainder === 0 ? 0 : bossInterval - remainder
}
