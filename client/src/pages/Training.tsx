import { Alert, Box, CircularProgress, IconButton, Stack, Typography, useMediaQuery, useTheme } from '@mui/material'
import { Link } from 'react-router-dom'
import { useEffect, useRef, useState, type ReactNode } from 'react'
import useSWR, { useSWRConfig } from 'swr'
import { PLAYER_DEDUPING_INTERVAL } from '@/constants/swr'
import {
  executeTraining,
  executeTrainingPvp,
  getTrainingEnemies,
  getPvpOpponents,
  TrainingCooldownError,
} from '@/api/training'
import { createPlayer, getPlayer } from '@/api/player'
import GamePanel from '@/components/common/GamePanel'
import TrainingBattleField from '@/components/training/TrainingBattleField'
import TrainingEnemySelect from '@/components/training/TrainingEnemySelect'
import TrainingMovePlanForm from '@/components/training/TrainingMovePlanForm'
import TrainingOpponentSelect from '@/components/training/TrainingOpponentSelect'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeFilledIcon, HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey, type HomeMenuItem } from '@/components/home/homeMenu'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { normalizeCharacterPath } from '@/lib/assets'
import { useAuth } from '@/contexts/useAuth'
import { useNotify } from '@/contexts/useNotify'
import { trainingPanelColors } from '@/constants/styles'
import { useMobileScrollToRef } from '@/hooks/useMobileScrollToRef'
import { getTutorialStep, setTutorialStep, type TutorialStep } from '@/lib/tutorial'
import { getNextMoveTurnIndex } from '@/lib/trainingMovePlan'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/training/Training.json'
import type { ExecuteTrainingResponse, TrainingEnemy } from '@/schema/training'
import type { GetPlayerResponse, PlayerSummary } from '@/schema/player'
import { palette } from '@/theme/tokens'

type TrainingMode = 'npc' | 'player'

const DEFAULT_CHARACTER_PATH = 'image/character/ch001_bmnpc.png'

function playerSummaryToDisplayEnemy(opponent: PlayerSummary): TrainingEnemy {
  return {
    id: -1,
    name: opponent.userName ?? locale.anonymousPlayer,
    imagePath: opponent.imagePath ? normalizeCharacterPath(opponent.imagePath) : DEFAULT_CHARACTER_PATH,
    level: opponent.level,
  }
}

const TRAINING_COOLDOWN_MS = 3000
// PC は NPC と同じ小さいカードを 2 段まで出す
const PVP_OPPONENTS_PAGE_SIZE = 16
const PVP_OPPONENTS_PAGE_SIZE_MOBILE = 5

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

