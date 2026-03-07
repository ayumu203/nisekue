import { Button, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

type TrainingBattleResultProps = {
  enemy: TrainingEnemy
  result: ExecuteTrainingResponse
  isActionDisabled: boolean
  lockRemainingSeconds: number
  onRematch: () => Promise<void> | void
}

function normalizeHp(current: number, max: number): number {
  if (max <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (current / max) * 100))
}

export default function TrainingBattleResult({
  enemy,
  result,
  isActionDisabled,
  lockRemainingSeconds,
  onRematch,
}: TrainingBattleResultProps) {
  const rematchInSeconds = locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))
  const battleAgainst = locale.battleAgainst.replace('{{enemyName}}', enemy.name)
  const resultSummary = locale.resultSummary
    .replace('{{trainingResult}}', result.trainingResult)
    .replace('{{turn}}', String(result.turn))
    .replace('{{exp}}', String(result.exp))
  const playerHp = locale.playerHp
    .replace('{{current}}', String(result.currentPlayerHp))
    .replace('{{max}}', String(result.maxPlayerHp))
  const enemyHp = locale.enemyHp
    .replace('{{current}}', String(result.currentEnemyHp))
    .replace('{{max}}', String(result.maxEnemyHp))

  return (
    <Paper variant="outlined" sx={{ borderRadius: 3, p: 2.5 }}>
      <Stack spacing={2}>
        <Typography variant="h5">{locale.battleResultTitle}</Typography>
        <img
          src={resolvePublicAssetPath(enemy.imagePath)}
          alt={enemy.name}
          style={{
            width: '100%',
            height: 220,
            objectFit: 'contain',
            borderRadius: 12,
            backgroundColor: 'rgba(0, 0, 0, 0.04)',
            padding: 8,
            boxSizing: 'border-box',
          }}
        />
        <Typography variant="subtitle1" fontWeight={700}>
          {battleAgainst}
        </Typography>
        <Typography variant="body2">{resultSummary}</Typography>
        <Stack spacing={0.75}>
          <Typography variant="body2">{playerHp}</Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentPlayerHp, result.maxPlayerHp)}
            sx={{ height: 8, borderRadius: 999 }}
          />
        </Stack>
        <Stack spacing={0.75}>
          <Typography variant="body2">{enemyHp}</Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentEnemyHp, result.maxEnemyHp)}
            color="secondary"
            sx={{ height: 8, borderRadius: 999 }}
          />
        </Stack>
        <Button variant="contained" disabled={isActionDisabled} onClick={onRematch}>
          {isActionDisabled ? rematchInSeconds : locale.rematch}
        </Button>
      </Stack>
    </Paper>
  )
}
