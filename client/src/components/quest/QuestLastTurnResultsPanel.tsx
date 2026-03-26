import { Box, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx } from '@/constants/styles'
import type { QuestRunDetailResponse } from '@/schema/quest'

type QuestLastTurnResultsPanelProps = {
  run: QuestRunDetailResponse | null | undefined
  locale: {
    lastTurnResultsTitle: string
    lastTurnResultsEmpty: string
  }
}

function getLogColor(log: string): string {
  if (log.includes('倒した') || log.includes('撃破')) {
    return '#111111'
  }

  if (log.includes('ダメージ') || log.includes('反動')) {
    return '#d32f2f'
  }

  if (log.includes('使った') || log.includes('発動')) {
    return '#1565c0'
  }

  if (log.includes('回復')) {
    return '#2e7d32'
  }

  return '#3b2f1f'
}

export default function QuestLastTurnResultsPanel({ run, locale }: QuestLastTurnResultsPanelProps) {
  const logs = run?.lastTurnResults?.actions.flatMap((action) => action.logs) ?? []

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
      <Stack spacing={2}>
        {logs.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {locale.lastTurnResultsEmpty}
          </Typography>
        ) : (
          <Stack spacing={0.5}>
            {logs.map((log, index) => (
              <Box
                key={`${log}-${index}`}
                sx={{
                  px: 0.5,
                  py: 0.75,
                  borderBottom: '1px solid #c9c2b7',
                }}
              >
                <Typography
                  variant="body2"
                  sx={{ fontWeight: 600, color: getLogColor(log), lineHeight: 1.5 }}
                >
                  {log}
                </Typography>
              </Box>
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
