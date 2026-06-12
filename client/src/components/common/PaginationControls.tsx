import { Button, Stack, Typography } from '@mui/material'

type PaginationControlsProps = {
  page: number
  hasNextPage: boolean
  isLoading?: boolean
  previousLabel: string
  nextLabel: string
  lastLabel?: string
  pageLabel: string
  onPrevious: () => void
  onNext: () => void
  onLast?: () => void
  isLastDisabled?: boolean
}

export default function PaginationControls({
  page,
  hasNextPage,
  isLoading = false,
  previousLabel,
  nextLabel,
  lastLabel,
  pageLabel,
  onPrevious,
  onNext,
  onLast,
  isLastDisabled = false,
}: PaginationControlsProps) {
  const secondaryButtonSx = {
    minWidth: 76,
    color: '#2f646c',
    borderColor: 'rgba(98, 149, 155, 0.6)',
    backgroundColor: '#ffffff',
    '&:hover': {
      borderColor: 'rgba(84, 130, 136, 0.92)',
      backgroundColor: '#f1fbf8',
    },
    '&.Mui-disabled': {
      color: 'rgba(47, 100, 108, 0.45)',
      borderColor: 'rgba(98, 149, 155, 0.3)',
      backgroundColor: '#f6fbfa',
    },
  } as const

  const nextButtonSx = {
    minWidth: 76,
    color: '#ffffff',
    borderColor: '#3f8f8a',
    backgroundColor: '#58a9a0',
    boxShadow: 'none',
    '&:hover': {
      borderColor: '#327b76',
      backgroundColor: '#449a92',
      boxShadow: 'none',
    },
    '&.Mui-disabled': {
      color: 'rgba(255, 255, 255, 0.78)',
      borderColor: '#95c7c2',
      backgroundColor: '#9fcfc9',
    },
  } as const

  return (
    <Stack direction="row" justifyContent="center" alignItems="center" spacing={1.25}>
      <Button variant="outlined" sx={secondaryButtonSx} disabled={page <= 1 || isLoading} onClick={onPrevious}>
        {previousLabel}
      </Button>
      <Typography variant="body2" sx={{ minWidth: 92, textAlign: 'center' }}>
        {pageLabel}
      </Typography>
      <Button variant="contained" sx={nextButtonSx} disabled={!hasNextPage || isLoading} onClick={onNext}>
        {nextLabel}
      </Button>
      {onLast && lastLabel ? (
        <Button variant="outlined" sx={secondaryButtonSx} disabled={isLastDisabled || isLoading} onClick={onLast}>
          {lastLabel}
        </Button>
      ) : null}
    </Stack>
  )
}
