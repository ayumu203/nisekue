import type { ReactNode } from 'react'
import { Box, LinearProgress, Stack, Typography } from '@mui/material'

type StatusStatRowProps = {
  label: string
  value: ReactNode
  normalized: number
  hideGauge?: boolean
  rank?: string
  compact?: boolean
}

function resolveRankColor(rank: string | undefined): string | undefined {
  switch (rank) {
    case 'G':
      return '#3b82f6'
    case 'F':
      return '#4f79e2'
    case 'E':
      return '#6366f1'
    case 'D':
      return '#8b5cf6'
    case 'C':
      return '#a855f7'
    case 'B':
      return '#d946ef'
    case 'A':
      return '#ef4444'
    case 'S':
      return '#f97316'
    case 'SS':
      return '#f59e0b'
    case 'SSS':
      return '#d4af37'
    default:
      return undefined
  }
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
