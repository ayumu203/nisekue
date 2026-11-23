import { Alert, Box, Chip, CircularProgress, Stack, Typography, useMediaQuery, useTheme } from '@mui/material'
import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import useSWR, { useSWRConfig, type KeyedMutator } from 'swr'
import { useQuestRunHub } from '@/hooks/useQuestRunHub'
import { createPlayer, getPlayer, listPlayers } from '@/api/player'
import { getPets } from '@/api/pet'
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
  updateQuestRoomRestrictions,
  updateQuestRoomPosition,
} from '@/api/quest'
import GamePanel from '@/components/common/GamePanel'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import { getTutorialStep, setTutorialStep } from '@/lib/tutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import QuestRoomCreateSection from '@/components/quest/QuestRoomCreateSection'
import QuestRoomLobbySection from '@/components/quest/QuestRoomLobbySection'
import QuestMultiRoomList from '@/components/quest/QuestMultiRoomList'
import QuestBattleStatusPanel from '@/components/quest/QuestBattleStatusPanel'
import QuestLastTurnResultsPanel from '@/components/quest/QuestLastTurnResultsPanel'
import QuestRunResultPanel from '@/components/quest/QuestRunResultPanel'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey, type HomeMenuItem } from '@/components/home/homeMenu'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { useAuth } from '@/contexts/useAuth'
import { questPanelColors } from '@/constants/styles'
import { useMobileScrollToRef } from '@/hooks/useMobileScrollToRef'
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
import type { PlayerSummary } from '@/schema/player'

const battleRowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const battleColumnOrder: BattleColumn[] = ['Left', 'Right']
const questSessionStorageKeyPrefix = 'nisekue:quest-session'
// ステージ一覧の並び順（一般ステージ → チャレンジ → エンドレス）。
const STAGE_GROUP_ORDER: Record<GetQuestStagesResponse[number]['progressionType'], number> = {
  Static: 0,
  Mirror: 1,
  Endless: 2,
}

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
  const { session, isLoading, isAnonymous } = useAuth()
  const tutorialUserId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (tutorialUserId ? getTutorialStep(tutorialUserId) : null))

  useEffect(() => {
    if (!tutorialUserId) {
      return
    }

    const step = getTutorialStep(tutorialUserId)
    if (step === 'home-quest') {
      setTutorialStep(tutorialUserId, 'quest-info')
      setTutorialStepState('quest-info')
    } else {
      setTutorialStepState(step)
    }
  }, [tutorialUserId])

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!tutorialUserId) {
      return
    }

    setTutorialStep(tutorialUserId, next)
    setTutorialStepState(next)
  }
  const { mutate: mutateCache } = useSWRConfig()
  const theme = useTheme()
  // 最初の描画から正しい側を出す（チュートリアルのスポットライトが最初に測った要素を使い続けるため）
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  // 2 列に並べられる幅。これより狭いときは箱を縦に積み、ページごとスクロールする
  const isWide = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  // ヘッダーメニューで開いているグループ（強化・交流・その他のうち 1 つだけ）
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)
  const [selectedStageId, setSelectedStageId] = useState<number | ''>('')
  const [mode, setMode] = useState<CreateQuestRoomRequest['mode']>('Solo')
  const [multiEntryView, setMultiEntryView] = useState<'create' | 'list'>('create')
  const [minRequiredLevelInput, setMinRequiredLevelInput] = useState('')
  const [allowedPlayerIds, setAllowedPlayerIds] = useState<string[]>([])
  const [isRestrictionsDirty, setIsRestrictionsDirty] = useState(false)
  const [createdRoom, setCreatedRoom] = useState<QuestRoomDetailResponse | null>(null)
  const [startedRun, setStartedRun] = useState<QuestRunDetailResponse | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isStarting, setIsStarting] = useState(false)
  const [isCancellingRoom, setIsCancellingRoom] = useState(false)
  const [isUpdatingRestrictions, setIsUpdatingRestrictions] = useState(false)
  const [selectedActionKind, setSelectedActionKind] = useState<QuestActionKind>('NormalAttack')
  const [selectedMoveId, setSelectedMoveId] = useState<number | ''>('')
  const [selectedTargetRow, setSelectedTargetRow] = useState<BattleRow | ''>('')
  const [selectedTargetColumn, setSelectedTargetColumn] = useState<BattleColumn | ''>('')
  const [isCommandSubmitting, setIsCommandSubmitting] = useState(false)
  const [positionDrafts, setPositionDrafts] = useState<Record<string, { row: BattleRow; column: BattleColumn }>>({})
  const [isSavingFormation, setIsSavingFormation] = useState(false)
  const [isJoiningRoomId, setIsJoiningRoomId] = useState<string | null>(null)
  const [chatMessage, setChatMessage] = useState('')
  const [isChatSubmitting, setIsChatSubmitting] = useState(false)
  const [isRecoveringQuest, setIsRecoveringQuest] = useState(false)
  const [hasTriedQuestRecovery, setHasTriedQuestRecovery] = useState(false)
  const hasAttemptedQuestRecoveryRef = useRef(false)
  const mutateRunRef = useRef<KeyedMutator<QuestRunDetailResponse> | null>(null)
  const questMainRef = useRef<HTMLDivElement | null>(null)
  const errorAlertRef = useRef<HTMLDivElement | null>(null)
  useMobileScrollToRef(questMainRef, { enabled: !isLoading })
  useMobileScrollToRef(errorAlertRef, {
    enabled: submitError != null,
    offsetTop: 12,
    trigger: submitError,
  })

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

  const refreshPlayerStatus = useCallback(async (): Promise<void> => {
    if (!session?.user.id) {
      return
    }

    await Promise.all([
      mutatePlayer(),
      mutateCache([`player`, session.user.id]),
      mutateCache([`quest-player`, session.user.id]),
      mutateCache([`training-player`, session.user.id]),
    ])
  }, [mutateCache, mutatePlayer, session?.user.id])

  const petsSWRKey =
    session?.user.id && (startedRun != null || createdRoom?.closeReason === 'Started')
      ? ([`quest-pets`, session.user.id] as const)
      : null
  const { data: petsResponse, mutate: mutatePets } = useSWR(petsSWRKey, async () => {
    if (!session?.access_token) {
      return null
    }

    return getPets(session.access_token)
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
        .sort((left, right) => {
          // 一般ステージ → チャレンジ（名鑑の祠）→ エンドレスの順に、推奨レベルに関わらず並べる。
          const leftGroup = STAGE_GROUP_ORDER[left.progressionType]
          const rightGroup = STAGE_GROUP_ORDER[right.progressionType]
          if (leftGroup !== rightGroup) {
            return leftGroup - rightGroup
          }

          return left.recommendedLevel - right.recommendedLevel
        }),
    [stages],
  )
  const playerCandidatesSWRKey =
    session?.access_token && startedRun == null ? ([`quest-player-candidates`] as const) : null
  const {
    data: playerCandidates,
    error: playerCandidatesError,
    isLoading: isPlayerCandidatesLoading,
  } = useSWR(playerCandidatesSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return listPlayers(session.access_token)
  })

  const selectablePlayers = useMemo<PlayerSummary[]>(
    () => (playerCandidates?.items ?? []).filter((candidate) => candidate.userId !== player?.userId),
    [player?.userId, playerCandidates],
  )
  const isCreateDisabled =
    isSubmitting ||
    isPlayerLoading ||
    isStagesLoading ||
    isPlayerCandidatesLoading ||
    playerError != null ||
    playerCandidatesError != null ||
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
          minRequiredLevel: minRequiredLevelInput.trim() === '' ? null : Number(minRequiredLevelInput),
          allowedPlayerIds,
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

  function handleRestrictionsChange(nextMinRequiredLevelInput: string, nextAllowedPlayerIds: string[]): void {
    setIsRestrictionsDirty(true)
    setMinRequiredLevelInput(nextMinRequiredLevelInput)
    setAllowedPlayerIds(nextAllowedPlayerIds)
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
  const currentRoomRestrictionSignature = currentRoom
    ? JSON.stringify({
        roomId: currentRoom.roomId,
        minRequiredLevel: currentRoom.restrictions.minRequiredLevel,
        allowedPlayerIds: currentRoom.restrictions.allowedPlayers.map((player) => player.playerId).sort(),
      })
    : null

  useEffect(() => {
    if (!currentRoom || isRestrictionsDirty) {
      return
    }

    setMinRequiredLevelInput(currentRoom.restrictions.minRequiredLevel?.toString() ?? '')
    setAllowedPlayerIds(currentRoom.restrictions.allowedPlayers.map((player) => player.playerId))
  }, [currentRoom, currentRoomRestrictionSignature, isRestrictionsDirty])

  useEffect(() => {
    setIsRestrictionsDirty(false)
  }, [currentRoom?.roomId])

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

  async function handleUpdateRoomRestrictions(options?: {
    minRequiredLevelInput?: string
    allowedPlayerIds?: string[]
  }): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    if (!currentRoom) {
      setSubmitError(locale.createRoomFirst)
      return
    }

    setIsUpdatingRestrictions(true)
    setSubmitError(null)

    try {
      const nextMinRequiredLevelInput = options?.minRequiredLevelInput ?? minRequiredLevelInput
      const nextAllowedPlayerIds = options?.allowedPlayerIds ?? allowedPlayerIds

      const room = await updateQuestRoomRestrictions(
        currentRoom.roomId,
        {
          minRequiredLevel: nextMinRequiredLevelInput.trim() === '' ? null : Number(nextMinRequiredLevelInput),
          allowedPlayerIds: nextAllowedPlayerIds,
        },
        session.access_token,
      )
      setIsRestrictionsDirty(false)
      setMinRequiredLevelInput(nextMinRequiredLevelInput)
      setAllowedPlayerIds(nextAllowedPlayerIds)
      setCreatedRoom(room)
      await mutateRoom(room, { revalidate: false })
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.updateRestrictionsFailed)
    } finally {
      setIsUpdatingRestrictions(false)
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
      setCreatedRoom(null)
      setMultiEntryView('create')
      await mutateRoom(room, { revalidate: false })
      await mutateRooms()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.cancelRoomFailed)
    } finally {
      setIsCancellingRoom(false)
    }
  }

  const hubRunId = startedRun?.status === 'InProgress' ? startedRun.runId : null
  const { isConnected: isHubConnected } = useQuestRunHub({
    runId: hubRunId,
    accessToken: session?.access_token ?? null,
    onSnapshot: (event) => {
      mutateRunRef.current?.(event, { revalidate: false })
    },
    onUpdated: (event) => {
      // 解決ホットパスは軽量ロード由来で lastTurnResults が null になり得る（コマンド送信のみで未解決の場合など）。
      // 直前ターンの結果表示が消えないよう、null のときは現在値を保持する。
      mutateRunRef.current?.(
        (current) =>
          event.lastTurnResults == null && current?.lastTurnResults != null
            ? { ...event, lastTurnResults: current.lastTurnResults }
            : event,
        { revalidate: false },
      )
    },
  })

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
      refreshInterval: isHubConnected
        ? 0
        : currentRoom?.closeReason === 'Started' || startedRun?.status === 'InProgress'
          ? 2000
          : 0,
    },
  )

  mutateRunRef.current = mutateRun

  const currentRun = liveRun ?? startedRun

  const currentRoomStage = currentRoom
    ? (activeStages.find((stage) => stage.stageId === currentRoom.stageId) ?? null)
    : null
  const currentRunStage = currentRun
    ? (activeStages.find((stage) => stage.stageId === currentRun.stageId) ?? null)
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
          targetLifeState: slot.targetLifeState,
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
    selectedActionKind === 'NormalAttack' ||
    selectedActionKind === 'Capture' ||
    (selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Enemy')
  const isAllyTargetingAction = selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Ally'
  const isSelfTargetingAction = selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Self'
  const selfPartyMember = useMemo(
    () =>
      currentRun && selfParticipantId
        ? (currentRun.partyMembers.find((member) => member.participantId === selfParticipantId) ?? null)
        : null,
    [currentRun, selfParticipantId],
  )
  // 名鑑の祠（プレイヤーが敵）では捕獲できない。
  const canCapture =
    currentRun?.floor.isMirror !== true &&
    petsResponse != null &&
    petsResponse.pets != null &&
    petsResponse.pets.length < petsResponse.maxPetCount
  // ペット呼出はソロ限定・1クエスト1回。プレイヤーが1人だけのランをソロとみなす（マルチは2人以上で開始する）。
  const isSoloRun =
    currentRun != null && currentRun.partyMembers.filter((member) => member.type === 'Player').length === 1
  const isPetDeploymentActive = currentRun?.petSummon.deployment != null
  const canSummonPet =
    isSoloRun &&
    currentRun != null &&
    !currentRun.petSummon.used &&
    !isPetDeploymentActive &&
    (selfPartyMember?.pets.length ?? 0) > 0

  // エンドレスでは Escape を「終了（成功）」としてチェックポイント階限定で許可している。
  // フロア遷移（SignalR 更新など）で非チェックポイント階に移った後も Escape が選択されたまま残ると、
  // 送信ボタンから無効な Escape を送って 409 になるため、ここで NormalAttack に差し戻す。
  const isEndlessNonCheckpointFloor =
    currentRun?.floor.isEndless === true &&
    !(
      currentRun.floor.bossInterval != null &&
      currentRun.floor.bossInterval > 0 &&
      currentRun.floor.currentFloorNo % currentRun.floor.bossInterval === 0
    )
  useEffect(() => {
    if (
      (selectedActionKind === 'Capture' && !canCapture) ||
      (selectedActionKind === 'SummonPet' && !canSummonPet) ||
      (selectedActionKind === 'EntrustPet' && !isPetDeploymentActive) ||
      (selectedActionKind === 'Escape' && isEndlessNonCheckpointFloor)
    ) {
      setSelectedActionKind('NormalAttack')
      return
    }

    // ペット出撃中に選べるのは「ペットにまかせる」だけ。
    if (isPetDeploymentActive && selectedActionKind !== 'EntrustPet') {
      setSelectedActionKind('EntrustPet')
    }
  }, [selectedActionKind, canCapture, canSummonPet, isPetDeploymentActive, isEndlessNonCheckpointFloor])

  const lastTurnResultsTurnNo = currentRun?.lastTurnResults?.turnNo ?? null
  const capturedInLastTurn =
    currentRun?.lastTurnResults?.actions.some((action) => action.actionKind === 'Capture' && action.succeeded) ?? false
  useEffect(() => {
    // ターン解決後に所持ペット数（捕獲結果）を取り直す
    if (lastTurnResultsTurnNo != null && capturedInLastTurn) {
      void mutatePets()
    }
  }, [capturedInLastTurn, lastTurnResultsTurnNo, mutatePets])

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
    if (currentRun?.status !== 'Succeeded' && currentRun?.status !== 'Failed') {
      return
    }

    void refreshPlayerStatus()
  }, [currentRun?.status, refreshPlayerStatus])

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
    if (isSelfTargetingAction) {
      if (selfPartyMember) {
        setSelectedTargetRow(selfPartyMember.position.row)
        setSelectedTargetColumn(selfPartyMember.position.column)
      }
      return
    }

    if (isEnemyTargetingAction || isAllyTargetingAction) {
      return
    }

    setSelectedTargetRow('')
    setSelectedTargetColumn('')
  }, [
    isEnemyTargetingAction,
    isAllyTargetingAction,
    isSelfTargetingAction,
    selfPartyMember,
    selfPartyMember?.position.row,
    selfPartyMember?.position.column,
  ])

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

  async function handleSaveFormation(): Promise<void> {
    if (!session?.access_token || !currentRoom) {
      setSubmitError(locale.sessionInfoMissing)
      return
    }

    const participantsToUpdate = currentRoom.participants.filter((participant) => {
      const draft = positionDrafts[participant.participantId]
      return draft != null && (draft.row !== participant.position.row || draft.column !== participant.position.column)
    })
    if (participantsToUpdate.length === 0) {
      return
    }

    setIsSavingFormation(true)
    setSubmitError(null)
    let successfulUpdates = 0

    try {
      let latestRoom = currentRoom
      for (const participant of participantsToUpdate) {
        const draft = positionDrafts[participant.participantId]
        if (!draft) {
          continue
        }

        latestRoom = await updateQuestRoomPosition(
          latestRoom.roomId,
          {
            participantId: participant.participantId,
            row: draft.row,
            column: draft.column,
          },
          session.access_token,
        )
        successfulUpdates += 1
      }
      if (successfulUpdates > 0) {
        setCreatedRoom(latestRoom)
        await mutateRoom(latestRoom, { revalidate: false })
      }
    } catch (error) {
      const baseMessage = error instanceof Error ? error.message : locale.updatePositionFailed
      if (successfulUpdates > 0) {
        await mutateRoom()
        setSubmitError(`${baseMessage} ${locale.updatePositionPartiallyFailedSuffix}`)
      } else {
        setSubmitError(baseMessage)
      }
    } finally {
      setIsSavingFormation(false)
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
    if (isAnonymous) {
      setSubmitError(locale.anonymousPostingRestricted)
      return
    }

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

    if (normalizedMessage.length > 50) {
      setSubmitError(locale.chatSubmitFailed)
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

  async function handleLeaveFinishedRun(): Promise<void> {
    if (session?.user.id) {
      clearPersistedQuestSession(session.user.id)
    }

    await refreshPlayerStatus()
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

  const tutorialHomeButtonId = tutorialStep === 'quest-to-home' ? 'tutorial-home-btn-quest' : undefined
  const handleHomeClick = () => {
    if (tutorialStep === 'quest-to-home') {
      advanceTutorial('home-move-setting')
    }
  }

  // PC のヘッダーメニュー。ホームと同じ項目の先頭に「ホーム」を足し、今いるクエストを帯の色で示す
  const homeMenu = buildHomeMenu({
    userId: tutorialUserId,
    tutorialStep: null,
    showRebirthTutorial: false,
    showSurveyGuide: false,
    onSurveyClick: () => {},
    canUseDebug,
  })
  const homeMenuItem: HomeMenuItem = {
    key: 'home',
    label: locale.backToHome,
    icon: <HomeIcon />,
    to: '/',
    id: tutorialHomeButtonId,
    onClick: handleHomeClick,
  }

  const panelProps = { shadowColor: questPanelColors.shadow, fillHeight: isWide }
  // ルーム作成・募集中・出発待ちの箱は中身の高さまで縮める（バトル画面とメッセージは画面の高さいっぱいに使う）
  const fitPanelProps = { ...panelProps, fitContent: true }
  const stageName = currentRunStage?.name ?? currentRoomStage?.name ?? null
  const isRunFinished = currentRun?.status === 'Succeeded' || currentRun?.status === 'Failed'
  const isRunInProgress = currentRun?.status === 'InProgress'

  // 画面の中身。PC は横に 2 列、スマホは縦に積む
  function renderColumns(main: ReactNode, side: ReactNode, sideWidth: number) {
    return (
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'grid',
          gap: { xs: 2, sm: 3 },
          gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: side ? `minmax(0, 1fr) ${sideWidth}px` : 'minmax(0, 1fr)' },
        }}
      >
        <Box sx={{ minHeight: 0, minWidth: 0 }}>{main}</Box>
        {side ? <Box sx={{ minHeight: 0, minWidth: 0 }}>{side}</Box> : null}
      </Box>
    )
  }

  const createTabs = [
    {
      key: 'create',
      label: locale.showCreateRoom,
      selected: multiEntryView === 'create',
      onClick: () => setMultiEntryView('create'),
    },
    {
      key: 'list',
      label: locale.showRecruitingRooms,
      selected: multiEntryView === 'list',
      onClick: () => {
        setMode('Multi')
        setMultiEntryView('list')
      },
    },
  ]

  const createContent = showCreateSection ? (
    mode === 'Multi' && multiEntryView === 'list' ? (
      renderColumns(
        <GamePanel tabs={createTabs} {...fitPanelProps}>
          <QuestMultiRoomList
            rooms={latestRooms ?? []}
            stages={activeStages}
            isLoading={isRoomsLoading}
            error={roomsError instanceof Error ? roomsError : null}
            isJoiningRoomId={isJoiningRoomId}
            locale={locale}
            onJoinRoom={handleJoinRoom}
          />
        </GamePanel>,
        // 「作成」と切り替えても左の箱の幅が変わらないよう、右の列は空けておく
        <Box />,
        440,
      )
    ) : (
      <QuestRoomCreateSection
        activeStages={activeStages}
        selectedStageId={selectedStageId}
        mode={mode}
        currentPlayerLevel={player?.level ?? null}
        currentPlayerImagePath={player?.imagePath ?? null}
        minRequiredLevelInput={minRequiredLevelInput}
        selectablePlayers={selectablePlayers}
        allowedPlayerIds={allowedPlayerIds}
        isLoading={isPlayerLoading || isStagesLoading}
        isPlayerCandidatesLoading={isPlayerCandidatesLoading}
        playerCandidatesError={playerCandidatesError}
        isSubmitting={isSubmitting}
        isCreateDisabled={isCreateDisabled}
        onStageChange={setSelectedStageId}
        onModeChange={setMode}
        onRestrictionsChange={handleRestrictionsChange}
        onCreateRoom={handleCreateRoom}
        stagePageSize={isWide ? 6 : 3}
        stageColumns={isWide ? 2 : 1}
        renderLayout={({ stages, form }) =>
          renderColumns(
            <GamePanel tabs={createTabs} {...fitPanelProps}>
              {stages}
            </GamePanel>,
            <GamePanel title={locale.openRestrictionsModal} {...fitPanelProps}>
              {form}
            </GamePanel>,
            440,
          )
        }
      />
    )
  ) : null

  const waitingContent = showWaitingSection ? (
    <QuestRoomLobbySection
      currentRoom={currentRoom}
      selfParticipantId={selfParticipantId}
      currentPlayerLevel={player?.level ?? null}
      selectablePlayers={selectablePlayers}
      minRequiredLevelInput={minRequiredLevelInput}
      allowedPlayerIds={allowedPlayerIds}
      positionDrafts={positionDrafts}
      isLoading={isRoomLoading && liveRoom == null}
      isStarting={isStarting}
      isCancellingRoom={isCancellingRoom}
      isUpdatingRestrictions={isUpdatingRestrictions}
      isPlayerCandidatesLoading={isPlayerCandidatesLoading}
      playerCandidatesError={playerCandidatesError}
      isSavingFormation={isSavingFormation}
      onRestrictionsChange={handleRestrictionsChange}
      onUpdateRestrictions={handleUpdateRoomRestrictions}
      onPositionDraftChange={(participantId, nextPosition) => {
        setPositionDrafts((current) => ({
          ...current,
          [participantId]: nextPosition,
        }))
      }}
      onSaveFormation={handleSaveFormation}
      onStartQuest={handleStartQuest}
      onCancelRoom={handleCancelRoom}
      renderLayout={({ info, editor, editorTitle }) =>
        renderColumns(
          <GamePanel title={stageName ?? locale.stage} {...fitPanelProps}>
            {info}
          </GamePanel>,
          editor ? (
            <GamePanel title={editorTitle} {...fitPanelProps}>
              {editor}
            </GamePanel>
          ) : null,
          620,
        )
      }
    />
  ) : null

  const runContent =
    showRunSection && currentRun ? (
      isRunFinished ? (
        <Box sx={{ width: '100%', maxWidth: 960, mx: 'auto' }}>
          <QuestRunResultPanel
            run={currentRun}
            endlessBestFloor={player?.endlessBestFloor}
            onLeaveFinishedRun={handleLeaveFinishedRun}
            locale={locale}
          />
        </Box>
      ) : isRunInProgress ? (
        renderColumns(
          <GamePanel
            title={stageName ?? locale.stage}
            action={
              currentRun.floor.isEndless ? null : (
                <Chip
                  size="small"
                  label={`${locale.waitingParticipantsLabel}: ${currentRun.turn.waitingParticipantIds.length}`}
                  sx={{ mr: 1.5, color: '#ffffff', fontWeight: 700, backgroundColor: 'rgba(255, 255, 255, 0.22)' }}
                />
              )
            }
            disablePadding
            {...panelProps}
          >
            <QuestBattleStatusPanel
              run={currentRun}
              battlefieldImagePath={
                currentRunStage?.battlefieldImagePath ?? currentRoomStage?.battlefieldImagePath ?? null
              }
              selfParticipantId={selfParticipantId}
              availableMoves={availableMoves}
              selectedActionKind={selectedActionKind}
              selectedMoveId={selectedMoveId}
              selectedTargetRow={selectedTargetRow}
              selectedTargetColumn={selectedTargetColumn}
              canCapture={canCapture}
              canSummonPet={canSummonPet}
              canSubmitCurrentTurn={canSubmitCurrentTurn}
              isCommandSubmitting={isCommandSubmitting}
              onActionKindChange={setSelectedActionKind}
              onMoveChange={setSelectedMoveId}
              onTargetRowChange={setSelectedTargetRow}
              onTargetColumnChange={setSelectedTargetColumn}
              onSubmitCommand={handleSubmitCommand}
              locale={locale}
            />
          </GamePanel>,
          <GamePanel title={locale.messagePanelTitle} {...panelProps}>
            <QuestLastTurnResultsPanel
              run={currentRun}
              chatMessage={chatMessage}
              isChatSubmitting={isChatSubmitting}
              canPostChat={selfParticipantId != null}
              chatDisabledReason={isAnonymous ? locale.anonymousPostingRestricted : null}
              onChatMessageChange={setChatMessage}
              onSubmitChatMessage={handleSubmitChatMessage}
              locale={locale}
            />
          </GamePanel>,
          480,
        )
      ) : null
    ) : null

  const alerts = (
    <>
      {playerError ? <Alert severity="warning">{playerError.message}</Alert> : null}
      {stagesError ? <Alert severity="warning">{stagesError.message}</Alert> : null}
      {roomsError ? <Alert severity="warning">{roomsError.message}</Alert> : null}
      {roomError ? <Alert severity="warning">{roomError.message}</Alert> : null}
      {runError ? <Alert severity="warning">{runError.message}</Alert> : null}
      {submitError ? (
        <Box ref={errorAlertRef}>
          <Alert severity="error">{submitError}</Alert>
        </Box>
      ) : null}
      {isRecoveringQuest ? (
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={16} sx={{ color: '#ffffff' }} />
          <Typography variant="body2" sx={{ color: '#ffffff' }}>
            {locale.runLoading}
          </Typography>
        </Stack>
      ) : null}
    </>
  )

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        background: `linear-gradient(${questPanelColors.topBand}, ${questPanelColors.topBand}) top / 100% 300px no-repeat, ${questPanelColors.ground}`,
      }}
    >
      {isDesktop ? (
        <HomeHeaderMenu
          primary={[homeMenuItem, ...homeMenu.primary]}
          groups={homeMenu.groups}
          openGroup={openMenuGroup}
          lockedGroup={null}
          onToggleGroup={(key) => setOpenMenuGroup((current) => (current === key ? null : key))}
          onCloseGroup={() => setOpenMenuGroup(null)}
          currentKey="quest"
          currentColor={questPanelColors.topBand}
        />
      ) : null}
      <Box
        id="quest-main"
        ref={questMainRef}
        sx={{
          flex: 1,
          // PC は箱の中でスクロールさせるため、画面の高さに合わせる（低すぎる画面ではページごとスクロール）
          height: { md: 'calc(100dvh - 57px)' },
          minHeight: { md: 640 },
          boxSizing: 'border-box',
          px: { xs: 1.5, sm: 4 },
          py: { xs: 1.5, sm: 3 },
          display: 'flex',
          flexDirection: 'column',
          gap: { xs: 1.5, sm: 2 },
        }}
      >
        {isDesktop ? null : (
          <Box>
            <HomeNavIconButton id={tutorialHomeButtonId} ariaLabel={locale.backToHome} onClick={handleHomeClick} />
          </Box>
        )}
        {alerts}
        {createContent}
        {waitingContent}
        {runContent}
      </Box>
      {tutorialStep === 'quest-info' && (
        <SpotlightTutorial
          guide="captain"
          message={tutorialLocale.steps.questInfo.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.questInfo.dismissLabel}
          onDismiss={() => advanceTutorial('quest-to-home')}
        />
      )}
      {tutorialStep === 'quest-to-home' && (
        <SpotlightTutorial
          guide="captain"
          targetId="tutorial-home-btn-quest"
          message={tutorialLocale.steps.questToHome.message}
        />
      )}
    </Box>
  )
}
