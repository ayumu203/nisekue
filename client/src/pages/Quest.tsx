import { useEffect, useMemo, useState } from 'react'
import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, SvgIcon, Typography, type SvgIconProps } from '@mui/material'
import { Link, useSearchParams } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import {
  approveQuestManualControl,
  createQuestRoom,
  getQuestRoom,
  getQuestRunByRoom,
  getQuestRun,
  getQuestStages,
  joinQuestRoom,
  listQuestRooms,
  postQuestChatMessage,
  requestQuestManualControl,
  startQuestRoom,
  submitQuestCommand,
  updateQuestRoomPosition,
} from '@/api/quest'
import SignOut from '@/components/auth/SignOut'
import Status from '@/components/home/Status'
import QuestRoomDetail from '@/components/quest/QuestRoomDetail'
import QuestRoomList from '@/components/quest/QuestRoomList'
import QuestRunPanel from '@/components/quest/QuestRunPanel'
import {
  innerSurfaceSx,
  menuButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
  twoColumnContentGridSx,
} from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { useQuestRunHub } from '@/hooks/useQuestRunHub'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/quest/Quest.json'

function HomeIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M12 4.5 4 11v9h5v-5h6v5h5v-9z" />
    </SvgIcon>
  )
}

export default function Quest() {
  const { session, isLoading } = useAuth()
  const [searchParams, setSearchParams] = useSearchParams()
  const [createMode, setCreateMode] = useState<'Solo' | 'Multi'>('Multi')
  const [createStageId, setCreateStageId] = useState<number | ''>('')
  const [uiMessage, setUiMessage] = useState<string | null>(null)
  const [uiError, setUiError] = useState<string | null>(null)

  const selectedRoomId = searchParams.get('roomId')
  const selectedRunId = searchParams.get('runId')

  const playerSWRKey = session?.user.id ? (['quest-player', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
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

  const stagesSWRKey = session?.access_token ? (['quest-stages'] as const) : null
  const {
    data: stages,
    error: stagesError,
    isLoading: isStagesLoading,
  } = useSWR(stagesSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getQuestStages(session.access_token)
  })

  useEffect(() => {
    if (!createStageId && stages?.length) {
      setCreateStageId(stages[0].stageId)
    }
  }, [createStageId, stages])

  const roomsSWRKey = session?.access_token ? (['quest-rooms'] as const) : null
  const {
    data: rooms,
    error: roomsError,
    isLoading: isRoomsLoading,
    mutate: mutateRooms,
  } = useSWR(roomsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return listQuestRooms({}, session.access_token)
  })

  useEffect(() => {
    if (selectedRoomId || !rooms?.length) {
      return
    }

    const nextParams = new URLSearchParams(searchParams)
    nextParams.set('roomId', rooms[0].roomId)
    setSearchParams(nextParams, { replace: true })
  }, [rooms, searchParams, selectedRoomId, setSearchParams])

  const roomSWRKey = session?.access_token && selectedRoomId ? (['quest-room', selectedRoomId] as const) : null
  const {
    data: selectedRoom,
    error: roomError,
    isLoading: isRoomLoading,
    mutate: mutateRoom,
  } = useSWR(roomSWRKey, async () => {
    if (!session?.access_token || !selectedRoomId) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getQuestRoom(selectedRoomId, session.access_token)
  })

  const runSWRKey = session?.access_token && selectedRunId ? (['quest-run', selectedRunId] as const) : null
  const {
    data: run,
    error: runError,
    isLoading: isRunLoading,
    mutate: mutateRun,
  } = useSWR(runSWRKey, async () => {
    if (!session?.access_token || !selectedRunId) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getQuestRun(selectedRunId, session.access_token)
  }, {
    refreshInterval: selectedRunId ? 2000 : 0,
  })

  const roomRunSWRKey =
    session?.access_token &&
    selectedRoomId &&
    !selectedRunId &&
    selectedRoom?.status === 'Closed' &&
    selectedRoom?.closeReason === 'Started'
      ? (['quest-room-run', selectedRoomId] as const)
      : null
  useSWR(roomRunSWRKey, async () => {
    if (!session?.access_token || !selectedRoomId) {
      throw new Error(locale.sessionInfoMissing)
    }

    const roomRun = await getQuestRunByRoom(selectedRoomId, session.access_token)
    const nextParams = new URLSearchParams(searchParams)
    nextParams.set('roomId', roomRun.roomId)
    nextParams.set('runId', roomRun.runId)
    setSearchParams(nextParams, { replace: true })
    await mutateRun(roomRun, { revalidate: false })
    return roomRun
  })

  const questRunHub = useQuestRunHub({
    runId: selectedRunId,
    accessToken: session?.access_token ?? null,
    onSnapshot: (payload) => {
      void mutateRun(payload, { revalidate: false })
    },
    onUpdated: (payload) => {
      void mutateRun(payload, { revalidate: false })
    },
    onError: (payload) => {
      setUiError(payload.message)
    },
  })

  const currentUserId = session?.user.id ?? null
  const selectedStage = useMemo(
    () => stages?.find((stage) => stage.stageId === createStageId) ?? null,
    [createStageId, stages],
  )

  async function withUiFeedback(action: () => Promise<void>) {
    setUiError(null)
    setUiMessage(null)

    try {
      await action()
    } catch (error) {
      setUiError(error instanceof Error ? error.message : '処理に失敗しました')
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
    <Container maxWidth="xl" sx={{ py: { xs: 2, sm: 8 } }}>
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
                <Status player={player} />
              )}
              <Button component={Link} to="/" variant="outlined" startIcon={<HomeIcon />} sx={menuButtonSx}>
                {locale.backToHome}
              </Button>
              <SignOut buttonSx={menuButtonSx} />
            </Stack>

            <Stack spacing={2}>
              {uiError ? <Alert severity="warning">{uiError}</Alert> : null}
              {uiMessage ? <Alert severity="success">{uiMessage}</Alert> : null}
              {stagesError ? <Alert severity="warning">{stagesError.message}</Alert> : null}
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', xl: 'minmax(280px, 340px) minmax(320px, 420px) minmax(0, 1fr)' },
                  gap: 2,
                  alignItems: 'start',
                }}
              >
                <QuestRoomList
                  stages={stages}
                  rooms={rooms}
                  isStagesLoading={isStagesLoading}
                  isRoomsLoading={isRoomsLoading}
                  roomsError={roomsError?.message ?? null}
                  selectedRoomId={selectedRoomId}
                  createStageId={createStageId}
                  createMode={createMode}
                  onChangeStageId={setCreateStageId}
                  onChangeMode={setCreateMode}
                  onCreateRoom={async () =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !createStageId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      const createdRoom = await createQuestRoom(
                        {
                          stageId: createStageId,
                          mode: createMode,
                        },
                        session.access_token,
                      )
                      const nextParams = new URLSearchParams(searchParams)
                      nextParams.set('roomId', createdRoom.roomId)
                      nextParams.delete('runId')
                      setSearchParams(nextParams)
                      await mutateRooms()
                      setUiMessage(`${locale.createRoom}: ${selectedStage?.name ?? createdRoom.stageId}`)
                    })
                  }
                  onSelectRoom={(roomId) => {
                    const nextParams = new URLSearchParams(searchParams)
                    nextParams.set('roomId', roomId)
                    nextParams.delete('runId')
                    setSearchParams(nextParams)
                    setUiMessage(null)
                    setUiError(null)
                  }}
                  onRefresh={() => mutateRooms()}
                />

                <QuestRoomDetail
                  room={selectedRoom}
                  currentUserId={currentUserId}
                  isLoading={isRoomLoading}
                  error={roomError?.message ?? null}
                  onJoin={async () =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRoomId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      const joinedRoom = await joinQuestRoom(selectedRoomId, session.access_token)
                      await mutateRoom(joinedRoom, { revalidate: false })
                      await mutateRooms()
                      setUiMessage(locale.alreadyJoinedRoom)
                    })
                  }
                  onStart={async () =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRoomId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      const startedRun = await startQuestRoom(selectedRoomId, session.access_token)
                      const nextParams = new URLSearchParams(searchParams)
                      nextParams.set('roomId', startedRun.roomId)
                      nextParams.set('runId', startedRun.runId)
                      setSearchParams(nextParams)
                      await mutateRoom()
                      await mutateRooms()
                      setUiMessage(locale.questStarted)
                    })
                  }
                  onUpdatePosition={(participantId, row, column) =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRoomId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      const updatedRoom = await updateQuestRoomPosition(
                        selectedRoomId,
                        { participantId, row, column },
                        session.access_token,
                      )
                      await mutateRoom(updatedRoom, { revalidate: false })
                    })
                  }
                />

                <QuestRunPanel
                  run={run}
                  room={selectedRoom}
                  player={player}
                  isLoading={isRunLoading}
                  error={runError?.message ?? null}
                  isConnected={questRunHub.isConnected}
                  connectionError={questRunHub.connectionError?.message ?? null}
                  currentUserId={currentUserId}
                  onSubmitCommand={(input) =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRunId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      const result = await submitQuestCommand(selectedRunId, input, session.access_token)
                      if (!questRunHub.isConnected) {
                        await mutateRun()
                      }
                      setUiMessage(
                        input.actionKind === 'LeaveQuest'
                          ? locale.leftQuest
                          : result.resolvedInThisRequest
                            ? locale.commandResolved
                            : locale.commandAccepted,
                      )
                    })
                  }
                  onRequestManualControl={(participantId) =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRunId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      await requestQuestManualControl(selectedRunId, { participantId }, session.access_token)
                      if (!questRunHub.isConnected) {
                        await mutateRun()
                      }
                      setUiMessage(locale.manualRequested)
                    })
                  }
                  onApproveManualControl={(participantId) =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRunId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      await approveQuestManualControl(selectedRunId, { participantId }, session.access_token)
                      if (!questRunHub.isConnected) {
                        await mutateRun()
                      }
                      setUiMessage(locale.manualApproved)
                    })
                  }
                  onSendChat={(participantId, message) =>
                    withUiFeedback(async () => {
                      if (!session?.access_token || !selectedRunId) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      await postQuestChatMessage(selectedRunId, { participantId, message }, session.access_token)
                      if (!questRunHub.isConnected) {
                        await mutateRun()
                      }
                      setUiMessage(locale.chatPosted)
                    })
                  }
                />
              </Box>

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                  <Typography variant="body2" color="text.secondary">
                    {locale.stage}: {selectedStage?.name ?? '-'}
                  </Typography>
                  <Button component={Link} to="/" variant="contained" sx={{ ...menuButtonSx, ...softGreenButtonSx }}>
                    {locale.backToHome}
                  </Button>
                </Stack>
              </Paper>
            </Stack>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
