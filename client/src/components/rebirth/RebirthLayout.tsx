export const rebirthPagePaperSx = {
  background:
    'linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(250,252,255,0.98) 38%, rgba(255,248,220,0.98) 100%)',
  border: '1px solid',
  borderColor: '#efe1a6',
  borderRadius: { xs: 3, sm: 4 },
  boxShadow: '0 20px 60px rgba(224, 196, 94, 0.18)',
  p: { xs: 2, sm: 4 },
} as const

export const rebirthSurfaceSx = {
  background:
    'linear-gradient(180deg, rgba(255,255,255,0.99) 0%, rgba(247,251,255,0.97) 56%, rgba(255,251,235,0.97) 100%)',
  borderColor: '#eadb9f',
  boxShadow: '0 8px 24px rgba(214, 186, 92, 0.12)',
} as const

export const rebirthPrimaryButtonSx = {
  '&&': {
    background: 'linear-gradient(135deg, #6ec8ff 0%, #7fb3ff 46%, #f5d96b 100%)',
    color: '#ffffff',
    border: '1px solid #cfe6ff',
    boxShadow: '0 10px 24px rgba(113, 176, 237, 0.28)',
  },
  '&&:hover': {
    background: 'linear-gradient(135deg, #5dbcf7 0%, #729ff0 46%, #efcd52 100%)',
    boxShadow: '0 12px 28px rgba(113, 176, 237, 0.34)',
  },
  '&&.Mui-disabled': {
    background: 'linear-gradient(135deg, #d7eaf8 0%, #dae6f6 55%, #f4ebc2 100%)',
    color: '#8ca3b9',
    border: '1px solid #d9e8f6',
  },
} as const

export const rebirthHintBadgeSx = {
  px: 1.5,
  py: 0.75,
  borderRadius: 999,
  border: '1px solid #eadb9f',
  background: 'linear-gradient(135deg, rgba(255, 234, 151, 0.3), rgba(150, 218, 255, 0.18))',
} as const

export const rebirthHintTextSx = {
  color: '#8a6a00',
  fontWeight: 700,
} as const

export const templeColumnSx = {
  position: 'relative',
  borderRadius: 3,
  border: '1px solid #e8dcab',
  background:
    'linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(248,251,255,0.95) 40%, rgba(255,249,230,0.94) 100%)',
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.9), 0 10px 24px rgba(200, 179, 92, 0.08)',
  px: { xs: 1.5, sm: 2 },
  py: { xs: 2, sm: 2.5 },
} as const

export const templeSectionTitleSx = {
  color: '#8a6a00',
  letterSpacing: '0.08em',
} as const

export const templeBodyTextSx = {
  color: 'text.secondary',
  fontSize: { xs: '0.875rem', md: '0.8125rem' },
  lineHeight: 1.55,
  whiteSpace: 'normal',
  textWrap: 'pretty',
  overflowWrap: 'anywhere',
} as const
