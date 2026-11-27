import { Button, Stack, Typography } from '@mui/material'
import { palette, withAlpha } from '@/theme/tokens'

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
    color: palette.teal700,
    borderColor: withAlpha(palette.teal400, 0.5),
    backgroundColor: palette.white,
    '&.Mui-disabled': {
      color: withAlpha(palette.teal700, 0.5),
      borderColor: withAlpha(palette.teal400, 0.25),
      backgroundColor: palette.white,
    },
  } as const

  const nextButtonSx = {
    minWidth: 76,
    color: palette.white,
    borderColor: palette.teal400,
    backgroundColor: palette.teal400,
    boxShadow: 'none',
    '&.Mui-disabled': {
      color: withAlpha(palette.white, 0.75),
      borderColor: palette.teal400,
      backgroundColor: palette.teal400,
    },
  } as const

  return (
    <Stack direction="row" justifyContent="center" alignItems="center" spacing={1.5}>
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
