/**
 * 見た目の規定値。文字サイズ・間隔はここから選ぶ（直書きは ESLint で検出する）。
 */

/** 文字サイズ（9 段階）。sx の fontSize にはこの値を使う */
export const font = {
  /** 10px */
  xxs: '0.625rem',
  /** 12px */
  xs: '0.75rem',
  /** 14px */
  sm: '0.875rem',
  /** 16px */
  md: '1rem',
  /** 18px */
  lg: '1.125rem',
  /** 20px */
  xl: '1.25rem',
  /** 24px */
  xxl: '1.5rem',
  /** 28px */
  xxxl: '1.75rem',
  /** 36px */
  display: '2.25rem',
} as const

export type FontSizeToken = (typeof font)[keyof typeof font]

/**
 * 間隔（10 段階）。MUI の spacing の倍率（1 = 8px）で、sx の p・m・gap や spacing にはこの数だけを書く。
 * ほかに使えるのは 0・'auto'・この値のマイナス。
 * 2・4・6・8・12・16・20・24・32・48px
 */
export const spaceScale = [0.25, 0.5, 0.75, 1, 1.5, 2, 2.5, 3, 4, 6] as const

/** 色（24 色）。色の直書きはせず、ここから選ぶ */
export const palette = {
  /** 白 */
  white: '#ffffff',
  /** 黒（暗幕・影・黒地） */
  black: '#000000',
  /** クリーム（紙） */
  cream: '#fff7e8',
  /** 水色（地） */
  sky: '#eaf3f8',
  /** 灰・淡 */
  gray300: '#d7d9d2',
  /** 灰 */
  gray600: '#8a8173',
  /** 茶・文字 */
  brown900: '#4f392c',
  /** 茶・中 */
  brown500: '#94704c',
  /** 茶・淡 */
  brown200: '#d9c6aa',
  /** 金・濃 */
  gold700: '#8a6a00',
  /** 金・中 */
  gold300: '#f2d27a',
  /** 黄 */
  yellow: '#ffcc00',
  /** 緑・濃 */
  green700: '#44644a',
  /** 緑 */
  green400: '#78c27d',
  /** 青緑・濃 */
  teal700: '#295d63',
  /** 青緑 */
  teal400: '#68b7a7',
  /** 紺（夜） */
  navy: '#1a0f33',
  /** 空色 */
  blue300: '#9cbbe6',
  /** 藍 */
  blue700: '#2f5d8a',
  /** 藍・濃 */
  blue900: '#1f3f60',
  /** 赤 */
  red700: '#b3261e',
  /** 赤・淡 */
  red300: '#d77a70',
  /** 紫 */
  purple: '#7a5bd0',
  /** ピンク（primary） */
  pink: '#f4a6c1',
} as const

export type PaletteColor = (typeof palette)[keyof typeof palette]

/** 半透明の不透明度（5 段階） */
export type Opacity = 0.1 | 0.25 | 0.5 | 0.75 | 0.9

/** 規定色を半透明にする（例：withAlpha(palette.white, 0.5)） */
export function withAlpha(color: PaletteColor, opacity: Opacity): string {
  const r = parseInt(color.slice(1, 3), 16)
  const g = parseInt(color.slice(3, 5), 16)
  const b = parseInt(color.slice(5, 7), 16)
  return `rgba(${r}, ${g}, ${b}, ${opacity})`
}
