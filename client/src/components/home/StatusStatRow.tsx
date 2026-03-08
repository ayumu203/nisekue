import { Box, LinearProgress, Stack, Typography } from '@mui/material'

type StatusStatRowProps = {
  label: string
  value: number | string
  normalized: number
  hideGauge?: boolean
}

export default function StatusStatRow({ label, value, normalized, hideGauge = false }: StatusStatRowProps) {
  return (
    <Box
      sx={{
        p: 1.5,
        borderRadius: 1.5,
        border: '1px solid',
        borderColor: 'divider',
        backgroundColor: 'background.paper',
      }}
    >
      <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: 1 }}>
        <Typography variant="body2" color="text.secondary">
          {label}
        </Typography>
        <Typography variant="subtitle2" fontWeight={700}>
          {value}
        </Typography>
      </Stack>
      {!hideGauge ? (
        <LinearProgress
          variant="determinate"
          value={normalized}
          sx={{ height: 6, borderRadius: 999, backgroundColor: 'action.hover' }}
        />
      ) : null}
    </Box>
  )
}
