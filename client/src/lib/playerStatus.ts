// ステータス欄で使う表示用の整形（ホームのステータス・訓練画面などで共通）

export function toNormalized(value: number | undefined, maxValue: number): number {
  if (typeof value !== 'number' || maxValue <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (value / maxValue) * 100))
}

export function formatExpProgress(exp: number | undefined, requiredExp: number | undefined, fallback: string): string {
  if (typeof exp !== 'number' || typeof requiredExp !== 'number') {
    return `${fallback} / ${fallback}`
  }

  // レベル上限に達すると requiredExp は 0 になる。値としては正しいので fallback にはしない。
  if (!Number.isFinite(exp) || !Number.isFinite(requiredExp) || requiredExp < 0) {
    return `${fallback} / ${fallback}`
  }

  return `${exp} / ${requiredExp}`
}

// 次のレベルまでの進み具合（0〜100）。レベル上限のときは 100 にする
export function expProgressPercent(exp: number | undefined, requiredExp: number | undefined, isMaxLevel: boolean) {
  if (isMaxLevel) {
    return 100
  }

  if (typeof exp !== 'number' || typeof requiredExp !== 'number' || requiredExp <= 0) {
    return 0
  }

  return toNormalized(exp, requiredExp)
}

export function formatStatusValue(
  baseValue: number | undefined,
  effectiveValue: number | undefined,
  fallback: string,
): string | number {
  if (typeof baseValue !== 'number' || typeof effectiveValue !== 'number') {
    return fallback
  }

  const diff = effectiveValue - baseValue
  if (diff === 0) {
    return effectiveValue
  }

  return `${effectiveValue} (${diff > 0 ? '+' : ''}${diff})`
}

export function formatEquippedItemWithPlus(
  equipment:
    | {
        name: string
        plusValue: number
      }
    | undefined,
  fallback: string,
): string {
  if (!equipment) {
    return fallback
  }

  return equipment.plusValue > 0 ? `${equipment.name} (+${equipment.plusValue})` : equipment.name
}
