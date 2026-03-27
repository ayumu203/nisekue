import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useEffect, useMemo, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import {
  cancelQuestRoom,
  createQuestRoom,
  getActiveQuestRun,
  getQuestRoom,
  getQuestRunByRoom,
  getQuestRun,
  getQuestStages,
  joinQuestRoom,
  listQuestRooms,
  postQuestChatMessage,
  startQuestRoom,
  submitQuestCommand,
  updateQuestRoomPosition,
} from '@/api/quest'
import QuestRoomCreateSection from '@/components/quest/QuestRoomCreateSection'
import QuestRoomLobbySection from '@/components/quest/QuestRoomLobbySection'
import QuestMultiRoomList from '@/components/quest/QuestMultiRoomList'
import QuestRunSection from '@/components/quest/QuestRunSection'
import Status from '@/components/home/Status'
import { useAuth } from '@/contexts/useAuth'
import { menuButtonSx, outerPagePaperSx, twoColumnContentGridSx } from '@/constants/styles'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/quest/QuestRoom.json'
import type {
  BattleColumn,
  BattleRow,
  CreateQuestRoomRequest,
  QuestActionKind,
  QuestRoomDetailResponse,
  QuestRunDetailResponse,
} from '@/schema/quest'

const battleRowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const battleColumnOrder: BattleColumn[] = ['Left', 'Right']
const questSessionStorageKeyPrefix = 'nisekue:quest-session'

type PersistedQuestSession = {
  roomId: string | null
  runId: string | null
}

function getQuestSessionStorageKey(userId: string): string {
  return `${questSessionStorageKeyPrefix}:${userId}`
}

function readPersistedQuestSession(userId: string): PersistedQuestSession | null {
  if (typeof window === 'undefined') {
    return null
  }

  const raw = window.localStorage.getItem(getQuestSessionStorageKey(userId))
  if (!raw) {
    return null
  }

  try {
    const parsed = JSON.parse(raw) as Partial<PersistedQuestSession>
    return {
      roomId: typeof parsed.roomId === 'string' ? parsed.roomId : null,
      runId: typeof parsed.runId === 'string' ? parsed.runId : null,
    }
  } catch {
    window.localStorage.removeItem(getQuestSessionStorageKey(userId))
    return null
  }
}

function writePersistedQuestSession(userId: string, value: PersistedQuestSession): void {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.setItem(getQuestSessionStorageKey(userId), JSON.stringify(value))
}

function clearPersistedQuestSession(userId: string): void {
  if (typeof window === 'undefined') {
    return
  }

  window.localStorage.removeItem(getQuestSessionStorageKey(userId))
}

