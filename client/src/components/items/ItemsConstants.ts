import { palette, withAlpha } from '@/theme/tokens'
export const deepGreen = palette.green700
export const deepGreenBorder = palette.green700
export const accentBeige = palette.brown200
export const accentBeigeBorder = palette.gold300

export const itemInnerPanelSx = {
  backgroundColor: palette.white,
  borderColor: deepGreenBorder,
  boxShadow: `inset 0 1px 0 ${withAlpha(palette.white, 0.75)}`,
} as const

export const framedPanelSx = {
  borderRadius: 2,
  border: `1px solid ${deepGreenBorder}`,
  boxShadow: `0 8px 20px ${withAlpha(palette.brown900, 0.1)}`,
} as const

export const listCardSx = {
  p: 1,
  borderRadius: 2,
  background: palette.white,
  borderColor: palette.gray300,
  boxShadow: `0 4px 12px ${withAlpha(palette.brown900, 0.1)}`,
  transition: 'transform 140ms ease, box-shadow 140ms ease, border-color 140ms ease',
  '&:hover': {
    transform: 'translateY(-2px)',
    borderColor: deepGreenBorder,
    boxShadow: `0 10px 20px ${withAlpha(palette.brown900, 0.1)}`,
  },
} as const

export const inputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: 2,
    backgroundColor: palette.white,
    '& fieldset': {
      borderColor: palette.teal400,
    },
    '&:hover fieldset': {
      borderColor: deepGreenBorder,
    },
    '&.Mui-focused fieldset': {
      borderColor: deepGreenBorder,
      borderWidth: 2,
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: deepGreen,
  },
} as const

export const primaryActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    background: palette.green700,
    color: palette.white,
    boxShadow: 'none',
  },
  '&&:hover': {
    background: palette.green700,
    boxShadow: 'none',
  },
} as const

export const secondaryActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    borderColor: palette.green700,
    color: deepGreen,
    backgroundColor: palette.white,
    boxShadow: 'none',
  },
  '&&:hover': {
    borderColor: deepGreenBorder,
    backgroundColor: palette.white,
    boxShadow: 'none',
  },
} as const

export const destructiveActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    borderColor: palette.gray600,
    color: palette.brown900,
    backgroundColor: palette.white,
    boxShadow: 'none',
  },
  '&&:hover': {
    borderColor: palette.gray600,
    backgroundColor: palette.white,
    boxShadow: 'none',
  },
} as const

export const tabsPaperSx = {
  p: 0.75,
  borderRadius: 2,
  border: `1px solid ${deepGreenBorder}`,
  backgroundColor: deepGreen,
} as const

export const tabsSx = {
  minHeight: 40,
  '& .MuiTab-root': {
    minHeight: 40,
    borderRadius: 1.5,
    fontWeight: 800,
    color: withAlpha(palette.white, 0.75),
    opacity: 1,
    transition: 'color 140ms ease, background-color 140ms ease',
    position: 'relative',
    zIndex: 1,
  },
  '& .MuiTab-root.Mui-selected': {
    color: `${deepGreen} !important`,
    backgroundColor: accentBeige,
  },
  '& .MuiTabs-indicator': {
    display: 'none',
  },
} as const
