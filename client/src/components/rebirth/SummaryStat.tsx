import { Box, Typography } from '@mui/material'
import { palette, withAlpha } from '@/theme/tokens'

export function SummaryStat({ label, value }: { label: string; value: number | string }) {
  return (
    <Box
      sx={{
        px: 1.5,
        py: 1.5,
        border: `1px solid ${palette.gold300}`,
        borderRadius: 2,
        bgcolor: withAlpha(palette.cream, 0.9),
      }}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h6" fontWeight={800} color={palette.gold700}>
        {value}
      </Typography>
    </Box>
  )
}
