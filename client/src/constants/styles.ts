export const menuButtonSx = {
  width: '100%',
  minHeight: 48,
  whiteSpace: 'nowrap',
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
