import { Box, Button, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { innerSurfaceSx, playerHpBarSx, softGreenButtonSx } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

type TrainingBattleResultProps = {
  enemy: TrainingEnemy
  result: ExecuteTrainingResponse
  isActionDisabled: boolean
  lockRemainingSeconds: number
  movePlanSlot?: ReactNode
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
  movePlanSlot,
  onRematch,
}: TrainingBattleResultProps) {
  const rematchInSeconds = locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))
  const battleAgainst = locale.battleAgainst.replace('{{enemyName}}', enemy.name)
  const playerHp = locale.playerHp
    .replace('{{current}}', String(result.currentPlayerHp))
    .replace('{{max}}', String(result.maxPlayerHp))
  const enemyHp = locale.enemyHp
    .replace('{{current}}', String(result.currentEnemyHp))
    .replace('{{max}}', String(result.maxEnemyHp))
  const resultSummary = locale.resultSummary
    .replace('{{trainingResult}}', result.trainingResult)
    .replace('{{turn}}', String(result.turn))
    .replace('{{exp}}', String(result.exp))
  const expValue = locale.expValue.replace('{{exp}}', String(result.exp))

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
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
          {battleAgainst}
        </Typography>
        <Paper
          variant="outlined"
          sx={{
            ...innerSurfaceSx,
            p: 2,
            borderRadius: 3,
            background: 'linear-gradient(135deg, rgba(255, 244, 214, 0.95), rgba(255, 231, 182, 0.88))',
          }}
        >
          <Stack spacing={1.25} alignItems="center">
            <Typography variant="h4" fontWeight={900} color="warning.dark">
              {expValue}
            </Typography>
            <Typography variant="body2" color="text.secondary" textAlign="center">
              {resultSummary}
            </Typography>
            {result.isLevelUp ? <Chip color="success" label={locale.levelUp} sx={{ fontWeight: 700 }} /> : null}
            {result.newlyLearnedMoves.length > 0 ? (
              <Stack spacing={0.75} alignItems="center">
                <Typography variant="body2" fontWeight={700}>
                  {locale.newlyLearnedMoves}
                </Typography>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap justifyContent="center">
                  {result.newlyLearnedMoves.map((move) => (
                    <Chip key={move.moveId} color="info" label={move.moveName} variant="outlined" />
                  ))}
                </Stack>
              </Stack>
            ) : null}
          </Stack>
        </Paper>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(3, minmax(0, 1fr))' },
            gap: 1,
          }}
        >
          <Paper variant="outlined" sx={{ ...innerSurfaceSx, p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              {locale.result}
            </Typography>
            <Typography variant="subtitle2" fontWeight={700} color={toResultColor(result.trainingResult)}>
              {result.trainingResult}
            </Typography>
          </Paper>
          <Paper variant="outlined" sx={{ ...innerSurfaceSx, p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              {locale.turn}
            </Typography>
            <Typography variant="subtitle2" fontWeight={700}>
              {result.turn}
            </Typography>
          </Paper>
          <Paper variant="outlined" sx={{ ...innerSurfaceSx, p: 1.25 }}>
            <Typography variant="caption" color="text.secondary">
              {locale.expGained}
            </Typography>
            <Typography variant="subtitle2" fontWeight={700} color="warning.dark">
              {expValue}
            </Typography>
          </Paper>
        </Box>
        <Stack spacing={0.75}>
          <Typography variant="body2">{playerHp}</Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentPlayerHp, result.maxPlayerHp)}
            sx={playerHpBarSx}
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
        {movePlanSlot}
        <Button variant="contained" disabled={isActionDisabled} onClick={onRematch} sx={softGreenButtonSx}>
          {isActionDisabled ? rematchInSeconds : locale.rematch}
        </Button>
      </Stack>
    </Paper>
  )
}
