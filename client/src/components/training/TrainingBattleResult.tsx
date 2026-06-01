import { Box, Button, Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { innerSurfaceSx, playerHpBarSx } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

type TrainingBattleResultProps = {
  enemy: TrainingEnemy
  result: ExecuteTrainingResponse
  playerLevel?: number
  playerExp?: number
  nextLevelRequiredExp?: number
  isActionDisabled: boolean
  lockRemainingSeconds: number
  canUseAutoBattle: boolean
  isAutoBattling: boolean
  autoBattleRemainingSeconds: number
  autoBattleCount: number
  movePlanSlot?: ReactNode
  onRematch: () => Promise<void> | void
  onToggleAutoBattle: () => void
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
  playerLevel,
  playerExp,
  nextLevelRequiredExp,
  isActionDisabled,
  lockRemainingSeconds,
  canUseAutoBattle,
  isAutoBattling,
  autoBattleRemainingSeconds,
  autoBattleCount,
  movePlanSlot,
  onRematch,
  onToggleAutoBattle,
}: TrainingBattleResultProps) {
  const rematchInSeconds = locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))
  const battleAgainst = locale.battleAgainst.replace('{{enemyName}}', enemy.name)
  const playerHp = locale.playerHp
    .replace('{{current}}', String(result.currentPlayerHp))
    .replace('{{max}}', String(result.maxPlayerHp))
  const enemyHp = locale.enemyHp
    .replace('{{current}}', String(result.currentEnemyHp))
    .replace('{{max}}', String(result.maxEnemyHp))
  const expValue = locale.expValue.replace('{{exp}}', String(result.exp))
  const currentLevelLabel =
    typeof playerLevel === 'number' ? locale.currentLevel.replace('{{level}}', String(playerLevel)) : null
  const expProgressLabel =
    typeof playerExp === 'number' && typeof nextLevelRequiredExp === 'number'
      ? locale.expProgress
          .replace('{{current}}', String(playerExp))
          .replace('{{required}}', String(nextLevelRequiredExp))
      : null
  const levelUpLabel =
    result.isPlayerLevelUp && result.isJobLevelUp
      ? locale.playerAndJobLevelUp
      : result.isPlayerLevelUp
        ? locale.playerLevelUp
        : result.isJobLevelUp
          ? locale.levelUp
          : null

  const autoBattleMinutes = Math.floor(autoBattleRemainingSeconds / 60)
  const autoBattleSeconds = autoBattleRemainingSeconds % 60
  const autoBattleRemainingText = locale.autoBattleRemaining
    .replace('{{minutes}}', String(autoBattleMinutes))
    .replace('{{seconds}}', String(autoBattleSeconds).padStart(2, '0'))
  const autoBattleCountText = locale.autoBattleCount.replace('{{count}}', String(autoBattleCount))

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
        {isAutoBattling ? (
          <Stack spacing={0.75} alignItems="center">
            <Button
              variant="contained"
              onClick={onToggleAutoBattle}
              fullWidth
              sx={{
                borderRadius: 999,
                py: 1.1,
                fontWeight: 800,
                color: '#fff5ef',
                backgroundColor: '#c9443a',
                boxShadow: 'none',
                '&:hover': {
                  backgroundColor: '#d8554b',
                  boxShadow: 'none',
                },
              }}
            >
              {locale.autoBattleStop}
            </Button>
            <Typography variant="body2" sx={{ color: 'rgba(245, 240, 223, 0.82)', fontWeight: 700 }}>
              {autoBattleRemainingText}
            </Typography>
            <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.58)' }}>
              {autoBattleCountText}
            </Typography>
          </Stack>
        ) : (
          <Stack spacing={0.75}>
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
            {canUseAutoBattle && (
              <Button
                variant="outlined"
                disabled={isActionDisabled}
                onClick={onToggleAutoBattle}
                sx={{
                  borderRadius: 999,
                  py: 1,
                  fontWeight: 700,
                  color: 'rgba(245, 240, 223, 0.8)',
                  borderColor: 'rgba(214, 146, 112, 0.42)',
                  '&:hover': {
                    borderColor: 'rgba(214, 146, 112, 0.7)',
                    backgroundColor: 'rgba(182, 95, 73, 0.12)',
                  },
                  '&.Mui-disabled': {
                    color: 'rgba(255, 238, 229, 0.38)',
                    borderColor: 'rgba(214, 146, 112, 0.18)',
                  },
                }}
              >
                {locale.autoBattle}
              </Button>
            )}
            {!canUseAutoBattle && (
              <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.4)', textAlign: 'center' }}>
                {locale.autoBattleRequirement}
              </Typography>
            )}
          </Stack>
        )}
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
            {currentLevelLabel ? (
              <Typography variant="body1" fontWeight={800} textAlign="center" sx={{ color: '#fff7dd' }}>
                {currentLevelLabel}
              </Typography>
            ) : null}
            {expProgressLabel ? (
              <Typography variant="body2" textAlign="center" sx={{ color: 'rgba(245, 240, 223, 0.72)' }}>
                {expProgressLabel}
              </Typography>
            ) : null}
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
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
            gap: 1,
          }}
        >
          <Paper
            variant="outlined"
            sx={{
              ...innerSurfaceSx,
              p: 1.25,
              color: '#f5f0df',
              backgroundColor: '#382526',
              borderColor: 'rgba(214, 146, 112, 0.22)',
            }}
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
            sx={{
              ...innerSurfaceSx,
              p: 1.25,
              color: '#f5f0df',
              backgroundColor: '#382526',
              borderColor: 'rgba(214, 146, 112, 0.22)',
            }}
          >
            <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.62)' }}>
              {locale.turn}
            </Typography>
            <Typography variant="subtitle2" fontWeight={800}>
              {result.turn}
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
      </Stack>
    </Paper>
  )
}
