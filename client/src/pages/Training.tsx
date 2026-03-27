import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  IconButton,
  Paper,
  Stack,
  SvgIcon,
  Typography,
  useMediaQuery,
  useTheme,
  type SvgIconProps,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { executeTraining, getTrainingEnemies, TrainingCooldownError } from '@/api/training'
import { createPlayer, getPlayer } from '@/api/player'
import Status from '@/components/home/Status'
import TrainingBattleResult from '@/components/training/TrainingBattleResult'
import TrainingEnemySelect from '@/components/training/TrainingEnemySelect'
import TrainingMovePlanForm from '@/components/training/TrainingMovePlanForm'
import { useAuth } from '@/contexts/useAuth'
import { innerSurfaceSx, menuButtonSx, outerPagePaperSx, twoColumnContentGridSx } from '@/constants/styles'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { supabase } from '@/lib/supabase'
import signOutLocale from '../../locale/auth/SignOut.json'
import locale from '../../locale/training/Training.json'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'
import type { GetPlayerResponse } from '@/schema/player'

const TRAINING_COOLDOWN_MS = 3000

function SettingGearIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M19.14 12.94c.04-.31.06-.63.06-.94s-.02-.63-.06-.94l2.03-1.58a.5.5 0 0 0 .12-.64l-1.92-3.32a.5.5 0 0 0-.6-.22l-2.39.96a7.3 7.3 0 0 0-1.63-.94l-.36-2.54a.5.5 0 0 0-.5-.42H10.1a.5.5 0 0 0-.5.42l-.36 2.54c-.58.22-1.12.53-1.63.94l-2.39-.96a.5.5 0 0 0-.6.22L2.7 8.84a.5.5 0 0 0 .12.64l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94L2.82 14.52a.5.5 0 0 0-.12.64l1.92 3.32c.13.22.39.31.6.22l2.39-.96c.5.41 1.05.72 1.63.94l.36 2.54c.04.24.25.42.5.42h3.8c.25 0 .46-.18.5-.42l.36-2.54c.58-.22 1.12-.53 1.63-.94l2.39.96c.22.09.47 0 .6-.22l1.92-3.32a.5.5 0 0 0-.12-.64zM12 15.5A3.5 3.5 0 1 1 12 8.5a3.5 3.5 0 0 1 0 7" />
    </SvgIcon>
  )
}

function SignOutDoorIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M6 3h9a2 2 0 0 1 2 2v4h-2V5H6v14h9v-4h2v4a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2m9.59 4.59L21 13l-5.41 5.41L14.17 17 17.17 14H9v-2h8.17l-3-3z" />
    </SvgIcon>
  )
}

function getAvailableTrainingMoveIds(player: GetPlayerResponse): number[] {
  return player.moveSlots.flatMap((slot) => (slot.moveId === null ? [] : [slot.moveId]))
}

function buildDefaultTrainingMoveIds(): Array<number | null> {
  return [null, null, null]
}

function normalizeTrainingMoveIds(
  player: GetPlayerResponse,
  moveIds: Array<number | null> | null,
): Array<number | null> {
  const availableMoveIds = new Set(getAvailableTrainingMoveIds(player))
  const fallbackMoveIds = buildDefaultTrainingMoveIds()

  if (!moveIds || moveIds.length !== 3) {
    return fallbackMoveIds
  }

  return moveIds.map((moveId, index) => {
    if (moveId === null) {
      return null
    }

    return availableMoveIds.has(moveId) ? moveId : fallbackMoveIds[index]
  })
}

function areMoveIdArraysEqual(left: Array<number | null> | null, right: Array<number | null> | null): boolean {
  if (left === right) {
    return true
  }

  if (!left || !right || left.length !== right.length) {
    return false
  }

  return left.every((moveId, index) => moveId === right[index])
}

