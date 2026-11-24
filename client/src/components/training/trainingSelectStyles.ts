import { trainingPanelColors } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'

// 相手カードと戦闘画面の背景。クエストのステージと同じ背景画像を使う
const trainingBackgroundPaths = [
  'image/quest/myu-town-battlefield.png',
  'image/quest/fuga-forest-battlefield.png',
  'image/quest/sakasaka-desert-battlefield.png',
  'image/quest/college-town-battlefield.png',
  'image/quest/ehi-park-battlefield.png',
  'image/quest/ougon-no-sato-battlefield.png',
  'image/quest/tenkai-gakudan-battlefield.png',
  'image/quest/trial-forest-battlefield.png',
  'image/quest/rush-rush-battlefield.png',
  'image/quest/last-sanctuary-battlefield.png',
  'image/quest/boss-rush-battlefield.png',
  'image/quest/void-armory-battlefield.png',
] as const

function hashKey(key: string | number): number {
  if (typeof key === 'number') {
    return Math.abs(key)
  }

  let hash = 0
  for (const char of key) {
    hash = (hash * 31 + char.charCodeAt(0)) >>> 0
  }
  return hash
}

// NPC は敵の id、PLAYER はプレイヤーの id から背景を決める（同じ相手はいつも同じ背景）
export function getTrainingBackgroundPath(key: string | number): string {
  return resolvePublicAssetPath(trainingBackgroundPaths[hashKey(key) % trainingBackgroundPaths.length])
}

// 相手カード（白地）。選んでいるカードは藍の枠と薄い藍の地で示す
export function trainingSelectCardSx(isSelected: boolean, isDisabled: boolean) {
  return {
    position: 'relative',
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    overflow: 'hidden',
    borderRadius: '12px',
    cursor: isDisabled ? 'default' : 'pointer',
    backgroundColor: isSelected ? trainingPanelColors.selectedSurface : '#fffdf8',
    boxShadow: '0 1px 0 #e6d6c4',
    outline: isSelected ? `3px solid ${trainingPanelColors.band}` : 'none',
    outlineOffset: -3,
  } as const
}

export const trainingSelectRadioSx = {
  position: 'absolute',
  top: 6,
  left: 6,
  zIndex: 1,
  p: 0.25,
  color: '#b9a98a',
  backgroundColor: '#ffffff',
  '&:hover': {
    backgroundColor: '#ffffff',
  },
  '&.Mui-checked': {
    color: trainingPanelColors.band,
  },
  '&.Mui-disabled': {
    color: '#d8ccb4',
  },
} as const

export const trainingLevelChipSx = {
  height: 24,
  fontWeight: 800,
  color: '#6b3a22',
  backgroundColor: '#f3e6d8',
} as const

// 一覧のマス目。PC は 4 列（戦闘結果の下では 8 列に詰める）、スマホは 2 列
export function trainingCardGridSx(compact: boolean) {
  return {
    display: 'grid',
    gap: compact ? 1.5 : 2,
    gridTemplateColumns: {
      xs: 'repeat(2, minmax(0, 1fr))',
      sm: 'repeat(3, minmax(0, 1fr))',
      md: compact ? 'repeat(auto-fill, minmax(132px, 1fr))' : 'repeat(4, minmax(0, 1fr))',
    },
  } as const
}
