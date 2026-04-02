import type { ReactNode } from 'react'
import { Box, LinearProgress, Stack, Typography } from '@mui/material'
import { resolveRankColor } from '@/lib/playerRank'

type StatusStatRowProps = {
  label: string
  value: ReactNode
  normalized: number
  hideGauge?: boolean
  rank?: string
  compact?: boolean
}

export default function StatusStatRow({
  label,
  value,
  normalized,
  hideGauge = false,
  rank,
  compact = false,
}: StatusStatRowProps) {
  const rankColor = resolveRankColor(rank)

  return (
    <Box sx={{ display: 'flex', alignItems: 'stretch', gap: 0.6 }}>
      <Box
        sx={{
          flex: 1,
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
          <Typography
            variant="subtitle2"
            fontWeight={700}
            sx={{ minWidth: compact ? 0 : 68, textAlign: 'right', whiteSpace: 'nowrap' }}
          >
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
      {rank ? (
        <Box
          component="span"
          sx={{
            alignSelf: 'stretch',
            minWidth: compact ? 24 : 30,
            display: 'inline-flex',
            alignItems: 'center',
            justifyContent: 'center',
            borderRadius: 1,
            border: '1px solid',
            borderColor: rankColor,
            color: rankColor,
            backgroundColor: 'rgba(255,255,255,0.72)',
            fontSize: '0.94rem',
            fontWeight: 900,
            lineHeight: 1,
            letterSpacing: 0,
            textRendering: 'geometricPrecision',
          }}
        >
          {rank}
        </Box>
      ) : null}
    </Box>
  )
}
