import { Box, Button, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'

type TrainingBattleResultProps = {
  enemy: TrainingEnemy
  result: ExecuteTrainingResponse
  isActionDisabled: boolean
  lockRemainingSeconds: number
  onRematch: () => Promise<void> | void
}

function toResultColor(trainingResult: ExecuteTrainingResponse['trainingResult']): string {
  if (trainingResult === 'Win') {
    return 'success.main'
  }

  if (trainingResult === 'Lose') {
    return 'error.main'
  }

  return 'warning.main'
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
  return (
    <Paper variant="outlined" sx={{ borderRadius: 3, p: 2.5 }}>
      <Stack spacing={2}>
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
        <Typography variant="subtitle1" fontWeight={700} textAlign="center">
          {enemy.name} と戦闘
        </Typography>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, minmax(0, 1fr))' },
            gap: 1,
          }}
        >
          <Paper variant="outlined" sx={{ p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              結果
            </Typography>
            <Typography variant="subtitle2" fontWeight={700} color={toResultColor(result.trainingResult)}>
              {result.trainingResult}
            </Typography>
          </Paper>
          <Paper variant="outlined" sx={{ p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              ターン
            </Typography>
            <Typography variant="subtitle2" fontWeight={700}>
              {result.turn}
            </Typography>
          </Paper>
          <Paper variant="outlined" sx={{ p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              獲得経験値
            </Typography>
            <Typography variant="subtitle2" fontWeight={700}>
              {result.exp}
            </Typography>
          </Paper>
        </Box>
        <Stack spacing={0.75}>
          <Typography variant="body2">
            プレイヤー HP: {result.currentPlayerHp} / {result.maxPlayerHp}
          </Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentPlayerHp, result.maxPlayerHp)}
            sx={{ height: 8, borderRadius: 999 }}
          />
        </Stack>
        <Stack spacing={0.75}>
          <Typography variant="body2">
            敵 HP: {result.currentEnemyHp} / {result.maxEnemyHp}
          </Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentEnemyHp, result.maxEnemyHp)}
            color="secondary"
            sx={{ height: 8, borderRadius: 999 }}
          />
        </Stack>
        <Button variant="contained" disabled={isActionDisabled} onClick={onRematch}>
          {isActionDisabled ? `再戦まで ${lockRemainingSeconds}s` : '再戦する'}
        </Button>
      </Stack>
    </Paper>
  )
}
