export const cyberColors = {
  bg: '#050505',
  panel: '#101111',
  panelLight: '#171919',
  panelBorder: 'rgba(138, 174, 158, 0.32)',
  accent: '#89b9a4',
  accentDim: 'rgba(137, 185, 164, 0.56)',
  accentFaint: 'rgba(137, 185, 164, 0.14)',
  text: '#e3ece7',
  textDim: 'rgba(227, 236, 231, 0.82)',
  danger: '#ff3860',
  warn: '#ffd166',
  mp: '#3ec5ff',
} as const

export const cyberPanelSx = {
  backgroundColor: cyberColors.panel,
  border: `1px solid ${cyberColors.panelBorder}`,
  borderRadius: 2.5,
  boxShadow: '0 0 20px rgba(0, 0, 0, 0.35)',
} as const

export const cyberButtonSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 42,
    px: 2.5,
    fontWeight: 800,
    letterSpacing: '0.08em',
    color: '#0a0d0c',
    background: cyberColors.accent,
    boxShadow: '0 0 12px rgba(137, 185, 164, 0.3)',
  },
  '&&:hover': { background: '#9fcab7', boxShadow: '0 0 16px rgba(137, 185, 164, 0.4)' },
  '&&.Mui-disabled': { background: 'rgba(137, 185, 164, 0.2)', color: 'rgba(227, 236, 231, 0.38)', boxShadow: 'none' },
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
  '&&.Mui-disabled': { color: 'rgba(227, 236, 231, 0.3)', border: '1px solid rgba(137, 185, 164, 0.2)' },
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
