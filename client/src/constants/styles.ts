export const menuButtonSx = {
  width: '100%',
  minHeight: 48,
  whiteSpace: 'nowrap',
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
  height: 8,
  borderRadius: 999,
  backgroundColor: '#dcefdc',
  '& .MuiLinearProgress-bar': {
    backgroundColor: '#78c27d',
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
  p: { xs: 2, sm: 4 },
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
