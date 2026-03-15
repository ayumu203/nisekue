import { Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx } from '@/constants/styles'
import type { QuestRunDetailResponse } from '@/schema/quest'

type QuestLastTurnResultsPanelProps = {
  run: QuestRunDetailResponse | null | undefined
  locale: {
    lastTurnResultsTitle: string
    lastTurnResultsEmpty: string
    turnNo: string
    resolvedAtLabel: string
    labels: {
      actionKind: string
    }
    actionKinds: Record<string, string>
    hpChangeLabel: string
    mpChangeLabel: string
  }
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function formatDelta(value: number): string {
  return value > 0 ? `+${value}` : String(value)
}

export default function QuestLastTurnResultsPanel({ run, locale }: QuestLastTurnResultsPanelProps) {
  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
      <Stack spacing={2}>
        <Typography variant="h5">{locale.lastTurnResultsTitle}</Typography>
        {!run?.lastTurnResults ? (
          <Typography variant="body2" color="text.secondary">
            {locale.lastTurnResultsEmpty}
          </Typography>
        ) : (
          <Stack spacing={1.5}>
            <Typography>{`${locale.turnNo}: ${run.lastTurnResults.turnNo}`}</Typography>
            <Typography>{`${locale.resolvedAtLabel}: ${formatDateTime(run.lastTurnResults.resolvedAt)}`}</Typography>
            {run.lastTurnResults.actions.map((action, index) => (
              <Paper
                key={`${action.actorDisplayName}-${index}`}
                variant="outlined"
                sx={{
                  borderRadius: 2,
                  p: 1.5,
                  backgroundColor: '#fffdf8',
                }}
              >
                <Typography variant="subtitle2">{action.actorDisplayName}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {`${locale.labels.actionKind}: ${locale.actionKinds[action.actionKind] ?? action.actionKind}`}
                </Typography>
                {action.logs.length > 0 ? (
                  <Stack spacing={0.75} sx={{ mt: 1.25 }}>
                    {action.logs.map((log, logIndex) => (
                      <Typography
                        key={`${action.actorDisplayName}-${index}-${logIndex}`}
                        variant="body2"
                        sx={{ fontWeight: 600, color: '#3b2f1f' }}
                      >
                        {log}
                      </Typography>
                    ))}
                  </Stack>
                ) : null}
                {action.targetSummaries.map((target, targetIndex) => (
                  <Paper
                    key={`${action.actorDisplayName}-${index}-target-${targetIndex}`}
                    variant="outlined"
                    sx={{
                      mt: 1,
                      borderRadius: 2,
                      p: 1.25,
                      backgroundColor: '#fffaf0',
                    }}
                  >
                    <Typography variant="body2">{target.targetDisplayName}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {`${locale.hpChangeLabel}: ${formatDelta(target.hpChange)}`}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {`${locale.mpChangeLabel}: ${formatDelta(target.mpChange)}`}
                    </Typography>
                  </Paper>
                ))}
              </Paper>
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