function resolveMoveNames(player: GetPlayerResponse, moveIds: Array<number | null> | null): string[] {
  const moveNameById = new Map(
    player.moveSlots.flatMap((slot) => (slot.moveId === null ? [] : [[slot.moveId, slot.moveName] as const])),
  )

  return (moveIds ?? []).map((moveId) =>
    moveId === null ? locale.normalAttack : (moveNameById.get(moveId) ?? locale.normalAttack),
  )
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
  // 最初の描画から正しい側を出す（チュートリアルのスポットライトが最初に測った要素を使い続けるため）
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  // 2 列に並べられる幅。これより狭いときは箱を縦に積み、ページごとスクロールする
  const isWide = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  // ヘッダーメニューで開いているグループ（強化・交流・その他のうち 1 つだけ）
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)
  const trainingMainRef = useRef<HTMLDivElement | null>(null)
  const trainingPanelRef = useRef<HTMLDivElement | null>(null)
  const battleResultRef = useRef<HTMLDivElement | null>(null)
  const trainingMovePlanRef = useRef<HTMLDivElement | null>(null)
  const [mode, setMode] = useState<TrainingMode>('npc')
  const [selectedEnemy, setSelectedEnemy] = useState<TrainingEnemy | null>(null)
  const [selectedOpponent, setSelectedOpponent] = useState<PlayerSummary | null>(null)
  const [trainingResult, setTrainingResult] = useState<ExecuteTrainingResponse | null>(null)
  const notify = useNotify()
  const [isTrainingSubmitting, setIsTrainingSubmitting] = useState(false)
  const [trainingLockUntilMs, setTrainingLockUntilMs] = useState(0)
  const [trainingLockRemainingMs, setTrainingLockRemainingMs] = useState(0)
  const trainingLockRemainingSeconds = Math.ceil(trainingLockRemainingMs / 1000)
  const [plannedMoveIds, setPlannedMoveIds] = useState<Array<number | null> | null>(null)
  const [lastSubmittedMoveIds, setLastSubmittedMoveIds] = useState<Array<number | null> | null>(null)
  const prevLevelRef = useRef<number | undefined>(undefined)
  const knownEnemyIdsRef = useRef<Set<number>>(new Set())
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const [opponentsPage, setOpponentsPage] = useState(1)
  // 「OK」で閉じた案内。戦闘ボタンなどに重ならないように、この画面にいる間は出さない
  const [closedTutorialStep, setClosedTutorialStep] = useState<TutorialStep | null>(null)

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }

  useEffect(() => {
    if (!userId) {
      return
    }

    const step = getTutorialStep(userId)
    setTutorialStepState(step)
  }, [userId])

  useEffect(() => {
    if (trainingLockUntilMs <= Date.now()) {
      setTrainingLockRemainingMs(0)
      return
    }

    const update = () => {
      setTrainingLockRemainingMs(Math.max(0, trainingLockUntilMs - Date.now()))
    }

    update()
    const timerId = window.setInterval(update, 250)
    return () => window.clearInterval(timerId)
  }, [trainingLockUntilMs])

  useEffect(() => {
    if (!isMobile || (!selectedEnemy && !selectedOpponent) || trainingResult || !plannedMoveIds) {
      return
    }

    trainingPanelRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [isMobile, plannedMoveIds, selectedEnemy, selectedOpponent, trainingResult])

  useEffect(() => {
    if (!isMobile || !trainingResult) {
      return
    }

    battleResultRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }, [isMobile, trainingResult])
  useMobileScrollToRef(trainingMainRef, { enabled: isMobile })

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
  } = useSWR(
    trainingEnemiesSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getTrainingEnemies(session.access_token)
    },
    { dedupingInterval: PLAYER_DEDUPING_INTERVAL },
  )

  const opponentsPageSize = isMobile ? PVP_OPPONENTS_PAGE_SIZE_MOBILE : PVP_OPPONENTS_PAGE_SIZE
  const opponentsSWRKey =
    session?.access_token && player && mode === 'player'
      ? ([`training-opponents`, session.user.id, opponentsPage, opponentsPageSize] as const)
      : null
  const {
    data: playerListPage,
    error: playerListError,
    isLoading: isPlayerListLoading,
  } = useSWR(
    opponentsSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getPvpOpponents(session.access_token, {
        page: opponentsPage,
        pageSize: opponentsPageSize,
      })
    },
    { dedupingInterval: PLAYER_DEDUPING_INTERVAL },
  )
  const opponentsPageCount = playerListPage
    ? Math.max(1, Math.ceil(playerListPage.totalCount / playerListPage.pageSize))
    : 1

  useEffect(() => {
    if (mode !== 'player') {
      return
    }

    setOpponentsPage(1)
  }, [mode, opponentsPageSize])

  useEffect(() => {
    if (!playerListPage) {
      return
    }

    const pageCount = Math.max(1, Math.ceil(playerListPage.totalCount / playerListPage.pageSize))
    setOpponentsPage((current) => Math.min(current, pageCount))
  }, [playerListPage])

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

  useEffect(() => {
    if (!player || !tutorialStep || !userId) {
      return
    }

    if (tutorialStep === 'training-to-lv5' && player.level >= 5) {
      setTutorialStep(userId, 'home-job-change')
      setTutorialStepState('home-job-change')
    } else if (tutorialStep === 'training-to-lv7' && player.level >= 7) {
      setTutorialStep(userId, 'training-quest-guide')
      setTutorialStepState('training-quest-guide')
    }
  }, [player, tutorialStep, userId])

  useEffect(() => {
    if (!player || !userId || !trainingEnemies) {
      return
    }

    const prevLevel = prevLevelRef.current
    prevLevelRef.current = player.level

    if (prevLevel !== undefined && player.level > prevLevel) {
      const currentMaxEnemyLevel = Math.max(...trainingEnemies.map((e) => e.level))
      if (player.level >= currentMaxEnemyLevel) {
        void mutateCache([`training-enemies`, userId])
      }
    }
  }, [player, mutateCache, userId, trainingEnemies])

  useEffect(() => {
    if (!trainingEnemies || trainingEnemies.length === 0) {
      return
    }

    const knownIds = knownEnemyIdsRef.current
    const hasNewEnemies = trainingEnemies.some((e) => !knownIds.has(e.id))

    knownEnemyIdsRef.current = new Set(trainingEnemies.map((e) => e.id))

    if (knownIds.size > 0 && hasNewEnemies) {
      notify.info(locale.newEnemiesUnlocked)
    }
  }, [notify, trainingEnemies])

  const isBattleLocked = isTrainingSubmitting || trainingLockRemainingSeconds > 0
  const isTrainingActionDisabled = isBattleLocked
  const isOpponentMode = mode === 'player' && selectedOpponent !== null
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

    if (isBattleLocked) {
      return
    }

    setIsTrainingSubmitting(true)
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

      if (tutorialStep === 'training-confirm') {
        advanceTutorial('training-to-lv5')
      }

      await refreshPlayerStatus().catch((error) => {
        console.error('Failed to refresh player status after training.', error)
      })
    } catch (error) {
      if (error instanceof TrainingCooldownError) {
        const retryAfterMessage = locale.retryAfterSeconds.replace('{{seconds}}', String(error.retryAfterSeconds))
        notify.error(`${error.message} (${retryAfterMessage})`)
      } else if (error instanceof Error) {
        notify.error(error.message)
      } else {
        notify.error(locale.trainingFailed)
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

    if (isBattleLocked) {
      return
    }

    setIsTrainingSubmitting(true)
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
        notify.error(`${error.message} (${retryAfterMessage})`)
      } else if (error instanceof Error) {
        notify.error(error.message)
      } else {
        notify.error(locale.trainingFailed)
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

    if (tutorialStep === 'training-fight') {
      const hasMovesSelected = lastSubmittedMoveIds?.some((id) => id !== null) ?? false
      advanceTutorial(hasMovesSelected ? 'training-confirm' : 'training-move-select')
    }

    if (!player) {
      return
    }

    setPlannedMoveIds(normalizeTrainingMoveIds(player, lastSubmittedMoveIds))
  }

  function handleSelectOpponent(opponent: PlayerSummary): void {
    setSelectedOpponent(opponent)
    setSelectedEnemy(null)
    setTrainingResult(null)

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

  // 技選びのチュートリアルでは、技を選ぶたびに矢印を次のターンへ移し、MP を使い切ったら「戦闘」の案内へ進む
  const nextTutorialMoveTurnIndex =
    tutorialStep === 'training-move-select' && player && plannedMoveIds
      ? getNextMoveTurnIndex(player, plannedMoveIds)
      : null
  const isTutorialMovePlanDone =
    tutorialStep === 'training-move-select' &&
    player != null &&
    plannedMoveIds != null &&
    nextTutorialMoveTurnIndex === null

  useEffect(() => {
    if (isTutorialMovePlanDone) {
      advanceTutorial('training-confirm')
    }
    // advanceTutorial は毎回作り直されるので依存に入れない
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isTutorialMovePlanDone])

  async function startBattle(): Promise<void> {
    if (!player || !plannedMoveIds) {
      throw new Error(locale.playerLoading)
    }

    if (isOpponentMode && selectedOpponent) {
      await runPvpTraining(selectedOpponent, plannedMoveIds)
    } else if (selectedEnemy) {
      await runTraining(selectedEnemy, plannedMoveIds)
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

  const tutorialHomeButtonId = tutorialStep === 'home-job-change' ? 'tutorial-home-btn' : undefined

  // PC のヘッダーメニュー。ホームと同じ項目の先頭に「ホーム」を足し、今いる訓練場を帯の色で示す
  const homeMenu = buildHomeMenu({
    userId,
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
  }

  const panelProps = {
    fillHeight: isWide,
    bandColor: trainingPanelColors.band,
    bandSelectedColor: trainingPanelColors.bandSelected,
  }
  const displayEnemy = selectedEnemy ?? (selectedOpponent ? playerSummaryToDisplayEnemy(selectedOpponent) : null)
  const displayEnemyBackgroundKey = selectedOpponent?.userId ?? selectedEnemy?.id ?? 0

  const modeTabs = (['npc', 'player'] as const).map((key) => ({
    key,
    label: key === 'npc' ? locale.modeNpc : locale.modePlayer,
    selected: mode === key,
    onClick: () => {
      if (mode === key) return
      setMode(key)
      setSelectedEnemy(null)
      setSelectedOpponent(null)
      setTrainingResult(null)
    },
  }))

  // 相手の一覧（上の段に戦闘画面があるので、小さいカードで詰めて並べる）
  function renderOpponentList(): ReactNode {
    if (mode === 'npc') {
      if (isTrainingEnemiesLoading) {
        return (
          <Stack direction="row" spacing={1} alignItems="center">
            <CircularProgress size={16} />
            <Typography variant="body2">{locale.enemiesLoading}</Typography>
          </Stack>
        )
      }
      if (trainingEnemiesError) {
        return <Alert severity="warning">{trainingEnemiesError.message}</Alert>
      }
      return (
        <TrainingEnemySelect
          enemies={trainingEnemies ?? []}
          selectedEnemyId={selectedEnemy?.id ?? null}
          isSelectionDisabled={isTrainingSubmitting}
          tutorialHighlightFirstCard={tutorialStep === 'training-fight'}
          compact
          onSelect={handleSelectEnemy}
        />
      )
    }

    if (isPlayerListLoading) {
      return (
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={16} />
          <Typography variant="body2">{locale.opponentsLoading}</Typography>
        </Stack>
      )
    }
    if (playerListError) {
      return <Alert severity="warning">{playerListError.message}</Alert>
    }
    return (
      <TrainingOpponentSelect
        opponents={(playerListPage?.items ?? [])
          .filter((p) => p.userId !== session?.user.id)
          .sort((a, b) => a.level - b.level)}
        selectedOpponentId={selectedOpponent?.userId ?? null}
        isSelectionDisabled={isTrainingSubmitting}
        page={opponentsPage}
        pageCount={opponentsPageCount}
        compact
        onPageChange={setOpponentsPage}
        onSelect={handleSelectOpponent}
      />
    )
  }

  function renderMovePlan(): ReactNode {
    if (!player || !plannedMoveIds) {
      return null
    }

    return (
      <Box
        ref={trainingMovePlanRef}
        id={tutorialStep === 'training-move-select' ? 'tutorial-move-plan-form' : undefined}
        sx={{ height: '100%' }}
      >
        <TrainingMovePlanForm
          enemy={displayEnemy}
          enemyBackgroundKey={displayEnemyBackgroundKey}
          player={player}
          moveIds={plannedMoveIds}
          isActionDisabled={isTrainingActionDisabled}
          lockRemainingSeconds={trainingLockRemainingSeconds}
          onChangeMoveId={handleChangePlannedMoveId}
          tutorialMoveSelect={
            tutorialStep === 'training-move-select' && nextTutorialMoveTurnIndex !== null
              ? { index: nextTutorialMoveTurnIndex, id: 'tutorial-next-move-select' }
              : undefined
          }
          submitButtonId={tutorialStep === 'training-confirm' ? 'tutorial-start-training-btn' : undefined}
          onSubmit={startBattle}
        />
      </Box>
    )
  }

  const hasResult = trainingResult !== null && displayEnemy !== null

  // 上の段に全幅の戦闘画面（戦闘の前は自分と相手が向かい合い、戦闘のあとは HP とログを出す）、
  // 下の段に相手の一覧と技選択。戦闘の前後で画面の作りは変えない
  const content = player ? (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column', gap: { xs: 2, sm: 3 } }}>
      <Box ref={battleResultRef} sx={{ flex: 'none', height: { md: 'min(400px, 45%)' }, minHeight: { md: 320 } }}>
        <GamePanel
          title={locale.battleFieldTitle}
          // スマホはヘッダーがないので、帯の左端にホームへ戻るボタンを置く
          startAction={
            isDesktop ? undefined : (
              <IconButton
                id={tutorialHomeButtonId}
                component={Link}
                to="/"
                aria-label={locale.backToHome}
                sx={{ color: palette.white, width: 44, height: 44 }}
              >
                <HomeFilledIcon />
              </IconButton>
            )
          }
          disablePadding
          {...panelProps}
        >
          <TrainingBattleField
            player={{
              name: player.userName ?? locale.anonymousPlayer,
              imagePath: player.imagePath ? normalizeCharacterPath(player.imagePath) : DEFAULT_CHARACTER_PATH,
              level: player.level,
            }}
            enemy={displayEnemy}
            backgroundKey={displayEnemyBackgroundKey}
            result={hasResult ? trainingResult : null}
            usedMoveNames={resolveMoveNames(player, lastSubmittedMoveIds)}
            playerExp={playerExp}
            nextLevelRequiredExp={nextLevelRequiredExp}
          />
        </GamePanel>
      </Box>
      {renderColumns(
        <GamePanel tabs={modeTabs} aria-label={locale.enemySelectTitle} {...panelProps}>
          {renderOpponentList()}
        </GamePanel>,
        // スマホは相手を選ぶまで技選択を出さない
        isDesktop || displayEnemy ? (
          // スマホは見出しの帯を出さず、縦幅を節約する
          <GamePanel title={locale.movePlanTitle} hideBand={!isDesktop} {...panelProps}>
            {renderMovePlan()}
          </GamePanel>
        ) : null,
        600,
        // スマホは技選択を一覧の上に出す
        !isWide,
      )}
    </Box>
  ) : null

  // 画面の中身。PC は横に 2 列、狭い画面は縦に積む
  function renderColumns(main: ReactNode, side: ReactNode, sideWidth: number, sideFirst = false) {
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
        <Box sx={{ minHeight: 0, minWidth: 0, order: sideFirst ? 2 : 1 }}>{main}</Box>
        {side ? (
          <Box ref={trainingPanelRef} sx={{ minHeight: 0, minWidth: 0, order: sideFirst ? 1 : 2 }}>
            {side}
          </Box>
        ) : null}
      </Box>
    )
  }

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        background: `linear-gradient(${trainingPanelColors.topBand}, ${trainingPanelColors.topBand}) top / 100% 300px no-repeat, ${trainingPanelColors.ground}`,
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
          currentKey="training"
          currentColor={trainingPanelColors.topBand}
        />
      ) : null}
      <Box
        id="training-main"
        ref={trainingMainRef}
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
        {isPlayerLoading ? (
          <Stack direction="row" spacing={1} alignItems="center">
            <CircularProgress size={16} sx={{ color: palette.white }} />
            <Typography variant="body2" sx={{ color: palette.white }}>
              {locale.playerLoading}
            </Typography>
          </Stack>
        ) : playerError ? (
          <Alert severity="warning">{playerError.message}</Alert>
        ) : null}
        {content}
      </Box>
      {tutorialStep === 'home-job-change' && (
        <SpotlightTutorial
          guide="instructor"
          targetId="tutorial-home-btn"
          message={tutorialLocale.steps.trainingToJobChange.message}
        />
      )}
      {tutorialStep === 'training-fight' && (
        <SpotlightTutorial
          guide="instructor"
          targetId="tutorial-fight-card"
          message={tutorialLocale.steps.trainingFight.message}
        />
      )}
      {tutorialStep === 'training-move-select' && (
        <SpotlightTutorial
          guide="instructor"
          targetId="tutorial-move-plan-form"
          dimBackground={false}
          pointerTargetId="tutorial-next-move-select"
          message={tutorialLocale.steps.trainingMoveSelect.message}
          showDismiss
          onDismiss={() => advanceTutorial('training-confirm')}
        />
      )}
      {tutorialStep === 'training-confirm' && (
        <SpotlightTutorial
          guide="instructor"
          targetId="tutorial-start-training-btn"
          message={tutorialLocale.steps.trainingConfirm.message}
        />
      )}
      {tutorialStep === 'training-to-lv5' && closedTutorialStep !== 'training-to-lv5' && (
        <SpotlightTutorial
          guide="instructor"
          message={tutorialLocale.steps.trainingToLv5.message}
          showDismiss
          dismissLabel={tutorialLocale.closeLabel}
          onDismiss={() => setClosedTutorialStep('training-to-lv5')}
        />
      )}
      {tutorialStep === 'training-to-lv7' && closedTutorialStep !== 'training-to-lv7' && (
        <SpotlightTutorial
          guide="instructor"
          message={tutorialLocale.steps.trainingToLv7.message}
          showDismiss
          dismissLabel={tutorialLocale.closeLabel}
          onDismiss={() => setClosedTutorialStep('training-to-lv7')}
        />
      )}
      {tutorialStep === 'training-quest-guide' && (
        <SpotlightTutorial
          guide="instructor"
          message={tutorialLocale.steps.trainingQuestGuide.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.trainingQuestGuide.dismissLabel}
          onDismiss={() => advanceTutorial('home-quest')}
        />
      )}
    </Box>
  )
}
