import { Button, Paper, Stack, Typography } from '@mui/material'
import PetBattleTurnLog from '@/components/petbattle/PetBattleTurnLog'
import { cyberButtonSx, cyberColors, cyberPanelSx } from '@/components/petbattle/petBattleStyles'
import locale from '../../../locale/pet/PetBattle.json'
import type { PetBattleRun, PetBattleStats } from '@/schema/petBattle'

type PetBattleResultPanelProps = {
  run: PetBattleRun
  stats: PetBattleStats | null
  ratingChange: number | null
  onBackToPets: () => void
}

function formatRatingChange(ratingChange: number): string {
  if (ratingChange > 0) {
    return `+${ratingChange}`
  }

  return ratingChange.toString()
}

export default function PetBattleResultPanel({ run, stats, ratingChange, onBackToPets }: PetBattleResultPanelProps) {
  if (run.status === 'InProgress') {
    return null
  }

  const isWin = run.status === 'OwnerWon'

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 2, sm: 3 }, textAlign: 'center' }}>
        <Stack spacing={1.5} alignItems="center">
          <Typography
            variant="h4"
            sx={{
              fontWeight: 900,
              letterSpacing: '0.1em',
              color: isWin ? cyberColors.accent : cyberColors.danger,
              textShadow: isWin ? `0 0 18px ${cyberColors.accentDim}` : '0 0 18px rgba(255, 56, 96, 0.5)',
            }}
          >
            {locale.resultTitle[run.status]}
          </Typography>

          <Stack direction="row" spacing={2} justifyContent="center" useFlexGap flexWrap="wrap">
            {ratingChange != null ? (
              <Typography variant="body1" sx={{ color: cyberColors.text }}>
                {locale.ratingChangeLabel}: <strong>{formatRatingChange(ratingChange)}</strong>
              </Typography>
            ) : null}
            {stats != null ? (
              <Typography variant="body1" sx={{ color: cyberColors.text }}>
                {locale.resultRatingLabel}: <strong>{stats.rating}</strong>
              </Typography>
            ) : null}
          </Stack>

          <Button onClick={onBackToPets} sx={cyberButtonSx}>
            {locale.backToPets}
          </Button>
        </Stack>
      </Paper>

      {run.lastTurnResults != null ? <PetBattleTurnLog results={run.lastTurnResults} /> : null}
    </Stack>
  )
}
