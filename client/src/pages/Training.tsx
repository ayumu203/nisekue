import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { executeTraining, getTrainingEnemies, TrainingCooldownError } from '@/api/training'
import { createPlayer, getPlayer } from '@/api/player'
import SignOut from '@/components/auth/SignOut'
import Status from '@/components/home/Status'
import TrainingBattleResult from '@/components/training/TrainingBattleResult'
import TrainingEnemySelect from '@/components/training/TrainingEnemySelect'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/training/Training.json'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'

export default function Training() {
  const { session, isLoading } = useAuth()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const menuButtonSx = {
    width: '100%',
    minHeight: 48,
    whiteSpace: 'nowrap',
  }
  const battleResultRef = useRef<HTMLDivElement | null>(null)
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

  useEffect(() => {
    if (!isMobile || !selectedEnemy || !trainingResult) {
      return
    }

    battleResultRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [isMobile, selectedEnemy, trainingResult])

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
      if (!message.includes(locale.playerNotFoundMessage)) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
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
        const retryAfterMessage = locale.retryAfterSeconds.replace('{{seconds}}', String(error.retryAfterSeconds))
        setTrainingError(`${error.message} (${retryAfterMessage})`)
      } else if (error instanceof Error) {
        setTrainingError(error.message)
      } else {
        setTrainingError(locale.trainingFailed)
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
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={{ p: { xs: 2, sm: 4 } }}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'minmax(180px, 260px) minmax(0, 1fr)',
                md: 'minmax(280px, 360px) minmax(0, 1fr)',
              },
              gap: { xs: 1.5, sm: 3 },
              alignItems: 'start',
            }}
          >
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
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
              <Button component={Link} to="/" variant="outlined" sx={menuButtonSx}>
                {locale.backToHome}
              </Button>
              <SignOut buttonSx={menuButtonSx} />
            </Stack>

            <Paper variant="outlined" sx={{ borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                {selectedEnemy && trainingResult ? (
                  <Box ref={battleResultRef}>
                    <TrainingBattleResult
                      enemy={selectedEnemy}
                      result={trainingResult}
                      isActionDisabled={isTrainingActionDisabled}
                      lockRemainingSeconds={trainingLockRemainingSeconds}
                      onRematch={async () => {
                        await runTraining(selectedEnemy)
                      }}
                    />
                  </Box>
                ) : null}

                {isTrainingEnemiesLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.enemiesLoading}</Typography>
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
              </Stack>
            </Paper>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
