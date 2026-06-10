export const cyberColors = {
  bg: '#050b07',
  panel: '#0a1410',
  panelLight: '#102018',
  panelBorder: 'rgba(0, 255, 136, 0.28)',
  accent: '#00ff88',
  accentDim: 'rgba(0, 255, 136, 0.55)',
  accentFaint: 'rgba(0, 255, 136, 0.1)',
  text: '#d6ffe9',
  textDim: 'rgba(214, 255, 233, 0.62)',
  danger: '#ff3860',
  warn: '#ffd166',
  mp: '#3ec5ff',
} as const

export const cyberPanelSx = {
  backgroundColor: cyberColors.panel,
  border: `1px solid ${cyberColors.panelBorder}`,
  borderRadius: 2.5,
  boxShadow: '0 0 18px rgba(0, 255, 136, 0.08)',
} as const

export const cyberButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 42,
    px: 2.5,
    fontWeight: 800,
    letterSpacing: '0.08em',
    color: '#031007',
    background: cyberColors.accent,
    boxShadow: '0 0 14px rgba(0, 255, 136, 0.45)',
  },
  '&&:hover': { background: '#33ffa1', boxShadow: '0 0 18px rgba(0, 255, 136, 0.6)' },
  '&&.Mui-disabled': { background: 'rgba(0, 255, 136, 0.16)', color: 'rgba(214, 255, 233, 0.38)', boxShadow: 'none' },
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
  '&&.Mui-disabled': { color: 'rgba(214, 255, 233, 0.3)', border: '1px solid rgba(0, 255, 136, 0.16)' },
} as const

export const cyberDangerButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    px: 2,
    fontWeight: 700,
    color: cyberColors.danger,
    border: '1px solid rgba(255, 56, 96, 0.55)',
    background: 'transparent',
    boxShadow: 'none',
  },
  '&&:hover': { background: 'rgba(255, 56, 96, 0.1)', border: `1px solid ${cyberColors.danger}` },
  '&&.Mui-disabled': { color: 'rgba(255, 56, 96, 0.3)', border: '1px solid rgba(255, 56, 96, 0.2)' },
} as const

export const cyberSelectSx = {
  color: cyberColors.text,
  backgroundColor: cyberColors.panelLight,
  borderRadius: 1.5,
  fontSize: '0.85rem',
  '& .MuiOutlinedInput-notchedOutline': { borderColor: cyberColors.panelBorder },
  '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: cyberColors.accentDim },
  '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: cyberColors.accent },
  '& .MuiSvgIcon-root': { color: cyberColors.accentDim },
} as const
