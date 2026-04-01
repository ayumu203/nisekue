import { Box, Typography } from '@mui/material'

export function SummaryStat({ label, value }: { label: string; value: number | string }) {
  return (
    <Box
      sx={{
        px: 1.5,
        py: 1.25,
        border: '1px solid #eadb9f',
        borderRadius: 2,
        bgcolor: 'rgba(255, 250, 230, 0.88)',
      }}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h6" fontWeight={800} color="#8a6a00">
        {value}
      </Typography>
    </Box>
  )
}
