import { Button, Stack, Typography } from '@mui/material'

type PaginationControlsProps = {
  page: number
  hasNextPage: boolean
  isLoading?: boolean
  previousLabel: string
  nextLabel: string
  pageLabel: string
  onPrevious: () => void
  onNext: () => void
}

export default function PaginationControls({
  page,
  hasNextPage,
  isLoading = false,
  previousLabel,
  nextLabel,
  pageLabel,
  onPrevious,
  onNext,
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
    </Stack>
  )
}
