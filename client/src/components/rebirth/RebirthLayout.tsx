import { palette, withAlpha } from '@/theme/tokens'
// 画面いっぱいに広げた外枠（GamePageFrame）の背景
export const rebirthPageBackground = `linear-gradient(180deg, ${palette.white} 0%, ${palette.white} 38%, ${palette.cream} 100%)`

export const rebirthSurfaceSx = {
  background: `linear-gradient(180deg, ${palette.white} 0%, ${palette.white} 56%, ${palette.cream} 100%)`,
  borderColor: palette.gold300,
  boxShadow: `0 8px 24px ${withAlpha(palette.gold300, 0.1)}`,
} as const

export const rebirthPrimaryButtonSx = {
  '&&': {
    background: `linear-gradient(135deg, ${palette.blue300} 0%, ${palette.blue300} 46%, ${palette.gold300} 100%)`,
    color: palette.white,
    border: `1px solid ${palette.sky}`,
    boxShadow: `0 10px 24px ${withAlpha(palette.blue300, 0.25)}`,
  },
  '&&.Mui-disabled': {
    background: `linear-gradient(135deg, ${palette.sky} 0%, ${palette.sky} 55%, ${palette.brown200} 100%)`,
    color: palette.blue300,
    border: `1px solid ${palette.sky}`,
  },
} as const

export const templeColumnSx = {
  position: 'relative',
  borderRadius: 3,
  border: `1px solid ${palette.brown200}`,
  background: `linear-gradient(180deg, ${palette.white} 0%, ${palette.white} 40%, ${withAlpha(palette.cream, 0.9)} 100%)`,
  boxShadow: `inset 0 1px 0 ${withAlpha(palette.white, 0.9)}, 0 10px 24px ${withAlpha(palette.gold300, 0.1)}`,
  px: { xs: 1.5, sm: 2 },
  py: { xs: 2, sm: 2.5 },
} as const

export const templeSectionTitleSx = {
  color: palette.gold700,
  letterSpacing: '0.08em',
} as const
