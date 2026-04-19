import {
  Alert,
  Box,
  CircularProgress,
  Container,
  Paper,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import useSWR, { useSWRConfig } from 'swr'
import { executeTraining, executeTrainingPvp, getTrainingEnemies, TrainingCooldownError } from '@/api/training'
import { createPlayer, getPlayer, listPlayers } from '@/api/player'
import BeginnerGuide from '@/components/common/BeginnerGuide'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import Status from '@/components/home/Status'
import TrainingBattleResult from '@/components/training/TrainingBattleResult'
import TrainingEnemySelect from '@/components/training/TrainingEnemySelect'
import TrainingMovePlanForm from '@/components/training/TrainingMovePlanForm'
import TrainingOpponentSelect from '@/components/training/TrainingOpponentSelect'
import { useAuth } from '@/contexts/useAuth'
import { innerSurfaceSx, outerPagePaperSx, twoColumnContentGridSx } from '@/constants/styles'
import { useMobileScrollToRef } from '@/hooks/useMobileScrollToRef'
import { beginnerGuides } from '@/lib/beginnerGuides'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/training/Training.json'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'
import type { GetPlayerResponse, PlayerSummary } from '@/schema/player'

type TrainingMode = 'npc' | 'player'

function playerSummaryToDisplayEnemy(opponent: PlayerSummary): TrainingEnemy {
  const rawPath = opponent.imagePath
  const imagePath = rawPath
    ? rawPath.startsWith('image/')
      ? rawPath
      : `image/character/${rawPath}`
    : 'image/character/ch001_bmnpc.png'
  return {
    id: -1,
    name: opponent.userName ?? locale.anonymousPlayer,
    imagePath,
    level: opponent.level,
  }
}

const TRAINING_COOLDOWN_MS = 3000

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
  const { mutate: mutateCache } = useSWRConfig()
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const trainingPanelRef = useRef<HTMLDivElement | null>(null)
  const battleResultRef = useRef<HTMLDivElement | null>(null)
  const trainingMovePlanRef = useRef<HTMLDivElement | null>(null)
  const [mode, setMode] = useState<TrainingMode>('npc')
  const [selectedEnemy, setSelectedEnemy] = useState<TrainingEnemy | null>(null)
  const [selectedOpponent, setSelectedOpponent] = useState<PlayerSummary | null>(null)
  const [trainingResult, setTrainingResult] = useState<ExecuteTrainingResponse | null>(null)
  const [trainingError, setTrainingError] = useState<string | null>(null)
  const [isTrainingSubmitting, setIsTrainingSubmitting] = useState(false)
  const [trainingLockUntilMs, setTrainingLockUntilMs] = useState(0)
  const [trainingLockRemainingSeconds, setTrainingLockRemainingSeconds] = useState(0)
  const [plannedMoveIds, setPlannedMoveIds] = useState<Array<number | null> | null>(null)
  const [lastSubmittedMoveIds, setLastSubmittedMoveIds] = useState<Array<number | null> | null>(null)

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
    if (!isMobile || !selectedEnemy || trainingResult || !plannedMoveIds) {
      return
    }

    trainingPanelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [isMobile, plannedMoveIds, selectedEnemy, trainingResult])

  useEffect(() => {
    if (!isMobile || !trainingResult) {
      return
    }

    trainingPanelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [isMobile, trainingResult])
  useMobileScrollToRef(trainingPanelRef, { enabled: isMobile })

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

  const playerListSWRKey =
    session?.access_token && player && mode === 'player' ? ([`training-opponents`, session.user.id] as const) : null
  const {
    data: playerList,
    error: playerListError,
    isLoading: isPlayerListLoading,
  } = useSWR(playerListSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return listPlayers(session.access_token)
  })

  async function refreshPlayerStatus(): Promise<void> {
    if (!session?.user.id) {
      return
    }

    await Promise.all([
      mutatePlayer(),
      mutateCache([`player`, session.user.id]),
      mutateCache([`quest-player`, session.user.id]),
      mutateCache([`training-player`, session.user.id]),
    ])
  }

  const isTrainingActionDisabled = isTrainingSubmitting || trainingLockRemainingSeconds > 0
  const playerLevel = player?.level
  const playerExp = player?.exp
  const nextLevelRequiredExp = player?.requiredExpForNextLevel

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
      await refreshPlayerStatus().catch((error) => {
        console.error('Failed to refresh player status after training.', error)
      })
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

  async function runPvpTraining(opponent: PlayerSummary, moveIds: Array<number | null>): Promise<void> {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    if (isTrainingActionDisabled) {
      return
    }

    setIsTrainingSubmitting(true)
    setTrainingError(null)
    setSelectedOpponent(opponent)

    try {
      if (!player) {
        throw new Error(locale.playerLoading)
      }

      const normalizedMoveIds = normalizeTrainingMoveIds(player, moveIds)
      const result = await executeTrainingPvp(
        {
          opponentPlayerId: opponent.userId,
          moveIds: normalizedMoveIds,
        },
        session.access_token,
      )
      setLastSubmittedMoveIds(normalizedMoveIds)
      setPlannedMoveIds(normalizedMoveIds)
      setTrainingResult(result)
      await refreshPlayerStatus().catch((error) => {
        console.error('Failed to refresh player status after pvp training.', error)
      })
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
    setSelectedOpponent(null)
    setTrainingResult(null)
    setTrainingError(null)

    if (!player) {
      return
    }

    setPlannedMoveIds(normalizeTrainingMoveIds(player, lastSubmittedMoveIds))
  }

  function handleSelectOpponent(opponent: PlayerSummary): void {
    setSelectedOpponent(opponent)
    setSelectedEnemy(null)
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
      <Paper
        elevation={2}
        sx={{
          ...outerPagePaperSx,
          background: 'linear-gradient(180deg, rgba(157, 69, 52, 0.98) 0%, rgba(100, 40, 34, 0.96) 100%)',
        }}
      >
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
              ) : isMobile ? (
                <Box sx={{ px: 0.5, pb: 1 }}>
                  <HomeNavIconButton ariaLabel={locale.backToHome} />
                </Box>
              ) : !isMobile ? (
                <Status
                  player={player}
                  compactTrainingMobile={isMobile}
                  showDesktopActions={false}
                  topAction={<HomeNavIconButton ariaLabel={locale.backToHome} />}
                />
              ) : null}
            </Stack>

            <Paper
              id="training-main"
              ref={trainingPanelRef}
              variant="outlined"
              sx={{
                ...innerSurfaceSx,
                borderRadius: 3,
                p: { xs: 2, sm: 2.5 },
                mt: '48px',
                color: '#f5f0df',
                backgroundColor: '#2d1d1e',
                borderColor: 'rgba(214, 146, 112, 0.55)',
              }}
            >
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Stack
                  direction="row"
                  justifyContent="space-between"
                  alignItems="flex-end"
                  spacing={1.5}
                  sx={{
                    px: { xs: 0.4, sm: 0.75 },
                    pb: 1.25,
                    borderBottom: '1px solid rgba(214, 146, 112, 0.24)',
                  }}
                >
                  <Stack spacing={0.6} sx={{ minWidth: 0 }}>
                    <Typography
                      variant="overline"
                      sx={{ color: 'rgba(248, 221, 207, 0.72)', letterSpacing: '0.18em', lineHeight: 1.2 }}
                    >
                      TRAINING GROUND
                    </Typography>
                    <Typography variant="h5" fontWeight={900} sx={{ color: '#fff7dd', lineHeight: 1.15 }}>
                      訓練場
                    </Typography>
                  </Stack>
                  <BeginnerGuide userId={session?.user.id} guide={beginnerGuides.training} inverted />
                </Stack>

                <ToggleButtonGroup
                  value={mode}
                  exclusive
                  onChange={(_e, next: TrainingMode | null) => {
                    if (next === null) return
                    setMode(next)
                    setSelectedEnemy(null)
                    setSelectedOpponent(null)
                    setTrainingResult(null)
                    setTrainingError(null)
                  }}
                  size="small"
                  sx={{
                    alignSelf: 'flex-start',
                    '& .MuiToggleButton-root': {
                      color: 'rgba(248, 221, 207, 0.7)',
                      borderColor: 'rgba(214, 146, 112, 0.4)',
                      fontWeight: 700,
                      px: 2,
                    },
                    '& .Mui-selected': {
                      color: '#fff5ef !important',
                      backgroundColor: 'rgba(182, 95, 73, 0.35) !important',
                    },
                  }}
                >
                  <ToggleButton value="npc">{locale.modeNpc}</ToggleButton>
                  <ToggleButton value="player">{locale.modePlayer}</ToggleButton>
                </ToggleButtonGroup>

                {(() => {
                  const displayEnemy =
                    selectedEnemy ?? (selectedOpponent ? playerSummaryToDisplayEnemy(selectedOpponent) : null)
                  const isOpponentMode = mode === 'player' && selectedOpponent !== null
                  return (
                    <>
                      {displayEnemy && trainingResult ? (
                        <Box ref={battleResultRef}>
                          <TrainingBattleResult
                            enemy={displayEnemy}
                            result={trainingResult}
                            playerLevel={playerLevel}
                            playerExp={playerExp}
                            nextLevelRequiredExp={nextLevelRequiredExp}
                            isActionDisabled={isTrainingActionDisabled}
                            lockRemainingSeconds={trainingLockRemainingSeconds}
                            movePlanSlot={
                              player && plannedMoveIds ? (
                                <TrainingMovePlanForm
                                  enemy={displayEnemy}
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
                              if (!player || !plannedMoveIds) {
                                throw new Error(locale.playerLoading)
                              }

                              if (isOpponentMode && selectedOpponent) {
                                await runPvpTraining(selectedOpponent, plannedMoveIds)
                              } else if (selectedEnemy) {
                                await runTraining(selectedEnemy, plannedMoveIds)
                              }
                            }}
                          />
                        </Box>
                      ) : null}

                      {displayEnemy && player && plannedMoveIds && !trainingResult ? (
                        <Box ref={trainingMovePlanRef}>
                          <TrainingMovePlanForm
                            enemy={displayEnemy}
                            player={player}
                            moveIds={plannedMoveIds}
                            isActionDisabled={isTrainingActionDisabled}
                            lockRemainingSeconds={trainingLockRemainingSeconds}
                            onChangeMoveId={handleChangePlannedMoveId}
                            onSubmit={async () => {
                              if (isOpponentMode && selectedOpponent) {
                                await runPvpTraining(selectedOpponent, plannedMoveIds)
                              } else if (selectedEnemy) {
                                await runTraining(selectedEnemy, plannedMoveIds)
                              }
                            }}
                          />
                        </Box>
                      ) : null}
                    </>
                  )
                })()}

                {mode === 'npc' ? (
                  isTrainingEnemiesLoading ? (
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
                  )
                ) : isPlayerListLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.opponentsLoading}</Typography>
                  </Stack>
                ) : playerListError ? (
                  <Alert severity="warning">{playerListError.message}</Alert>
                ) : (
                  <TrainingOpponentSelect
                    opponents={(playerList ?? [])
                      .filter((p) => p.userId !== session?.user.id)
                      .sort((a, b) => a.level - b.level)}
                    isActionDisabled={isTrainingActionDisabled}
                    lockRemainingSeconds={trainingLockRemainingSeconds}
                    onFight={handleSelectOpponent}
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
