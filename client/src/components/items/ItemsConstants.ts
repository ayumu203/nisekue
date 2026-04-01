export const deepGreen = '#1f4a33'
export const deepGreenBorder = '#2e6a49'
export const accentBeige = '#f0ddb5'
export const accentBeigeBorder = '#d4b57a'

export const itemInnerPanelSx = {
  backgroundColor: '#ffffff',
  borderColor: deepGreenBorder,
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.75)',
} as const

export const framedPanelSx = {
  borderRadius: 2,
  border: `1px solid ${deepGreenBorder}`,
  boxShadow: '0 8px 20px rgba(22, 44, 31, 0.14)',
} as const

export const listCardSx = {
  p: 1,
  borderRadius: 2,
  background: '#ffffff',
  borderColor: '#c6d6ca',
  boxShadow: '0 4px 12px rgba(22, 44, 31, 0.08)',
  transition: 'transform 140ms ease, box-shadow 140ms ease, border-color 140ms ease',
  '&:hover': {
    transform: 'translateY(-2px)',
    borderColor: deepGreenBorder,
    boxShadow: '0 10px 20px rgba(22, 44, 31, 0.14)',
  },
} as const

export const inputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: 2,
    backgroundColor: '#ffffff',
    '& fieldset': {
      borderColor: '#9bb89e',
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
    background: '#52785d',
    color: '#ffffff',
    boxShadow: 'none',
  },
  '&&:hover': {
    background: '#45684f',
    boxShadow: 'none',
  },
} as const

export const secondaryActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    borderColor: '#6f8f77',
    color: deepGreen,
    backgroundColor: '#ffffff',
    boxShadow: 'none',
  },
  '&&:hover': {
    borderColor: deepGreenBorder,
    backgroundColor: '#f4f7f4',
    boxShadow: 'none',
  },
} as const

export const destructiveActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    borderColor: '#b88d8d',
    color: '#7b4b4b',
    backgroundColor: '#ffffff',
    boxShadow: 'none',
  },
  '&&:hover': {
    borderColor: '#9e6f6f',
    backgroundColor: '#faf5f5',
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
    color: 'rgba(255,255,255,0.82)',
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
