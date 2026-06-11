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
  return (
    <Stack direction="row" justifyContent="center" alignItems="center" spacing={1.25}>
      <Button variant="outlined" disabled={page <= 1 || isLoading} onClick={onPrevious}>
        {previousLabel}
      </Button>
      <Typography variant="body2" sx={{ minWidth: 92, textAlign: 'center' }}>
        {pageLabel}
      </Typography>
      <Button variant="outlined" disabled={!hasNextPage || isLoading} onClick={onNext}>
        {nextLabel}
      </Button>
      {onLast && lastLabel ? (
        <Button variant="outlined" disabled={isLastDisabled || isLoading} onClick={onLast}>
          {lastLabel}
        </Button>
      ) : null}
    </Stack>
  )
}
