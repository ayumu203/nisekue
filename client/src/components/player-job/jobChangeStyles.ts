import { resolvePublicAssetPath } from '@/lib/assets'

// 舞台の背景（ダーマ神殿の祭壇）。基本職の舞台だけで使う
export const stageBackgroundSx = {
  backgroundColor: '#5a3c1a',
  backgroundImage: `url(${resolvePublicAssetPath('image/job-change/background.jpg')})`,
  backgroundSize: 'cover',
  backgroundPosition: 'center 30%',
  backgroundRepeat: 'no-repeat',
} as const

// 経験値の進み具合。レベル上限に達すると requiredExp は 0 になる
export function formatExpProgress(
  currentExp: number,
  requiredExp: number,
): { current: number; required: number; ratio: number } {
  const required = Math.max(0, requiredExp)
  const current = Math.max(0, currentExp)
  return {
    current,
    required,
    ratio: required > 0 ? Math.max(0, Math.min(100, (current / required) * 100)) : 100,
  }
}
