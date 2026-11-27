import type { ReactNode } from 'react'
import { Box, LinearProgress, Stack, Typography } from '@mui/material'
import { resolveRankColor } from '@/lib/playerRank'
import { font, palette, withAlpha } from '@/theme/tokens'

type StatusStatRowProps = {
  label: string
  value: ReactNode
  normalized: number
  hideGauge?: boolean
  rank?: string
  compact?: boolean
  // 枠線と背景なしで表示する（スマホのホームの横長ステータス用）
  plain?: boolean
}

export default function StatusStatRow({
  label,
  value,
  normalized,
  hideGauge = false,
  rank,
  compact = false,
  plain = false,
}: StatusStatRowProps) {
  const rankColor = resolveRankColor(rank)

  return (
    <Box sx={{ display: 'flex', alignItems: 'stretch', gap: 0.5 }}>
      <Box
        sx={
          plain
            ? { flex: 1 }
            : {
                flex: 1,
                p: 1.5,
                borderRadius: 1.5,
                border: '1px solid',
                borderColor: 'divider',
                backgroundColor: 'background.paper',
              }
        }
      >
        <Stack direction="row" alignItems="center" justifyContent="space-between" sx={{ mb: plain ? 0.5 : 1 }}>
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
            sx={{ height: 6, borderRadius: 999, backgroundColor: withAlpha(palette.brown900, 0.1) }}
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
            backgroundColor: withAlpha(palette.white, 0.75),
            fontSize: font.md,
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