export default function Training() {
  const { session, isLoading } = useAuth()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const battleResultRef = useRef<HTMLDivElement | null>(null)
  const [selectedEnemy, setSelectedEnemy] = useState<TrainingEnemy | null>(null)
  const [trainingResult, setTrainingResult] = useState<ExecuteTrainingResponse | null>(null)
  const [trainingError, setTrainingError] = useState<string | null>(null)
  const [isTrainingSubmitting, setIsTrainingSubmitting] = useState(false)
  const [trainingLockUntilMs, setTrainingLockUntilMs] = useState(0)
  const [trainingLockRemainingSeconds, setTrainingLockRemainingSeconds] = useState(0)
  const [plannedMoveIds, setPlannedMoveIds] = useState<Array<number | null> | null>(null)
  const [lastSubmittedMoveIds, setLastSubmittedMoveIds] = useState<Array<number | null> | null>(null)
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [signOutError, setSignOutError] = useState<string | null>(null)

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

  const trainingEnemiesSWRKey =
    session?.access_token && player ? ([`training-enemies`, session.user.id] as const) : null
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

  useEffect(() => {
    if (!player) {
      return
    }

    setPlannedMoveIds((current) => {
      const next = normalizeTrainingMoveIds(player, current ?? lastSubmittedMoveIds)
      return areMoveIdArraysEqual(current, next) ? current : next
    })
    setLastSubmittedMoveIds((current) => {
      if (!current) {
        return current
      }

      const next = normalizeTrainingMoveIds(player, current)
      return areMoveIdArraysEqual(current, next) ? current : next
    })
  }, [lastSubmittedMoveIds, player])

  async function runTraining(enemy: TrainingEnemy, moveIds: Array<number | null>): Promise<void> {
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
      if (!player) {
        throw new Error(locale.playerLoading)
      }

      const normalizedMoveIds = normalizeTrainingMoveIds(player, moveIds)
      const result = await executeTraining(
        {
          enemyId: enemy.id,
          moveIds: normalizedMoveIds,
        },
        session.access_token,
      )
      setLastSubmittedMoveIds(normalizedMoveIds)
      setPlannedMoveIds(normalizedMoveIds)
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
      setTrainingLockUntilMs(Date.now() + TRAINING_COOLDOWN_MS)
    }
  }

  function handleSelectEnemy(enemy: TrainingEnemy): void {
    setSelectedEnemy(enemy)
    setTrainingResult(null)
    setTrainingError(null)

    if (!player) {
      return
    }

    setPlannedMoveIds(normalizeTrainingMoveIds(player, lastSubmittedMoveIds))
  }

  function handleChangePlannedMoveId(turnIndex: number, moveId: number | null): void {
    setPlannedMoveIds((current) => {
      const next = [...(current ?? [])]
      next[turnIndex] = moveId
      return next
    })
  }

  const handleSignOut = async () => {
    setIsSigningOut(true)
    setSignOutError(null)

    const { error } = await supabase.auth.signOut()
    if (error) {
      setSignOutError(error.message || signOutLocale.toastFailed)
      setIsSigningOut(false)
      return
    }

    setIsSigningOut(false)
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
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              {isPlayerLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.playerLoading}</Typography>
                </Stack>
              ) : playerError ? (
                <Alert severity="warning">{playerError.message}</Alert>
              ) : (
                <Status player={player} compactTrainingMobile={isMobile} showDesktopActions={false} />
              )}
              <Button component={Link} to="/" variant="outlined" sx={menuButtonSx}>
                {locale.backToHome}
              </Button>
            </Stack>

            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Stack direction="row" justifyContent="flex-end" sx={{ display: { xs: 'none', sm: 'flex' } }}>
                  <Stack direction="row" spacing={1}>
                    <IconButton
                      component={Link}
                      to="/player-setting"
                      aria-label="プレイヤー設定へ移動"
                      sx={{
                        width: 44,
                        height: 44,
                        border: '2px solid #ffffff',
                        color: '#ffffff',
                        backgroundColor: 'rgba(122, 77, 25, 0.9)',
                        '&:hover': {
                          backgroundColor: 'rgba(110, 68, 21, 0.94)',
                        },
                      }}
                    >
                      <SettingGearIcon />
                    </IconButton>
                    <IconButton
                      onClick={() => void handleSignOut()}
                      disabled={isSigningOut}
                      aria-label="ログアウト"
                      sx={{
                        width: 44,
                        height: 44,
                        border: '2px solid #ffffff',
                        color: '#ffffff',
                        backgroundColor: 'rgba(122, 77, 25, 0.9)',
                        '&:hover': {
                          backgroundColor: 'rgba(110, 68, 21, 0.94)',
                        },
                        '&.Mui-disabled': {
                          color: 'rgba(255,255,255,0.56)',
                          backgroundColor: 'rgba(122, 77, 25, 0.62)',
                        },
                      }}
                    >
                      <SignOutDoorIcon />
                    </IconButton>
                  </Stack>
                </Stack>
                {signOutError ? <Alert severity="error">{signOutError}</Alert> : null}
                {selectedEnemy && trainingResult ? (
                  <Box ref={battleResultRef}>
                    <TrainingBattleResult
                      enemy={selectedEnemy}
                      result={trainingResult}
                      isActionDisabled={isTrainingActionDisabled}
                      lockRemainingSeconds={trainingLockRemainingSeconds}
                      movePlanSlot={
                        player && plannedMoveIds ? (
                          <TrainingMovePlanForm
                            enemy={selectedEnemy}
                            player={player}
                            moveIds={plannedMoveIds}
                            isActionDisabled={isTrainingActionDisabled}
                            lockRemainingSeconds={trainingLockRemainingSeconds}
                            onChangeMoveId={handleChangePlannedMoveId}
                            showEnemyHeader={false}
                            showSubmitButton={false}
                            onSubmit={() => {}}
                          />
                        ) : null
                      }
                      onRematch={async () => {
                        if (!player) {
                          throw new Error(locale.playerLoading)
                        }

                        await runTraining(selectedEnemy, normalizeTrainingMoveIds(player, lastSubmittedMoveIds))
                      }}
                    />
                  </Box>
                ) : null}

                {selectedEnemy && player && plannedMoveIds && !trainingResult ? (
                  <TrainingMovePlanForm
                    enemy={selectedEnemy}
                    player={player}
                    moveIds={plannedMoveIds}
                    isActionDisabled={isTrainingActionDisabled}
                    lockRemainingSeconds={trainingLockRemainingSeconds}
                    onChangeMoveId={handleChangePlannedMoveId}
                    onSubmit={async () => {
                      await runTraining(selectedEnemy, plannedMoveIds)
                    }}
                  />
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
                    onFight={handleSelectEnemy}
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
