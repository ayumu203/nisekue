import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import {
  createQuestRoom,
  getQuestRoom,
  getQuestRunByRoom,
  getQuestStages,
  joinQuestRoom,
  listQuestRooms,
  startQuestRoom,
  submitQuestCommand,
  updateQuestRoomPosition,
} from '@/api/quest'
import QuestBattleStatusPanel from '@/components/quest/QuestBattleStatusPanel'
import QuestMultiRoomList from '@/components/quest/QuestMultiRoomList'
import { useAuth } from '@/contexts/useAuth'
import {
  greenOutlinedInputSx,
  innerSurfaceSx,
  menuButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
  twoColumnContentGridSx,
} from '@/constants/styles'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/quest/QuestMultTest.json'
import type {
  BattleColumn,
  BattleRow,
  GetQuestStagesResponse,
  QuestActionKind,
  QuestRoomDetailResponse,
} from '@/schema/quest'

function formatStagePartyRange(stage: GetQuestStagesResponse[number]): string {
  return `${stage.minPartyMemberCount} - ${stage.maxPartyMemberCount}`
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export default function QuestMultTest() {
  const { session, isLoading } = useAuth()
  const [selectedStageId, setSelectedStageId] = useState<number | ''>('')
  const [createdRoom, setCreatedRoom] = useState<QuestRoomDetailResponse | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isJoiningRoomId, setIsJoiningRoomId] = useState<string | null>(null)
  const [isUpdatingParticipantId, setIsUpdatingParticipantId] = useState<string | null>(null)
  const [isStarting, setIsStarting] = useState(false)
  const [positionDrafts, setPositionDrafts] = useState<Record<string, { row: BattleRow; column: BattleColumn }>>({})
  const [selectedActionKind, setSelectedActionKind] = useState<QuestActionKind>('NormalAttack')
  const [selectedMoveId, setSelectedMoveId] = useState<number | ''>('')
  const [selectedTargetRow, setSelectedTargetRow] = useState<BattleRow | ''>('')
  const [selectedTargetColumn, setSelectedTargetColumn] = useState<BattleColumn | ''>('')
  const [commandMessage, setCommandMessage] = useState<string | null>(null)
  const [isCommandSubmitting, setIsCommandSubmitting] = useState(false)

  const playerSWRKey = session?.user.id ? ([`quest-mult-player`, session.user.id] as const) : null
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

  const stagesSWRKey = session?.access_token ? ([`quest-mult-stages`] as const) : null
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

  const activeStages = useMemo(() => (stages ?? []).filter((stage) => stage.isActive), [stages])
  const selectedStage = activeStages.find((stage) => stage.stageId === selectedStageId) ?? null
  const isCreateDisabled =
    isSubmitting ||
    isPlayerLoading ||
    isStagesLoading ||
    playerError != null ||
    activeStages.length === 0 ||
    selectedStageId === ''
  const roomsSWRKey = session?.access_token ? ([`quest-mult-rooms`] as const) : null
  const {
    data: latestRooms,
    error: roomsError,
    isLoading: isRoomsLoading,
    mutate: mutateRooms,
  } = useSWR(roomsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    const rooms = await listQuestRooms(
      {
        mode: 'Multi',
        status: 'Recruiting',
        pageSize: 5,
      },
      session.access_token,
    )

    return [...rooms]
      .sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
      .slice(0, 5)
  })
  const roomSWRKey =
    session?.access_token && createdRoom?.roomId ? ([`quest-mult-room`, createdRoom.roomId] as const) : null
  const {
    data: liveRoom,
    error: roomError,
    mutate: mutateRoom,
  } = useSWR(
    roomSWRKey,
    async () => {
      if (!session?.access_token || !createdRoom?.roomId) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getQuestRoom(createdRoom.roomId, session.access_token)
    },
    {
      refreshInterval: createdRoom?.status === 'Recruiting' ? 2000 : 0,
    },
  )
  const currentRoom = liveRoom ?? createdRoom
  const isJoinedRoomOwner = currentRoom != null && player != null && currentRoom.ownerPlayerId === player.userId
  const roomRunSWRKey =
    session?.access_token &&
    currentRoom?.roomId &&
    currentRoom.status === 'Closed' &&
    currentRoom.closeReason === 'Started'
      ? ([`quest-mult-run`, currentRoom.roomId] as const)
      : null
  const {
    data: currentRun,
    error: runError,
    isLoading: isRunLoading,
    mutate: mutateRun,
  } = useSWR(
    roomRunSWRKey,
    async () => {
      if (!session?.access_token || !currentRoom?.roomId) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getQuestRunByRoom(currentRoom.roomId, session.access_token)
    },
    {
      refreshInterval: 2000,
    },
  )
  const selfParticipantId =
    player && currentRoom
      ? (currentRoom.participants.find((participant) => participant.playerId === player.userId)?.participantId ?? null)
      : null
  const availableMoves = useMemo(() => (player?.moveSlots ?? []).filter((slot) => slot.moveId != null), [player])
  const firstEnemyPosition = currentRun?.enemies[0]?.position ?? null
  const currentPendingCommand =
    currentRun && selfParticipantId
      ? (currentRun.pendingCommands.find(
          (command) => command.participantId === selfParticipantId && command.turnNo === currentRun.turn.currentTurnNo,
        ) ?? null)
      : null

  useEffect(() => {
    if (!firstEnemyPosition) {
      return
    }

    setSelectedTargetRow((current) => current || firstEnemyPosition.row)
    setSelectedTargetColumn((current) => current || firstEnemyPosition.column)
  }, [firstEnemyPosition])

  useEffect(() => {
    if (selectedActionKind !== 'UseMove') {
      setSelectedMoveId('')
    }
  }, [selectedActionKind])

  useEffect(() => {
    if (selectedStageId !== '' || activeStages.length === 0) {
      return
    }

    setSelectedStageId(activeStages[0]!.stageId)
  }, [activeStages, selectedStageId])

  useEffect(() => {
    if (!currentRoom) {
      setPositionDrafts({})
      return
    }

    setPositionDrafts(
      Object.fromEntries(
        currentRoom.participants.map((participant) => [
          participant.participantId,
          {
            row: participant.position.row,
            column: participant.position.column,
          },
        ]),
      ),
    )
  }, [currentRoom])

  async function handleCreateRoom(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (selectedStageId === '') {
      setSubmitError(locale.selectStageFirst)
      return
    }

    setIsSubmitting(true)
    setSubmitError(null)

    try {
      const room = await createQuestRoom(
        {
          stageId: selectedStageId,
          mode: 'Multi',
        },
        session.access_token,
      )
      setCreatedRoom(room)
      await mutateRoom(room, { revalidate: false })
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.createRoomFailed)
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleJoinRoom(roomId: string): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    setIsJoiningRoomId(roomId)
    setSubmitError(null)

    try {
      const room = await joinQuestRoom(roomId, session.access_token)
      setCreatedRoom(room)
      await mutateRoom(room, { revalidate: false })
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.joinRoomFailed)
    } finally {
      setIsJoiningRoomId(null)
    }
  }

  async function handleUpdateParticipantPosition(participantId: string): Promise<void> {
    if (!session?.access_token || !currentRoom) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    const draft = positionDrafts[participantId]
    if (!draft) {
      return
    }

    setIsUpdatingParticipantId(participantId)
    setSubmitError(null)

    try {
      const room = await updateQuestRoomPosition(
        currentRoom.roomId,
        {
          participantId,
          row: draft.row,
          column: draft.column,
        },
        session.access_token,
      )
      setCreatedRoom(room)
      await mutateRoom(room, { revalidate: false })
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.updatePositionFailed)
    } finally {
      setIsUpdatingParticipantId(null)
    }
  }

  async function handleStartQuest(): Promise<void> {
    if (!session?.access_token || !currentRoom) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    setIsStarting(true)
    setSubmitError(null)

    try {
      const run = await startQuestRoom(currentRoom.roomId, session.access_token)
      setCreatedRoom((current) =>
        current
          ? {
              ...current,
              status: 'Closed',
              closeReason: 'Started',
              canStart: false,
            }
          : current,
      )
      await mutateRoom()
      await Promise.resolve(run)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.startQuestFailed)
    } finally {
      setIsStarting(false)
    }
  }

  async function handleSubmitCommand(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!currentRun || !selfParticipantId) {
      setSubmitError(locale.commandUnavailable)
      return
    }

    setIsCommandSubmitting(true)
    setSubmitError(null)
    setCommandMessage(null)

    try {
      const selectedTargetPosition =
        selectedTargetRow !== '' && selectedTargetColumn !== ''
          ? {
              targetRow: selectedTargetRow,
              targetColumn: selectedTargetColumn,
            }
          : {
              targetRow: null,
              targetColumn: null,
            }

      const result = await submitQuestCommand(
        currentRun.runId,
        {
          participantId: selfParticipantId,
          turnNo: currentRun.turn.currentTurnNo,
          actionKind: selectedActionKind,
          moveId: selectedActionKind === 'UseMove' && selectedMoveId !== '' ? selectedMoveId : null,
          targetRow: selectedTargetPosition.targetRow,
          targetColumn: selectedTargetPosition.targetColumn,
        },
        session.access_token,
      )

      await mutateRun()
      setCommandMessage(result.resolvedInThisRequest ? locale.commandResolved : locale.commandAccepted)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.commandSubmitFailed)
    } finally {
      setIsCommandSubmitting(false)
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
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              <Button component={Link} to="/" variant="outlined" sx={menuButtonSx}>
                {locale.backToHome}
              </Button>
              <Button component={Link} to="/quest/quest-solo-test" variant="outlined" sx={menuButtonSx}>
                {locale.toSoloTest}
              </Button>
            </Stack>

            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={2}>
                  <div>
                    <Typography variant="h4">{locale.roomPageTitle}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {locale.roomPageSubtitle}
                    </Typography>
                  </div>

                  {playerError ? <Alert severity="warning">{playerError.message}</Alert> : null}
                  {stagesError ? <Alert severity="warning">{stagesError.message}</Alert> : null}
                  {roomError ? <Alert severity="warning">{roomError.message}</Alert> : null}
                  {runError ? <Alert severity="warning">{runError.message}</Alert> : null}
                  {submitError ? <Alert severity="error">{submitError}</Alert> : null}
                  {commandMessage ? <Alert severity="success">{commandMessage}</Alert> : null}

                  {isPlayerLoading || isStagesLoading ? (
                    <Stack direction="row" spacing={1} alignItems="center">
                      <CircularProgress size={18} />
                      <Typography variant="body2">{locale.roomPageLoading}</Typography>
                    </Stack>
                  ) : activeStages.length === 0 ? (
                    <Alert severity="info">{locale.stagesEmpty}</Alert>
                  ) : (
                    <Stack spacing={2}>
                      <FormControl fullWidth sx={greenOutlinedInputSx}>
                        <InputLabel id="quest-mult-stage-select-label">{locale.labels.stageSelect}</InputLabel>
                        <Select
                          labelId="quest-mult-stage-select-label"
                          value={selectedStageId}
                          label={locale.labels.stageSelect}
                          onChange={(event) => {
                            const nextValue = event.target.value
                            setSelectedStageId(typeof nextValue === 'number' ? nextValue : Number(nextValue))
                          }}
                        >
                          {activeStages.map((stage) => (
                            <MenuItem key={stage.stageId} value={stage.stageId}>
                              {stage.name}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>

                      <Paper
                        variant="outlined"
                        sx={{
                          ...innerSurfaceSx,
                          borderRadius: 3,
                          p: 2,
                        }}
                      >
                        <Stack spacing={1}>
                          <Typography variant="body2" color="text.secondary">
                            {`${locale.modeLabel}: ${locale.modeMultiFixed}`}
                          </Typography>
                          {selectedStage ? (
                            <>
                              <Typography variant="h6">{selectedStage.name}</Typography>
                              <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                                <Chip label={`${locale.recommendedLevel}: ${selectedStage.recommendedLevel}`} />
                                <Chip label={`${locale.partyRange}: ${formatStagePartyRange(selectedStage)}`} />
                                <Chip label={`${locale.floorCount}: ${selectedStage.floors.length}`} />
                              </Stack>
                            </>
                          ) : null}
                        </Stack>
                      </Paper>

                      <Button
                        variant="contained"
                        onClick={() => void handleCreateRoom()}
                        disabled={isCreateDisabled}
                        sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                      >
                        {isSubmitting ? locale.creatingRoom : locale.createRoom}
                      </Button>
                    </Stack>
                  )}
                </Stack>
              </Paper>

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={2}>
                  <Typography variant="h5">{locale.createdRoomTitle}</Typography>
                  {currentRoom == null ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.createdRoomEmpty}
                    </Typography>
                  ) : (
                    <Stack spacing={1.5}>
                      <Typography>{`${locale.roomId}: ${currentRoom.roomId}`}</Typography>
                      <Typography>{`${locale.stage}: ${currentRoom.stageId}`}</Typography>
                      <Typography>{`${locale.createdAt}: ${formatDateTime(currentRoom.createdAt)}`}</Typography>
                      <Typography>{`${locale.roomVersion}: ${currentRoom.version}`}</Typography>
                      <Typography>{`${locale.participants}: ${currentRoom.participants.length}`}</Typography>
                      <Typography>{`${locale.canStartLabel}: ${currentRoom.canStart ? locale.yes : locale.no}`}</Typography>
                      <Typography>{`${locale.roomStatusLabel}: ${locale.roomStatus[currentRoom.status]}`}</Typography>
                      <Typography>{`${locale.modeLabel}: ${locale.modeMultiFixed}`}</Typography>
                      {isJoinedRoomOwner ? (
                        <Button
                          variant="contained"
                          onClick={() => void handleStartQuest()}
                          disabled={isStarting || !currentRoom.canStart}
                          sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                        >
                          {isStarting ? locale.startingQuest : locale.startQuest}
                        </Button>
                      ) : null}
                      <Stack spacing={1}>
                        {currentRoom.participants.map((participant) => (
                          <Paper
                            key={participant.participantId}
                            variant="outlined"
                            sx={{
                              borderRadius: 2,
                              p: 1.5,
                              backgroundColor: '#fffdf8',
                            }}
                          >
                            <Typography variant="subtitle2">{participant.displayName}</Typography>
                            <Typography variant="body2" color="text.secondary">
                              {`${locale.positionRow}: ${locale.rows[participant.position.row]} / ${locale.positionColumn}: ${locale.columns[participant.position.column]}`}
                            </Typography>
                          </Paper>
                        ))}
                      </Stack>
                    </Stack>
                  )}
                </Stack>
              </Paper>

              {currentRoom == null ? (
                <QuestMultiRoomList
                  rooms={latestRooms ?? []}
                  isLoading={isRoomsLoading}
                  error={roomsError instanceof Error ? roomsError : null}
                  isJoiningRoomId={isJoiningRoomId}
                  locale={locale}
                  onJoinRoom={handleJoinRoom}
                />
              ) : isJoinedRoomOwner ? (
                <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                  <Stack spacing={2}>
                    <div>
                      <Typography variant="h5">{locale.ownerPositionTitle}</Typography>
                      <Typography variant="body2" color="text.secondary">
                        {locale.ownerPositionSubtitle}
                      </Typography>
                    </div>
                    {currentRoom.participants.map((participant) => {
                      const draft = positionDrafts[participant.participantId] ?? participant.position

                      return (
                        <Paper
                          key={participant.participantId}
                          variant="outlined"
                          sx={{
                            borderRadius: 2,
                            p: 1.5,
                            backgroundColor: '#fffdf8',
                          }}
                        >
                          <Stack spacing={1.5}>
                            <Typography variant="subtitle2">{participant.displayName}</Typography>
                            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                              <FormControl fullWidth sx={greenOutlinedInputSx}>
                                <InputLabel id={`participant-row-${participant.participantId}`}>
                                  {locale.positionRow}
                                </InputLabel>
                                <Select
                                  labelId={`participant-row-${participant.participantId}`}
                                  value={draft.row}
                                  label={locale.positionRow}
                                  onChange={(event) => {
                                    setPositionDrafts((current) => ({
                                      ...current,
                                      [participant.participantId]: {
                                        row: event.target.value as BattleRow,
                                        column: (current[participant.participantId] ?? participant.position).column,
                                      },
                                    }))
                                  }}
                                >
                                  <MenuItem value="Front">{locale.rows.Front}</MenuItem>
                                  <MenuItem value="Middle">{locale.rows.Middle}</MenuItem>
                                  <MenuItem value="Back">{locale.rows.Back}</MenuItem>
                                </Select>
                              </FormControl>
                              <FormControl fullWidth sx={greenOutlinedInputSx}>
                                <InputLabel id={`participant-column-${participant.participantId}`}>
                                  {locale.positionColumn}
                                </InputLabel>
                                <Select
                                  labelId={`participant-column-${participant.participantId}`}
                                  value={draft.column}
                                  label={locale.positionColumn}
                                  onChange={(event) => {
                                    setPositionDrafts((current) => ({
                                      ...current,
                                      [participant.participantId]: {
                                        row: (current[participant.participantId] ?? participant.position).row,
                                        column: event.target.value as BattleColumn,
                                      },
                                    }))
                                  }}
                                >
                                  <MenuItem value="Left">{locale.columns.Left}</MenuItem>
                                  <MenuItem value="Right">{locale.columns.Right}</MenuItem>
                                </Select>
                              </FormControl>
                            </Stack>
                            <Button
                              variant="contained"
                              onClick={() => void handleUpdateParticipantPosition(participant.participantId)}
                              disabled={isUpdatingParticipantId === participant.participantId}
                              sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                            >
                              {isUpdatingParticipantId === participant.participantId
                                ? locale.updatingPosition
                                : locale.savePosition}
                            </Button>
                          </Stack>
                        </Paper>
                      )
                    })}
                  </Stack>
                </Paper>
              ) : (
                <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                  <Stack spacing={2}>
                    <Typography variant="h5">{locale.waitingForOwnerTitle}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {locale.waitingForOwnerSubtitle}
                    </Typography>
                  </Stack>
                </Paper>
              )}

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={2}>
                  <Typography variant="h5">{locale.questRunTitle}</Typography>
                  {!currentRoom || currentRoom.closeReason !== 'Started' ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.questRunEmpty}
                    </Typography>
                  ) : isRunLoading && !currentRun ? (
                    <Stack direction="row" spacing={1} alignItems="center">
                      <CircularProgress size={18} />
                      <Typography variant="body2">{locale.runLoading}</Typography>
                    </Stack>
                  ) : currentRun ? (
                    <Stack spacing={2}>
                      <Typography>{`${locale.runId}: ${currentRun.runId}`}</Typography>
                      <Typography>{`${locale.runStatusLabel}: ${locale.runStatus[currentRun.status]}`}</Typography>
                      <Typography>{`${locale.currentFloorLabel}: ${currentRun.floor.currentFloorNo}`}</Typography>
                      <Typography>{`${locale.turnNo}: ${currentRun.turn.currentTurnNo}`}</Typography>
                      <QuestBattleStatusPanel run={currentRun} selfParticipantId={selfParticipantId} locale={locale} />
                    </Stack>
                  ) : (
                    <Typography variant="body2" color="text.secondary">
                      {locale.questRunEmpty}
                    </Typography>
                  )}
                </Stack>
              </Paper>

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={2}>
                  <Typography variant="h5">{locale.commandPanelTitle}</Typography>
                  {!currentRun ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.commandPanelEmpty}
                    </Typography>
                  ) : (
                    <Stack spacing={2}>
                      <Typography variant="body2" color="text.secondary">
                        {`${locale.currentTurnLabel}: ${currentRun.turn.currentTurnNo}`}
                      </Typography>

                      <FormControl fullWidth sx={greenOutlinedInputSx}>
                        <InputLabel id="quest-mult-action-kind-select-label">{locale.labels.actionKind}</InputLabel>
                        <Select
                          labelId="quest-mult-action-kind-select-label"
                          value={selectedActionKind}
                          label={locale.labels.actionKind}
                          onChange={(event) => {
                            setSelectedActionKind(event.target.value as QuestActionKind)
                          }}
                        >
                          <MenuItem value="NormalAttack">{locale.actionKinds.NormalAttack}</MenuItem>
                          <MenuItem value="UseMove">{locale.actionKinds.UseMove}</MenuItem>
                          <MenuItem value="Guard">{locale.actionKinds.Guard}</MenuItem>
                          <MenuItem value="Wait">{locale.actionKinds.Wait}</MenuItem>
                          <MenuItem value="LeaveQuest">{locale.actionKinds.LeaveQuest}</MenuItem>
                          <MenuItem value="Escape">{locale.actionKinds.Escape}</MenuItem>
                        </Select>
                      </FormControl>

                      <FormControl fullWidth sx={greenOutlinedInputSx} disabled={selectedActionKind !== 'UseMove'}>
                        <InputLabel id="quest-mult-move-select-label">{locale.labels.move}</InputLabel>
                        <Select
                          labelId="quest-mult-move-select-label"
                          value={selectedMoveId}
                          label={locale.labels.move}
                          onChange={(event) => {
                            const nextValue = event.target.value
                            setSelectedMoveId(nextValue === '' ? '' : Number(nextValue))
                          }}
                        >
                          <MenuItem value="">{locale.labels.none}</MenuItem>
                          {availableMoves.map((move) => (
                            <MenuItem key={move.slot} value={move.moveId!}>
                              {move.moveName}
                            </MenuItem>
                          ))}
                        </Select>
                      </FormControl>

                      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2}>
                        <FormControl fullWidth sx={greenOutlinedInputSx}>
                          <InputLabel id="quest-mult-target-row-select-label">{locale.labels.targetRow}</InputLabel>
                          <Select
                            labelId="quest-mult-target-row-select-label"
                            value={selectedTargetRow}
                            label={locale.labels.targetRow}
                            onChange={(event) => {
                              setSelectedTargetRow(event.target.value as BattleRow | '')
                            }}
                          >
                            <MenuItem value="">{locale.labels.none}</MenuItem>
                            <MenuItem value="Front">{locale.rows.Front}</MenuItem>
                            <MenuItem value="Middle">{locale.rows.Middle}</MenuItem>
                            <MenuItem value="Back">{locale.rows.Back}</MenuItem>
                          </Select>
                        </FormControl>

                        <FormControl fullWidth sx={greenOutlinedInputSx}>
                          <InputLabel id="quest-mult-target-column-select-label">
                            {locale.labels.targetColumn}
                          </InputLabel>
                          <Select
                            labelId="quest-mult-target-column-select-label"
                            value={selectedTargetColumn}
                            label={locale.labels.targetColumn}
                            onChange={(event) => {
                              setSelectedTargetColumn(event.target.value as BattleColumn | '')
                            }}
                          >
                            <MenuItem value="">{locale.labels.none}</MenuItem>
                            <MenuItem value="Left">{locale.columns.Left}</MenuItem>
                            <MenuItem value="Right">{locale.columns.Right}</MenuItem>
                          </Select>
                        </FormControl>
                      </Stack>

                      <Button
                        variant="contained"
                        onClick={() => void handleSubmitCommand()}
                        disabled={isCommandSubmitting || selfParticipantId == null}
                        sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                      >
                        {isCommandSubmitting ? locale.submittingCommand : locale.submitCommand}
                      </Button>

                      {currentPendingCommand ? (
                        <Paper
                          variant="outlined"
                          sx={{
                            borderRadius: 2,
                            p: 1.5,
                            backgroundColor: '#fffdf8',
                          }}
                        >
                          <Typography variant="subtitle2">{locale.pendingCommandTitle}</Typography>
                          <Typography variant="body2" color="text.secondary">
                            {`${locale.labels.actionKind}: ${locale.actionKinds[currentPendingCommand.actionKind]}`}
                          </Typography>
                        </Paper>
                      ) : null}
                    </Stack>
                  )}
                </Stack>
              </Paper>
            </Stack>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
