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
  cancelQuestRoom,
  createQuestRoom,
  escapeQuestRun,
  getQuestRun,
  getQuestStages,
  startQuestRoom,
  submitQuestCommand,
} from '@/api/quest'
import QuestBattleStatusPanel from '@/components/quest/QuestBattleStatusPanel'
import QuestLastTurnResultsPanel from '@/components/quest/QuestLastTurnResultsPanel'
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
import locale from '../../locale/quest/QuestRoom.json'
import type {
  BattleColumn,
  BattleRow,
  CreateQuestRoomRequest,
  GetQuestStagesResponse,
  QuestActionKind,
  QuestRoomDetailResponse,
  QuestRunDetailResponse,
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

export default function Quest() {
  const { session, isLoading } = useAuth()
  const [selectedStageId, setSelectedStageId] = useState<number | ''>('')
  const [mode, setMode] = useState<CreateQuestRoomRequest['mode']>('Solo')
  const [createdRoom, setCreatedRoom] = useState<QuestRoomDetailResponse | null>(null)
  const [startedRun, setStartedRun] = useState<QuestRunDetailResponse | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isStarting, setIsStarting] = useState(false)
  const [isCancellingRoom, setIsCancellingRoom] = useState(false)
  const [selectedActionKind, setSelectedActionKind] = useState<QuestActionKind>('NormalAttack')
  const [selectedMoveId, setSelectedMoveId] = useState<number | ''>('')
  const [selectedTargetRow, setSelectedTargetRow] = useState<BattleRow | ''>('')
  const [selectedTargetColumn, setSelectedTargetColumn] = useState<BattleColumn | ''>('')
  const [commandMessage, setCommandMessage] = useState<string | null>(null)
  const [isCommandSubmitting, setIsCommandSubmitting] = useState(false)
  const [isEscaping, setIsEscaping] = useState(false)

  const playerSWRKey = session?.user.id ? ([`quest-player`, session.user.id] as const) : null
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

  const stagesSWRKey = session?.access_token ? ([`quest-stages`] as const) : null
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

  useEffect(() => {
    if (selectedStageId !== '' || activeStages.length === 0) {
      return
    }

    setSelectedStageId(activeStages[0]!.stageId)
  }, [activeStages, selectedStageId])

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
          mode,
        },
        session.access_token,
      )
      setCreatedRoom(room)
      setStartedRun(null)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.createRoomFailed)
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleStartQuest(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!createdRoom) {
      setSubmitError(locale.createRoomFirst)
      return
    }

    setIsStarting(true)
    setSubmitError(null)

    try {
      const run = await startQuestRoom(createdRoom.roomId, session.access_token)
      setStartedRun(run)
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
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.startQuestFailed)
    } finally {
      setIsStarting(false)
    }
  }

  async function handleCancelRoom(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!createdRoom) {
      setSubmitError(locale.createRoomFirst)
      return
    }

    setIsCancellingRoom(true)
    setSubmitError(null)
    setCommandMessage(null)

    try {
      const room = await cancelQuestRoom(createdRoom.roomId, session.access_token)
      setCreatedRoom(room)
      setCommandMessage(locale.roomCancelled)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.cancelRoomFailed)
    } finally {
      setIsCancellingRoom(false)
    }
  }

  const runSWRKey = session?.access_token && startedRun?.runId ? ([`quest-run`, startedRun.runId] as const) : null
  const {
    data: liveRun,
    error: runError,
    isLoading: isRunLoading,
    mutate: mutateRun,
  } = useSWR(
    runSWRKey,
    async () => {
      if (!session?.access_token || !startedRun?.runId) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getQuestRun(startedRun.runId, session.access_token)
    },
    {
      refreshInterval: startedRun?.status === 'InProgress' ? 2000 : 0,
    },
  )

  const currentRun = liveRun ?? startedRun
  const selfParticipantId =
    player && createdRoom
      ? (createdRoom.participants.find((participant) => participant.playerId === player.userId)?.participantId ?? null)
      : null
  const availableMoves = useMemo(() => (player?.moveSlots ?? []).filter((slot) => slot.moveId != null), [player])
  const firstEnemyPosition = currentRun?.enemies[0]?.position ?? null
  const currentPendingCommand =
    currentRun && selfParticipantId
      ? (currentRun.pendingCommands.find(
          (command) => command.participantId === selfParticipantId && command.turnNo === currentRun.turn.currentTurnNo,
        ) ?? null)
      : null
  const canSubmitCurrentTurn =
    currentRun != null &&
    selfParticipantId != null &&
    currentRun.status === 'InProgress' &&
    currentRun.turn.waitingParticipantIds.includes(selfParticipantId) &&
    currentPendingCommand == null
  const isRunFinished = currentRun != null && currentRun.status !== 'InProgress'

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

  async function handleSubmitCommand(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!currentRun || !selfParticipantId) {
      setSubmitError(locale.commandUnavailable)
      return
    }

    if (!currentRun.turn.waitingParticipantIds.includes(selfParticipantId) || currentPendingCommand != null) {
      setSubmitError(locale.commandAlreadyConfirmed)
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

  async function handleEscapeRun(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!currentRun) {
      setSubmitError(locale.commandUnavailable)
      return
    }

    setIsEscaping(true)
    setSubmitError(null)
    setCommandMessage(null)

    try {
      const run = await escapeQuestRun(currentRun.runId, session.access_token)
      setStartedRun(run)
      await mutateRun(run, { revalidate: false })
      setCommandMessage(locale.runEscaped)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.escapeRunFailed)
    } finally {
      setIsEscaping(false)
    }
  }

  function handleLeaveFinishedRun(): void {
    setCreatedRoom(null)
    setStartedRun(null)
    setSubmitError(null)
    setCommandMessage(null)
    setSelectedActionKind('NormalAttack')
    setSelectedMoveId('')
    setSelectedTargetRow('')
    setSelectedTargetColumn('')
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
                        <InputLabel id="quest-stage-select-label">{locale.labels.stageSelect}</InputLabel>
                        <Select
                          labelId="quest-stage-select-label"
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

                      <FormControl fullWidth sx={greenOutlinedInputSx}>
                        <InputLabel id="quest-mode-select-label">{locale.labels.modeSelect}</InputLabel>
                        <Select
                          labelId="quest-mode-select-label"
                          value={mode}
                          label={locale.labels.modeSelect}
                          onChange={(event) => {
                            setMode(event.target.value as CreateQuestRoomRequest['mode'])
                          }}
                        >
                          <MenuItem value="Solo">{locale.modeSolo}</MenuItem>
                          <MenuItem value="Multi">{locale.modeMulti}</MenuItem>
                        </Select>
                      </FormControl>

                      {selectedStage ? (
                        <Paper
                          variant="outlined"
                          sx={{
                            ...innerSurfaceSx,
                            borderRadius: 3,
                            p: 2,
                          }}
                        >
                          <Stack spacing={1}>
                            <Typography variant="h6">{selectedStage.name}</Typography>
                            <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                              <Chip label={`${locale.recommendedLevel}: ${selectedStage.recommendedLevel}`} />
                              <Chip label={`${locale.partyRange}: ${formatStagePartyRange(selectedStage)}`} />
                              <Chip label={`${locale.floorCount}: ${selectedStage.floors.length}`} />
                            </Stack>
                          </Stack>
                        </Paper>
                      ) : null}

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
                  {createdRoom == null ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.createdRoomEmpty}
                    </Typography>
                  ) : (
                    <Stack spacing={1.5}>
                      <Typography>{`${locale.roomId}: ${createdRoom.roomId}`}</Typography>
                      <Typography>{`${locale.stage}: ${createdRoom.stageId}`}</Typography>
                      <Typography>{`${locale.createdAt}: ${formatDateTime(createdRoom.createdAt)}`}</Typography>
                      <Typography>{`${locale.roomVersion}: ${createdRoom.version}`}</Typography>
                      <Typography>{`${locale.participants}: ${createdRoom.participants.length}`}</Typography>
                      <Typography>{`${locale.canStartLabel}: ${createdRoom.canStart ? locale.yes : locale.no}`}</Typography>
                      <Typography>{`${locale.roomStatusLabel}: ${locale.roomStatus[createdRoom.status]}`}</Typography>
                      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                        <Button
                          variant="contained"
                          onClick={() => void handleStartQuest()}
                          disabled={
                            isStarting ||
                            isCancellingRoom ||
                            !createdRoom.canStart ||
                            createdRoom.mode !== 'Solo' ||
                            createdRoom.status !== 'Recruiting'
                          }
                          sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                        >
                          {isStarting ? locale.startingQuest : locale.startQuest}
                        </Button>
                        <Button
                          variant="outlined"
                          color="inherit"
                          onClick={() => void handleCancelRoom()}
                          disabled={isStarting || isCancellingRoom || createdRoom.status !== 'Recruiting'}
                          sx={menuButtonSx}
                        >
                          {isCancellingRoom ? locale.cancellingRoom : locale.cancelRoom}
                        </Button>
                      </Stack>
                      {createdRoom.mode !== 'Solo' ? (
                        <Typography variant="body2" color="text.secondary">
                          {locale.startSoloOnly}
                        </Typography>
                      ) : null}
                      <Stack spacing={1}>
                        {createdRoom.participants.map((participant) => (
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

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={2}>
                  <Typography variant="h5">{locale.startedRunTitle}</Typography>
                  {startedRun == null ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.startedRunEmpty}
                    </Typography>
                  ) : (
                    <Stack spacing={1.5}>
                      <Typography>{`${locale.runId}: ${startedRun.runId}`}</Typography>
                      <Typography>{`${locale.stage}: ${startedRun.stageId}`}</Typography>
                      <Typography>{`${locale.runStatusLabel}: ${locale.runStatus[startedRun.status]}`}</Typography>
                      <Typography>{`${locale.currentFloorLabel}: ${startedRun.floor.currentFloorNo}`}</Typography>
                      <Typography>{`${locale.turnNo}: ${startedRun.turn.currentTurnNo}`}</Typography>
                      <Typography>{`${locale.deadline}: ${formatDateTime(startedRun.turn.actionDeadlineAt)}`}</Typography>
                      <Typography>{`${locale.waitingParticipantsLabel}: ${startedRun.turn.waitingParticipantIds.length}`}</Typography>
                      <Typography>{`${locale.partyMembersLabel}: ${startedRun.partyMembers.length}`}</Typography>
                      <Typography>{`${locale.enemiesLabel}: ${startedRun.enemies.length}`}</Typography>
                      <Stack spacing={1}>
                        {startedRun.partyMembers.map((member) => (
                          <Paper
                            key={member.participantId}
                            variant="outlined"
                            sx={{
                              borderRadius: 2,
                              p: 1.5,
                              backgroundColor: '#fffdf8',
                            }}
                          >
                            <Typography variant="subtitle2">{member.displayName}</Typography>
                            <Typography variant="body2" color="text.secondary">
                              {`${locale.positionRow}: ${locale.rows[member.position.row]} / ${locale.positionColumn}: ${locale.columns[member.position.column]}`}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              {`HP ${member.currentHp} / MP ${member.currentMp}`}
                            </Typography>
                          </Paper>
                        ))}
                      </Stack>
                    </Stack>
                  )}
                </Stack>
              </Paper>

              <QuestLastTurnResultsPanel run={currentRun} locale={locale} />

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={2}>
                  <Typography variant="h5">{locale.commandPanelTitle}</Typography>
                  {!currentRun ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.commandPanelEmpty}
                    </Typography>
                  ) : isRunLoading ? (
                    <Stack direction="row" spacing={1} alignItems="center">
                      <CircularProgress size={18} />
                      <Typography variant="body2">{locale.runLoading}</Typography>
                    </Stack>
                  ) : (
                    <Stack spacing={2}>
                      <Typography variant="body2" color="text.secondary">
                        {`${locale.currentTurnLabel}: ${currentRun.turn.currentTurnNo}`}
                      </Typography>
                      <FormControl fullWidth sx={greenOutlinedInputSx}>
                        <InputLabel id="quest-action-kind-select-label">{locale.labels.actionKind}</InputLabel>
                        <Select
                          labelId="quest-action-kind-select-label"
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
                        <InputLabel id="quest-move-select-label">{locale.labels.move}</InputLabel>
                        <Select
                          labelId="quest-move-select-label"
                          value={selectedMoveId === '' ? '' : String(selectedMoveId)}
                          label={locale.labels.move}
                          onChange={(event) => {
                            const nextValue = String(event.target.value)
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
                          <InputLabel id="quest-target-row-select-label">{locale.labels.targetRow}</InputLabel>
                          <Select
                            labelId="quest-target-row-select-label"
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
                          <InputLabel id="quest-target-column-select-label">{locale.labels.targetColumn}</InputLabel>
                          <Select
                            labelId="quest-target-column-select-label"
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
                        disabled={isCommandSubmitting || isEscaping || !canSubmitCurrentTurn}
                        sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                      >
                        {isCommandSubmitting ? locale.submittingCommand : locale.submitCommand}
                      </Button>
                      <Button
                        variant="outlined"
                        color="inherit"
                        onClick={() => void handleEscapeRun()}
                        disabled={isEscaping || isCommandSubmitting || currentRun.status !== 'InProgress'}
                        sx={menuButtonSx}
                      >
                        {isEscaping ? locale.escapingRun : locale.escapeRun}
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
                          <Typography variant="body2" color="text.secondary">
                            {`${locale.submittedAtLabel}: ${formatDateTime(currentPendingCommand.submittedAt)}`}
                          </Typography>
                        </Paper>
                      ) : null}
                    </Stack>
                  )}
                </Stack>
              </Paper>

              {currentRun ? (
                <QuestBattleStatusPanel run={currentRun} selfParticipantId={selfParticipantId} locale={locale} />
              ) : null}

              {isRunFinished ? (
                <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                  <Stack spacing={2}>
                    <Typography variant="h5">{locale.finishedRunActionsTitle}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {locale.finishedRunActionsSubtitle}
                    </Typography>
                    <Button
                      variant="contained"
                      onClick={handleLeaveFinishedRun}
                      sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                    >
                      {locale.leaveFinishedRun}
                    </Button>
                  </Stack>
                </Paper>
              ) : null}
            </Stack>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
