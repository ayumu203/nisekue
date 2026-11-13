export function trainingSelectCardSx(isSelected: boolean, isDisabled: boolean) {
  return {
    height: '100%',
    display: 'flex',
    flexDirection: 'column',
    cursor: isDisabled ? 'default' : 'pointer',
    color: '#f5f0df',
    backgroundColor: isSelected ? '#553637' : '#382526',
    border: 'none',
    boxShadow: 'inset 0 1px 0 rgba(255, 227, 214, 0.08)',
    transition: 'background-color 120ms ease',
    '&:hover': {
      backgroundColor: isDisabled ? undefined : isSelected ? '#553637' : '#432c2d',
    },
  } as const
}

export const trainingSelectRadioSx = {
  position: 'absolute',
  top: 4,
  left: 4,
  zIndex: 1,
  p: 0.5,
  color: 'rgba(255, 245, 239, 0.86)',
  backgroundColor: 'rgba(30, 18, 19, 0.55)',
  '&:hover': {
    backgroundColor: 'rgba(30, 18, 19, 0.7)',
  },
  '&.Mui-checked': {
    color: '#f3b38d',
  },
  '&.Mui-disabled': {
    color: 'rgba(255, 245, 239, 0.4)',
  },
} as const
