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
