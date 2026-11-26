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