export default function Quest() {
  const { session, isLoading } = useAuth()
  const [selectedStageId, setSelectedStageId] = useState<number | ''>('')
  const [mode, setMode] = useState<CreateQuestRoomRequest['mode']>('Solo')
  const [multiEntryView, setMultiEntryView] = useState<'create' | 'list'>('create')
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
  const [isCommandSubmitting, setIsCommandSubmitting] = useState(false)
  const [positionDrafts, setPositionDrafts] = useState<Record<string, { row: BattleRow; column: BattleColumn }>>({})
  const [isUpdatingParticipantId, setIsUpdatingParticipantId] = useState<string | null>(null)
  const [isJoiningRoomId, setIsJoiningRoomId] = useState<string | null>(null)
  const [chatMessage, setChatMessage] = useState('')
  const [isChatSubmitting, setIsChatSubmitting] = useState(false)
  const [isRecoveringQuest, setIsRecoveringQuest] = useState(false)
  const [hasTriedQuestRecovery, setHasTriedQuestRecovery] = useState(false)
  const hasAttemptedQuestRecoveryRef = useRef(false)

  useEffect(() => {
    hasAttemptedQuestRecoveryRef.current = false
    setHasTriedQuestRecovery(false)
    setIsRecoveringQuest(false)
  }, [session?.user.id])

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

  const activeStages = useMemo(
    () =>
      [...(stages ?? [])]
        .filter((stage) => stage.isActive)
        .sort((left, right) => left.recommendedLevel - right.recommendedLevel),
    [stages],
  )
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

  useEffect(() => {
    if (mode === 'Solo') {
      setMultiEntryView('create')
    }
  }, [mode])

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
      setMultiEntryView('create')
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.createRoomFailed)
    } finally {
      setIsSubmitting(false)
    }
  }

  const roomsSWRKey =
    session?.access_token && mode === 'Multi' && createdRoom == null && startedRun == null
      ? ([`quest-rooms`, mode] as const)
      : null
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
        pageSize: 10,
      },
      session.access_token,
    )

    return [...rooms].sort((left, right) => new Date(right.createdAt).getTime() - new Date(left.createdAt).getTime())
  })

  const roomSWRKey =
    session?.access_token && createdRoom?.roomId && startedRun == null
      ? ([`quest-room`, createdRoom.roomId] as const)
      : null
  const {
    data: liveRoom,
    error: roomError,
    isLoading: isRoomLoading,
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
      refreshInterval: createdRoom?.status === 'Recruiting' && startedRun == null ? 2000 : 0,
    },
  )

  const currentRoom = liveRoom ?? createdRoom

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
      setStartedRun(null)
      setMultiEntryView('list')
      await mutateRoom(room, { revalidate: false })
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.joinRoomFailed)
    } finally {
      setIsJoiningRoomId(null)
    }
  }

  async function handleStartQuest(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!currentRoom) {
      setSubmitError(locale.createRoomFirst)
      return
    }

    setIsStarting(true)
    setSubmitError(null)

    try {
      const run = await startQuestRoom(currentRoom.roomId, session.access_token)
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
      await mutateRooms()
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

    if (!currentRoom) {
      setSubmitError(locale.createRoomFirst)
      return
    }

    setIsCancellingRoom(true)
    setSubmitError(null)

    try {
      const room = await cancelQuestRoom(currentRoom.roomId, session.access_token)
      setCreatedRoom(room)
      await mutateRoom(room, { revalidate: false })
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.cancelRoomFailed)
    } finally {
      setIsCancellingRoom(false)
    }
  }

  const runSWRKey =
    session?.access_token && startedRun?.runId
      ? ([`quest-run`, startedRun.runId] as const)
      : session?.access_token && currentRoom?.roomId && currentRoom.closeReason === 'Started'
        ? ([`quest-room-run`, currentRoom.roomId] as const)
        : null
  const {
    data: liveRun,
    error: runError,
    mutate: mutateRun,
  } = useSWR(
    runSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionInfoMissing)
      }

      if (startedRun?.runId) {
        return getQuestRun(startedRun.runId, session.access_token)
      }

      if (currentRoom?.roomId && currentRoom.closeReason === 'Started') {
        return getQuestRunByRoom(currentRoom.roomId, session.access_token)
      }

      throw new Error(locale.commandUnavailable)
    },
    {
      refreshInterval: currentRoom?.closeReason === 'Started' || startedRun?.status === 'InProgress' ? 2000 : 0,
    },
  )

  const currentRun = liveRun ?? startedRun
  const currentRoomStage = currentRoom
    ? (activeStages.find((stage) => stage.stageId === currentRoom.stageId) ?? null)
    : null
  const selfParticipantId =
    player && currentRoom
      ? (currentRoom.participants.find((participant) => participant.playerId === player.userId)?.participantId ?? null)
      : null
  const availableMoves = useMemo(
    () =>
      (player?.moveSlots ?? [])
        .filter((slot) => slot.moveId != null && slot.moveName != null)
        .map((slot) => ({
          slot: slot.slot,
          moveId: slot.moveId,
          moveName: slot.moveName!,
          targetType: slot.targetType,
          attackRange: slot.attackRange,
        })),
    [player],
  )
  const selectedMove = useMemo(
    () => availableMoves.find((move) => move.moveId === selectedMoveId) ?? null,
    [availableMoves, selectedMoveId],
  )
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
  const reachableEnemyPositions = useMemo(() => {
    if (!currentRun || !selfParticipantId) {
      return []
    }

    const selfPartyMember = currentRun.partyMembers.find((member) => member.participantId === selfParticipantId)
    if (!selfPartyMember) {
      return []
    }

    const aliveEnemies = currentRun.enemies
      .filter((enemy) => !enemy.isDead)
      .sort((left, right) => {
        if (left.position.row !== right.position.row) {
          return battleRowOrder.indexOf(left.position.row) - battleRowOrder.indexOf(right.position.row)
        }

        return battleColumnOrder.indexOf(left.position.column) - battleColumnOrder.indexOf(right.position.column)
      })

    const occupiedRows = Array.from(new Set(aliveEnemies.map((enemy) => enemy.position.row)))
    const reachableRows = new Set<BattleRow>(
      occupiedRows.slice(
        0,
        selfPartyMember.position.row === 'Front'
          ? 1
          : selfPartyMember.position.row === 'Middle'
            ? 2
            : occupiedRows.length,
      ),
    )

    return aliveEnemies.filter((enemy) => reachableRows.has(enemy.position.row)).map((enemy) => enemy.position)
  }, [currentRun, selfParticipantId])
  const isEnemyTargetingAction =
    selectedActionKind === 'NormalAttack' || (selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Enemy')

  useEffect(() => {
    if (!session?.user.id) {
      return
    }

    if (currentRun) {
      writePersistedQuestSession(session.user.id, {
        roomId: currentRun.roomId,
        runId: currentRun.runId,
      })
      return
    }

    if (currentRoom?.status === 'Recruiting') {
      writePersistedQuestSession(session.user.id, {
        roomId: currentRoom.roomId,
        runId: null,
      })
      return
    }

    clearPersistedQuestSession(session.user.id)
  }, [currentRoom, currentRun, session?.user.id])

  useEffect(() => {
    if (!session?.access_token || !player || isPlayerLoading || hasAttemptedQuestRecoveryRef.current) {
      return
    }

    if (createdRoom || startedRun || liveRoom || liveRun) {
      setHasTriedQuestRecovery(true)
      hasAttemptedQuestRecoveryRef.current = true
      return
    }

    hasAttemptedQuestRecoveryRef.current = true

    const recoverQuest = async () => {
      setIsRecoveringQuest(true)

      try {
        try {
          const activeRun = await getActiveQuestRun(session.access_token)
          const room = await getQuestRoom(activeRun.roomId, session.access_token)
          setCreatedRoom(room)
          setStartedRun(activeRun)
          return
        } catch (error) {
          const message = error instanceof Error ? error.message : ''
          if (message && !message.includes('進行中クエストが見つかりません')) {
            throw error
          }
        }

        const persistedQuestSession = readPersistedQuestSession(player.userId)
        if (!persistedQuestSession?.roomId) {
          return
        }

        try {
          const room = await getQuestRoom(persistedQuestSession.roomId, session.access_token)
          const isJoinedParticipant = room.participants.some(
            (participant) => participant.playerId === player.userId && participant.status !== 'Left',
          )

          if (!isJoinedParticipant) {
            clearPersistedQuestSession(player.userId)
            return
          }

          setCreatedRoom(room)

          if (room.closeReason !== 'Started') {
            return
          }

          const run = await getQuestRunByRoom(room.roomId, session.access_token)
          setStartedRun(run)
        } catch (error) {
          clearPersistedQuestSession(player.userId)

          const message = error instanceof Error ? error.message : ''
          if (
            message &&
            !message.includes('ルームが見つかりません') &&
            !message.includes('進行中クエストが見つかりません')
          ) {
            throw error
          }
        }
      } catch (error) {
        const message = error instanceof Error ? error.message : ''
        if (message) {
          setSubmitError(message)
        }
      } finally {
        setIsRecoveringQuest(false)
        setHasTriedQuestRecovery(true)
      }
    }

    void recoverQuest()
  }, [createdRoom, isPlayerLoading, liveRoom, liveRun, player, session?.access_token, startedRun])

  useEffect(() => {
    if (!isEnemyTargetingAction) {
      return
    }

    const currentTargetIsReachable =
      selectedTargetRow !== '' &&
      selectedTargetColumn !== '' &&
      reachableEnemyPositions.some(
        (position) => position.row === selectedTargetRow && position.column === selectedTargetColumn,
      )

    if (currentTargetIsReachable) {
      return
    }

    const nextTarget = reachableEnemyPositions[0]
    if (!nextTarget) {
      return
    }

    setSelectedTargetRow(nextTarget.row)
    setSelectedTargetColumn(nextTarget.column)
  }, [isEnemyTargetingAction, reachableEnemyPositions, selectedTargetColumn, selectedTargetRow])

  useEffect(() => {
    if (selectedActionKind !== 'UseMove') {
      setSelectedMoveId('')
    }
  }, [selectedActionKind])

  useEffect(() => {
    if (!currentRoom) {
      setPositionDrafts({})
      return
    }

    setPositionDrafts((current) =>
      Object.fromEntries(
        currentRoom.participants.map((participant) => [
          participant.participantId,
          current[participant.participantId] ?? {
            row: participant.position.row,
            column: participant.position.column,
          },
        ]),
      ),
    )
  }, [currentRoom])

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

    try {
      const selectedTargetPosition = isEnemyTargetingAction
        ? (reachableEnemyPositions.find(
            (position) => position.row === selectedTargetRow && position.column === selectedTargetColumn,
          ) ??
          reachableEnemyPositions[0] ??
          null)
        : selectedTargetRow !== '' && selectedTargetColumn !== ''
          ? {
              row: selectedTargetRow,
              column: selectedTargetColumn,
            }
          : null

      const requestTargetPosition =
        selectedTargetPosition != null
          ? {
              targetRow: selectedTargetPosition.row,
              targetColumn: selectedTargetPosition.column,
            }
          : {
              targetRow: null,
              targetColumn: null,
            }

      await submitQuestCommand(
        currentRun.runId,
        {
          participantId: selfParticipantId,
          turnNo: currentRun.turn.currentTurnNo,
          actionKind: selectedActionKind,
          moveId: selectedActionKind === 'UseMove' && selectedMoveId !== '' ? selectedMoveId : null,
          targetRow: requestTargetPosition.targetRow,
          targetColumn: requestTargetPosition.targetColumn,
        },
        session.access_token,
      )
      await mutateRun()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.commandSubmitFailed)
    } finally {
      setIsCommandSubmitting(false)
    }
  }

  async function handleSubmitChatMessage(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!currentRun || !selfParticipantId) {
      setSubmitError(locale.commandUnavailable)
      return
    }

    const normalizedMessage = chatMessage.trim()
    if (normalizedMessage.length === 0) {
      return
    }

    setIsChatSubmitting(true)
    setSubmitError(null)

    try {
      await postQuestChatMessage(
        currentRun.runId,
        {
          participantId: selfParticipantId,
          message: normalizedMessage,
        },
        session.access_token,
      )
      setChatMessage('')
      await mutateRun()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.chatSubmitFailed)
    } finally {
      setIsChatSubmitting(false)
    }
  }

  function handleLeaveFinishedRun(): void {
    if (session?.user.id) {
      clearPersistedQuestSession(session.user.id)
    }

    setCreatedRoom(null)
    setStartedRun(null)
    setSubmitError(null)
    setChatMessage('')
    setSelectedActionKind('NormalAttack')
    setSelectedMoveId('')
    setSelectedTargetRow('')
    setSelectedTargetColumn('')
  }

  const showCreateSection = currentRoom == null && currentRun == null && !isRecoveringQuest && hasTriedQuestRecovery
  const showWaitingSection = currentRoom != null && currentRun == null
  const showRunSection = currentRun != null

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
    <Container maxWidth="lg" sx={{ px: { xs: 1, sm: 3 }, py: { xs: 1.25, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.25, sm: 2 }}>
          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.25, sm: 2 }}>
              <Status player={player} />
            </Stack>

            <Stack spacing={{ xs: 1.25, sm: 2 }}>
              {playerError ? <Alert severity="warning">{playerError.message}</Alert> : null}
              {stagesError ? <Alert severity="warning">{stagesError.message}</Alert> : null}
              {roomsError ? <Alert severity="warning">{roomsError.message}</Alert> : null}
              {roomError ? <Alert severity="warning">{roomError.message}</Alert> : null}
              {runError ? <Alert severity="warning">{runError.message}</Alert> : null}
              {submitError ? <Alert severity="error">{submitError}</Alert> : null}
              {isRecoveringQuest ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.runLoading}</Typography>
                </Stack>
              ) : null}
              {showCreateSection ? (
                <>
                  <Paper
                    variant="outlined"
                    sx={{
                      borderRadius: 999,
                      borderColor: '#d3a93a',
                      backgroundColor: '#f4e4bf',
                      p: 0.5,
                    }}
                  >
                    <Box
                      sx={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
                        gap: 0.5,
                      }}
                    >
                      <Button
                        onClick={() => setMultiEntryView('create')}
                        disableRipple
                        sx={{
                          minHeight: 44,
                          borderRadius: 999,
                          fontWeight: 700,
                          color: multiEntryView === 'create' ? '#ffffff' : '#6a5320',
                          backgroundColor: multiEntryView === 'create' ? '#7a4d19' : 'transparent',
                          boxShadow: multiEntryView === 'create' ? '0 4px 12px rgba(94, 60, 16, 0.24)' : 'none',
                          transition: 'none',
                        }}
                      >
                        {locale.showCreateRoom}
                      </Button>
                      <Button
                        onClick={() => {
                          setMode('Multi')
                          setMultiEntryView('list')
                        }}
                        disableRipple
                        sx={{
                          minHeight: 44,
                          borderRadius: 999,
                          fontWeight: 700,
                          color: multiEntryView === 'list' ? '#ffffff' : '#6a5320',
                          backgroundColor: multiEntryView === 'list' ? '#7a4d19' : 'transparent',
                          boxShadow: multiEntryView === 'list' ? '0 4px 12px rgba(94, 60, 16, 0.24)' : 'none',
                          transition: 'none',
                        }}
                      >
                        {locale.showRecruitingRooms}
                      </Button>
                    </Box>
                  </Paper>

                  {mode === 'Solo' || multiEntryView === 'create' ? (
                    <QuestRoomCreateSection
                      activeStages={activeStages}
                      selectedStageId={selectedStageId}
                      mode={mode}
                      isLoading={isPlayerLoading || isStagesLoading}
                      isSubmitting={isSubmitting}
                      isCreateDisabled={isCreateDisabled}
                      onStageChange={setSelectedStageId}
                      onModeChange={setMode}
                      onCreateRoom={handleCreateRoom}
                    />
                  ) : null}

                  {mode === 'Multi' && multiEntryView === 'list' ? (
                    <QuestMultiRoomList
                      rooms={latestRooms ?? []}
                      stages={activeStages}
                      accessToken={session?.access_token}
                      isLoading={isRoomsLoading}
                      error={roomsError instanceof Error ? roomsError : null}
                      isJoiningRoomId={isJoiningRoomId}
                      locale={locale}
                      onJoinRoom={handleJoinRoom}
                    />
                  ) : null}
                </>
              ) : null}

              {showWaitingSection ? (
                <QuestRoomLobbySection
                  currentRoom={currentRoom}
                  stageLabel={currentRoomStage?.name ?? null}
                  selfParticipantId={selfParticipantId}
                  positionDrafts={positionDrafts}
                  isLoading={isRoomLoading && liveRoom == null}
                  isStarting={isStarting}
                  isCancellingRoom={isCancellingRoom}
                  isUpdatingParticipantId={isUpdatingParticipantId}
                  onPositionDraftChange={(participantId, nextPosition) => {
                    setPositionDrafts((current) => ({
                      ...current,
                      [participantId]: nextPosition,
                    }))
                  }}
                  onUpdateParticipantPosition={handleUpdateParticipantPosition}
                  onStartQuest={handleStartQuest}
                  onCancelRoom={handleCancelRoom}
                />
              ) : null}

              {showRunSection ? (
                <QuestRunSection
                  currentRun={currentRun}
                  selfParticipantId={selfParticipantId}
                  availableMoves={availableMoves}
                  selectedActionKind={selectedActionKind}
                  selectedMoveId={selectedMoveId}
                  selectedTargetRow={selectedTargetRow}
                  selectedTargetColumn={selectedTargetColumn}
                  currentPendingCommand={currentPendingCommand}
                  chatMessage={chatMessage}
                  canSubmitCurrentTurn={canSubmitCurrentTurn}
                  isCommandSubmitting={isCommandSubmitting}
                  isChatSubmitting={isChatSubmitting}
                  onActionKindChange={setSelectedActionKind}
                  onMoveChange={setSelectedMoveId}
                  onTargetRowChange={setSelectedTargetRow}
                  onTargetColumnChange={setSelectedTargetColumn}
                  onChatMessageChange={setChatMessage}
                  onSubmitCommand={handleSubmitCommand}
                  onSubmitChatMessage={handleSubmitChatMessage}
                  onLeaveFinishedRun={handleLeaveFinishedRun}
                />
              ) : null}
            </Stack>
          </Box>

          <Button component={Link} to="/" variant="outlined" sx={menuButtonSx}>
            {locale.backToHome}
          </Button>
        </Stack>
      </Paper>
    </Container>
  )
}
