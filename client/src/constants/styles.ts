export const menuButtonSx = {
  width: '100%',
  minHeight: 48,
  whiteSpace: 'nowrap',
  justifyContent: 'center',
} as const

export const softGreenButtonSx = {
  '&&': {
    backgroundColor: '#78c27d',
    backgroundImage: 'none',
    borderColor: '#eef8ef',
    color: '#ffffff',
  },
  '&&:hover': {
    backgroundColor: '#69b46f',
    backgroundImage: 'none',
    borderColor: '#e4f3e6',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#b7d9b9',
    backgroundImage: 'none',
    borderColor: '#eef8ef',
    color: '#f8fff8',
  },
} as const

export const mutedGreenButtonSx = {
  '&&': {
    backgroundColor: '#e4f3e6',
    backgroundImage: 'none',
    borderColor: '#8dbe90',
    color: '#2f5b33',
  },
  '&&:hover': {
    backgroundColor: '#d5ebd7',
    backgroundImage: 'none',
    borderColor: '#78b27b',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#eef6ef',
    backgroundImage: 'none',
    borderColor: '#c6ddc8',
    color: '#8aa48c',
  },
} as const

export const softGoldButtonSx = {
  '&&': {
    backgroundColor: '#f2d27a',
    backgroundImage: 'none',
    border: '2px solid #8f6b2f',
    borderColor: '#8f6b2f',
    color: '#6a4b35',
    boxShadow: 'none',
  },
  '&&:hover': {
    backgroundColor: '#e4c062',
    backgroundImage: 'none',
    borderColor: '#7f5d26',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#ead9a7',
    backgroundImage: 'none',
    border: '2px solid #b59a69',
    borderColor: '#b59a69',
    color: '#927b56',
    boxShadow: 'none',
  },
} as const

export const mutedRedButtonSx = {
  '&&': {
    backgroundColor: '#fff1ef',
    backgroundImage: 'none',
    borderColor: '#d77a70',
    color: '#9f2f24',
  },
  '&&:hover': {
    backgroundColor: '#ffe4e0',
    backgroundImage: 'none',
    borderColor: '#c96256',
    boxShadow: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: '#f8ecea',
    backgroundImage: 'none',
    borderColor: '#dfb2ac',
    color: '#bf8b84',
  },
} as const

export const greenBadgeSx = {
  px: 1.5,
  py: 0.75,
  border: '1px solid',
  borderColor: '#eef8ef',
  borderRadius: 999,
  backgroundColor: '#78c27d',
} as const

export const greenBadgeTextSx = {
  color: '#ffffff',
  fontWeight: 700,
} as const

export const greenOutlinedInputSx = {
  '& .MuiOutlinedInput-root': {
    backgroundColor: '#fffdf8',
    '& fieldset': {
      borderColor: '#d9cfb8',
    },
    '&:hover fieldset': {
      borderColor: '#d1c4aa',
    },
    '&.Mui-focused fieldset': {
      borderColor: '#c8b894',
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: '#4f4638',
  },
} as const

export const playerHpBarSx = {
  height: 5,
  borderRadius: 999,
  backgroundColor: '#dcefdc',
  '& .MuiLinearProgress-bar': {
    backgroundColor: '#78c27d',
  },
} as const

export const playerMpBarSx = {
  height: 5,
  borderRadius: 999,
  backgroundColor: '#d9ebf6',
  '& .MuiLinearProgress-bar': {
    backgroundColor: '#4f9ed8',
  },
} as const

export const innerSurfaceSx = {
  backgroundColor: '#fff7e8',
  borderColor: '#e7d9b6',
} as const

export const outerPagePaperSx = {
  backgroundColor: '#ffcc00',
  border: '1px solid',
  borderColor: '#d3a93a',
  borderRadius: { xs: 3, sm: 4 },
  p: '2%',
} as const

export const twoColumnContentGridSx = {
  display: 'grid',
  gridTemplateColumns: {
    xs: '1fr',
    sm: 'minmax(180px, 260px) minmax(0, 1fr)',
    md: 'minmax(280px, 360px) minmax(0, 1fr)',
  },
  gap: { xs: 1.5, sm: 3 },
  alignItems: 'start',
} as const

export const topNavigationIconButtonSx = {
  width: 44,
  height: 44,
  border: '2px solid #ffffff',
  color: '#ffffff',
  backgroundColor: 'rgba(122, 77, 25, 0.9)',
  '&:hover': {
    backgroundColor: 'rgba(110, 68, 21, 0.94)',
  },
} as const
