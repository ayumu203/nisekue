import { Box, Button, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { innerSurfaceSx, playerHpBarSx } from '@/constants/styles'
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
  const levelUpLabel =
    result.isPlayerLevelUp && result.isJobLevelUp
      ? locale.playerAndJobLevelUp
      : result.isPlayerLevelUp
        ? locale.playerLevelUp
        : result.isJobLevelUp
          ? locale.levelUp
          : null

  return (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: 2.5,
        color: '#f5f0df',
        backgroundColor: '#261819',
        borderColor: 'rgba(214, 146, 112, 0.34)',
      }}
    >
      <Stack spacing={2}>
        <img
          src={resolvePublicAssetPath(enemy.imagePath)}
          alt={enemy.name}
          style={{
            width: '100%',
            height: 220,
            objectFit: 'contain',
            borderRadius: 12,
            backgroundColor: '#1d1112',
            border: '1px solid rgba(214, 146, 112, 0.22)',
            padding: 10,
            boxSizing: 'border-box',
          }}
        />
        <Typography variant="subtitle1" fontWeight={800} textAlign="center" sx={{ color: '#fff7dd' }}>
          {battleAgainst}
        </Typography>
        <Paper
          variant="outlined"
          sx={{
            ...innerSurfaceSx,
            p: 2,
            borderRadius: 3,
            color: '#fff7dd',
            backgroundColor: '#382526',
            borderColor: 'rgba(214, 146, 112, 0.34)',
          }}
        >
          <Stack spacing={1.25} alignItems="center">
            <Typography variant="h4" fontWeight={900} sx={{ color: '#f3b38d' }}>
              {expValue}
            </Typography>
            <Typography variant="body2" textAlign="center" sx={{ color: 'rgba(245, 240, 223, 0.72)' }}>
              {resultSummary}
            </Typography>
            {levelUpLabel ? (
              <Chip
                label={levelUpLabel}
                sx={{
                  fontWeight: 800,
                  color: '#1d2b20',
                  backgroundColor: '#d9e6be',
                }}
              />
            ) : null}
            {result.newlyLearnedMoves.length > 0 ? (
              <Stack spacing={0.75} alignItems="center">
                <Typography variant="body2" fontWeight={800} sx={{ color: '#fff0c8' }}>
                  {locale.newlyLearnedMoves}
                </Typography>
                <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap justifyContent="center">
                  {result.newlyLearnedMoves.map((move) => (
                    <Chip
                      key={move.moveId}
                      label={move.moveName}
                      variant="outlined"
                      sx={{
                        color: '#f5f0df',
                        borderColor: 'rgba(214, 146, 112, 0.38)',
                      }}
                    />
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
          <Paper
            variant="outlined"
            sx={{ ...innerSurfaceSx, p: 1.25, color: '#f5f0df', backgroundColor: '#382526', borderColor: 'rgba(214, 146, 112, 0.22)' }}
          >
            <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.62)' }}>
              {locale.result}
            </Typography>
            <Typography variant="subtitle2" fontWeight={700} color={toResultColor(result.trainingResult)}>
              {result.trainingResult}
            </Typography>
          </Paper>
          <Paper
            variant="outlined"
            sx={{ ...innerSurfaceSx, p: 1.25, color: '#f5f0df', backgroundColor: '#382526', borderColor: 'rgba(214, 146, 112, 0.22)' }}
          >
            <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.62)' }}>
              {locale.turn}
            </Typography>
            <Typography variant="subtitle2" fontWeight={800}>
              {result.turn}
            </Typography>
          </Paper>
          <Paper
            variant="outlined"
            sx={{ ...innerSurfaceSx, p: 1.25, color: '#f5f0df', backgroundColor: '#382526', borderColor: 'rgba(214, 146, 112, 0.22)' }}
          >
            <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.62)' }}>
              {locale.expGained}
            </Typography>
            <Typography variant="subtitle2" fontWeight={800} sx={{ color: '#f3b38d' }}>
              {expValue}
            </Typography>
          </Paper>
        </Box>
        <Stack spacing={0.75}>
          <Typography variant="body2" sx={{ color: '#fff7dd' }}>
            {playerHp}
          </Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentPlayerHp, result.maxPlayerHp)}
            sx={{ ...playerHpBarSx, backgroundColor: 'rgba(245, 240, 223, 0.14)' }}
          />
        </Stack>
        <Stack spacing={0.75}>
          <Typography variant="body2" sx={{ color: '#fff7dd' }}>
            {enemyHp}
          </Typography>
          <LinearProgress
            variant="determinate"
            value={normalizeHp(result.currentEnemyHp, result.maxEnemyHp)}
            sx={{
              height: 8,
              borderRadius: 999,
              backgroundColor: 'rgba(245, 240, 223, 0.14)',
              '& .MuiLinearProgress-bar': {
                backgroundColor: '#b65f49',
              },
            }}
          />
        </Stack>
        {movePlanSlot}
        <Button
          variant="contained"
          disabled={isActionDisabled}
          onClick={onRematch}
          sx={{
            borderRadius: 999,
            py: 1.1,
            fontWeight: 800,
            color: '#fff5ef',
            backgroundColor: '#b65f49',
            boxShadow: 'none',
            '&:hover': {
              backgroundColor: '#c96a52',
              boxShadow: 'none',
            },
            '&.Mui-disabled': {
              color: 'rgba(255, 238, 229, 0.58)',
              backgroundColor: 'rgba(182, 95, 73, 0.24)',
            },
          }}
        >
          {isActionDisabled ? rematchInSeconds : locale.rematch}
        </Button>
      </Stack>
    </Paper>
  )
}
