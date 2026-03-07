import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { executeTraining, getTrainingEnemies, TrainingCooldownError } from '@/api/training'
import { createPlayer, getPlayer } from '@/api/player'
import SignOut from '@/components/auth/SignOut'
import Status from '@/components/home/Status'
import TrainingBattleResult from '@/components/training/TrainingBattleResult'
import TrainingEnemySelect from '@/components/training/TrainingEnemySelect'
import { useAuth } from '@/contexts/useAuth'
import locale from '../../locale/home/Home.json'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'

function toDefaultUserName(email: string | undefined): string {
  const fallback = 'player'
  if (!email) {
    return fallback
  }

  const local = email.split('@')[0]?.trim() ?? ''
  const normalized = local.replace(/\s+/g, '').slice(0, 20)
  return normalized.length > 0 ? normalized : fallback
}

export default function Training() {
  const { session, user, isLoading } = useAuth()
  const [selectedEnemy, setSelectedEnemy] = useState<TrainingEnemy | null>(null)
  const [trainingResult, setTrainingResult] = useState<ExecuteTrainingResponse | null>(null)
  const [trainingError, setTrainingError] = useState<string | null>(null)
  const [isTrainingSubmitting, setIsTrainingSubmitting] = useState(false)
  const [trainingLockUntilMs, setTrainingLockUntilMs] = useState(0)
  const [trainingLockRemainingSeconds, setTrainingLockRemainingSeconds] = useState(0)

  useEffect(() => {
    if (trainingLockUntilMs <= Date.now()) {
      setTrainingLockRemainingSeconds(0)
      return
    }

    const update = () => {
      setTrainingLockRemainingSeconds(Math.max(0, Math.ceil((trainingLockUntilMs - Date.now()) / 1000)))
    }

    update()
    const timerId = window.setInterval(update, 250)
    return () => window.clearInterval(timerId)
  }, [trainingLockUntilMs])

  const playerSWRKey = session?.user.id ? ([`training-player`, session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
    mutate: mutatePlayer,
  } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    try {
      return await getPlayer(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''
      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: toDefaultUserName(user?.email) }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  const trainingEnemiesSWRKey = session?.access_token ? ([`training-enemies`] as const) : null
  const {
    data: trainingEnemies,
    error: trainingEnemiesError,
    isLoading: isTrainingEnemiesLoading,
  } = useSWR(trainingEnemiesSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getTrainingEnemies(session.access_token)
  })

  const isTrainingActionDisabled = isTrainingSubmitting || trainingLockRemainingSeconds > 0

  async function runTraining(enemy: TrainingEnemy): Promise<void> {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    if (isTrainingActionDisabled) {
      return
    }

    setIsTrainingSubmitting(true)
    setTrainingError(null)
    setSelectedEnemy(enemy)

    try {
      const result = await executeTraining({ enemyId: enemy.id }, session.access_token)
      setTrainingResult(result)
      await mutatePlayer()
    } catch (error) {
      if (error instanceof TrainingCooldownError) {
        setTrainingError(`${error.message} (${error.retryAfterSeconds}秒後に再試行できます)`)
      } else if (error instanceof Error) {
        setTrainingError(error.message)
      } else {
        setTrainingError('訓練の実行に失敗しました')
      }
    } finally {
      setIsTrainingSubmitting(false)
      setTrainingLockUntilMs(Date.now() + 5000)
    }
  }

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
            <Typography variant="h4">訓練</Typography>
            <Button component={Link} to="/" variant="outlined">
              ホームへ戻る
            </Button>
          </Stack>

          <Alert severity="success">
            {locale.signedInAs}: {user?.email}
          </Alert>

          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.playerLoading}</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : (
            <Status player={player} />
          )}

          <Paper variant="outlined" sx={{ borderRadius: 3, p: 2.5 }}>
            <Stack spacing={2}>
              {isTrainingEnemiesLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">訓練相手を読み込み中...</Typography>
                </Stack>
              ) : trainingEnemiesError ? (
                <Alert severity="warning">{trainingEnemiesError.message}</Alert>
              ) : (
                <TrainingEnemySelect
                  enemies={trainingEnemies ?? []}
                  isActionDisabled={isTrainingActionDisabled}
                  lockRemainingSeconds={trainingLockRemainingSeconds}
                  onFight={runTraining}
                />
              )}

              {trainingError ? <Alert severity="warning">{trainingError}</Alert> : null}

              {selectedEnemy && trainingResult ? (
                <TrainingBattleResult
                  enemy={selectedEnemy}
                  result={trainingResult}
                  isActionDisabled={isTrainingActionDisabled}
                  lockRemainingSeconds={trainingLockRemainingSeconds}
                  onRematch={async () => {
                    await runTraining(selectedEnemy)
                  }}
                />
              ) : null}
            </Stack>
          </Paper>

          <SignOut />
        </Stack>
      </Paper>
    </Container>
  )
}
