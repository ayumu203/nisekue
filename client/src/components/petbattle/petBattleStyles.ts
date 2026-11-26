import { font, palette, withAlpha } from '@/theme/tokens'
export const cyberColors = {
  bg: palette.navy,
  panel: palette.brown900,
  panelLight: palette.brown900,
  panelBorder: withAlpha(palette.teal400, 0.25),
  accent: palette.teal400,
  accentDim: withAlpha(palette.teal400, 0.5),
  accentFaint: withAlpha(palette.teal400, 0.1),
  text: palette.sky,
  textDim: withAlpha(palette.sky, 0.75),
  danger: palette.red700,
  warn: palette.gold300,
  mp: palette.blue300,
} as const

export const cyberPanelSx = {
  backgroundColor: cyberColors.panel,
  border: `1px solid ${cyberColors.panelBorder}`,
  borderRadius: 2.5,
  boxShadow: `0 0 20px ${withAlpha(palette.navy, 0.25)}`,
} as const

export const cyberButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 42,
    px: 2.5,
    fontWeight: 800,
    letterSpacing: '0.08em',
    color: palette.brown900,
    background: cyberColors.accent,
    boxShadow: `0 0 12px ${withAlpha(palette.teal400, 0.25)}`,
  },
  '&&:hover': { background: palette.teal400, boxShadow: `0 0 16px ${withAlpha(palette.teal400, 0.5)}` },
  '&&.Mui-disabled': {
    background: withAlpha(palette.teal400, 0.25),
    color: withAlpha(palette.sky, 0.5),
    boxShadow: 'none',
  },
} as const

export const cyberOutlinedButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    px: 2,
    fontWeight: 700,
    color: cyberColors.accent,
    border: `1px solid ${cyberColors.accentDim}`,
    background: 'transparent',
    boxShadow: 'none',
  },
  '&&:hover': { background: cyberColors.accentFaint, border: `1px solid ${cyberColors.accent}` },
  '&&.Mui-disabled': { color: withAlpha(palette.sky, 0.25), border: `1px solid ${withAlpha(palette.teal400, 0.25)}` },
} as const

export const cyberDangerButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    px: 2,
    fontWeight: 700,
    color: cyberColors.danger,
    border: `1px solid ${withAlpha(palette.red700, 0.5)}`,
    background: 'transparent',
    boxShadow: 'none',
  },
  '&&:hover': { background: withAlpha(palette.red700, 0.1), border: `1px solid ${cyberColors.danger}` },
  '&&.Mui-disabled': { color: withAlpha(palette.red700, 0.25), border: `1px solid ${withAlpha(palette.red700, 0.25)}` },
} as const

export const cyberSelectSx = {
  color: cyberColors.text,
  backgroundColor: cyberColors.panelLight,
  borderRadius: 1.5,
  fontSize: font.sm,
  '& .MuiOutlinedInput-notchedOutline': { borderColor: cyberColors.panelBorder },
  '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: cyberColors.accentDim },
  '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: cyberColors.accent },
  '& .MuiSvgIcon-root': { color: cyberColors.accentDim },
} as const
