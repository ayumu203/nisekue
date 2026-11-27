import {
  Alert,
  Box,
  Button,
  CircularProgress,
  IconButton,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { Link } from 'react-router-dom'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import useSWR from 'swr'
import {
  claimTreasureMapReward,
  getCurrentTreasureMapExpedition,
  getTreasureMaps,
  startTreasureMapExpedition,
} from '@/api/treasureMap'
import { getPlayer } from '@/api/player'
import GamePanel from '@/components/common/GamePanel'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeFilledIcon, HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey, type HomeMenuItem } from '@/components/home/homeMenu'
import { treasureMapPanelColors as colors } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { useNotify } from '@/contexts/useNotify'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import { getTutorialStep, setTutorialStep } from '@/lib/tutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import locale from '../../locale/treasure-map/TreasureMap.json'
import type { TreasureMapExpedition, TreasureMapSummary } from '@/schema/treasureMap'
import { font, palette, withAlpha } from '@/theme/tokens'

const mapBackgroundByGrade: Record<TreasureMapSummary['grade'], string> = {
  S: 'image/quest/moonlit-castle-battlefield.svg',
  A: 'image/quest/floating-islands-battlefield.svg',
  B: 'image/quest/crystal-cavern-battlefield.svg',
  C: 'image/quest/riverbank-battlefield.svg',
  D: 'image/quest/enchanted-forest-battlefield.svg',
}

const companionPlayerCandidates = [
  'ch109_hero.png',
  'ch110_hero.png',
  'ch111_hero.png',
  'ch112_hero.png',
  'ch118_hero.png',
  'ch119_hero.png',
  'ch122_hero.png',
  'ch129_hero.png',
  'ch130_hero.png',
  'ch171_hero.png',
] as const

// 白い箱の中の主な操作（旅に出る・報酬獲得）。見出しの帯と同じ青
const primaryButtonSx = {
  '&&': {
    minHeight: 48,
    backgroundColor: colors.band,
    backgroundImage: 'none',
    border: `2px solid ${colors.band}`,
    borderRadius: '10px',
    color: palette.white,
    fontSize: font.md,
    fontWeight: 800,
    boxShadow: 'none',
    transition: 'none',
  },
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  '&&.Mui-disabled': {
    backgroundColor: colors.disabledButton,
    borderColor: colors.disabledButtonBorder,
    color: colors.disabledButtonText,
  },
} as const

const dottedDivider = `1px dotted ${colors.divider}`

function pickRandomDistinct<T>(values: readonly T[], count: number): T[] {
  const pool = [...values]
  for (let i = pool.length - 1; i > 0; i -= 1) {
    const j = Math.floor(Math.random() * (i + 1))
    const tmp = pool[i]
    pool[i] = pool[j]
    pool[j] = tmp
  }

  return pool.slice(0, Math.min(count, pool.length))
}

function formatSeconds(seconds: number): string {
  if (seconds <= 0) {
    return locale.ended
  }

  const minutes = Math.floor(seconds / 60)
  const remainSeconds = seconds % 60
  return `${minutes}:${String(remainSeconds).padStart(2, '0')}`
}

function formatDuration(seconds: number): string {
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  if (hours > 0) {
    return `${hours}h ${minutes}m`
  }

  return `${minutes}m`
}

function formatExperience(value: number): string {
  return locale.experienceValue.replace('{value}', value.toLocaleString('ja-JP'))
}

function formatGold(value: number): string {
  return locale.goldValue.replace('{value}', value.toLocaleString('ja-JP'))
}

function getProgressRate(expedition: TreasureMapExpedition | null, nowMs: number): number {
  if (!expedition || expedition.status !== 'InProgress') {
    return 0
  }

  const startedAt = new Date(expedition.startedAt).getTime()
  const endsAt = new Date(expedition.endsAt).getTime()
  const total = Math.max(1, endsAt - startedAt)
  const elapsed = Math.min(total, Math.max(0, nowMs - startedAt))

  return Math.round((elapsed * 1000) / total) / 10
}

function isStartDisabled(map: TreasureMapSummary, currentExpedition: TreasureMapExpedition | null): boolean {
  if (map.ownedQuantity < 1) {
    return true
  }

  if (!currentExpedition) {
    return false
  }

  return (
    currentExpedition.status === 'InProgress' ||
    (currentExpedition.status === 'Completed' && !currentExpedition.rewardClaimed)
  )
}

// 地図で手に入りうるもの（「アイテム・装備」）
function describeLootKinds(map: TreasureMapSummary): string {
  const kinds: string[] = []
  if (map.rewardTendency.itemRate > 0) {
    kinds.push(locale.candidateItems)
  }

  if (map.rewardTendency.equipmentRate > 0) {
    kinds.push(locale.candidateEquipments)
  }

  return kinds.length > 0 ? kinds.join('・') : locale.none
}

function gradeStarCount(grade: TreasureMapSummary['grade']): number {
  return grade === 'S' ? 5 : grade === 'A' ? 4 : grade === 'B' ? 3 : grade === 'C' ? 2 : 1
}

// 手に入れたアイテムと装備の名前。同じものを複数もらっても 1 回だけ出す（数は出さない）
function resolveLootNames(expedition: TreasureMapExpedition | null, map: TreasureMapSummary | null): string[] {
  if (!expedition?.reward) {
    return []
  }

  const itemNameById = new Map((map?.rewardCandidates.items ?? []).map((entry) => [entry.itemId, entry.name]))
  const equipmentNameById = new Map(
    (map?.rewardCandidates.equipments ?? []).map((entry) => [entry.equipmentId, entry.name]),
  )
  const names = [
    ...expedition.reward.itemIds.map((id) => itemNameById.get(id) ?? `Item ${id}`),
    ...expedition.reward.equipmentIds.map((id) => equipmentNameById.get(id) ?? `Equipment ${id}`),
  ]
  return Array.from(new Set(names))
}

function GradeStars({ grade }: { grade: TreasureMapSummary['grade'] }) {
  const count = gradeStarCount(grade)
  return (
    <Box component="span" aria-label={`★${count}`} sx={{ letterSpacing: '1px', whiteSpace: 'nowrap' }}>
      <Box component="span" aria-hidden="true" sx={{ color: colors.star }}>
        {'★'.repeat(count)}
      </Box>
      <Box component="span" aria-hidden="true" sx={{ color: colors.starOff }}>
        {'★'.repeat(5 - count)}
      </Box>
    </Box>
  )
}

// 背景の上に置くキャラの絵（装飾なので読み上げない）
function SceneImage({ src, sx }: { src: string; sx: object }) {
  return (
    <Box
      component="img"
      src={src}
      alt=""
      aria-hidden="true"
      sx={{ position: 'absolute', objectFit: 'contain', pointerEvents: 'none', userSelect: 'none', ...sx }}
    />
  )
}

export default function TreasureMap() {
  const { session, isLoading } = useAuth()
  const theme = useTheme()
  // PC（sm 以上）はヘッダーメニュー、md 以上は 2 列、lg 以上は地図の行を横 1 列にし、帰還記録を表にする
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  const isWide = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const isLarge = useMediaQuery(theme.breakpoints.up('lg'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const notify = useNotify()
  const [actionMapId, setActionMapId] = useState<number | null>(null)
  const [isClaiming, setIsClaiming] = useState(false)
  const [nowMs, setNowMs] = useState(() => Date.now())
  const [claimedHistory, setClaimedHistory] = useState<TreasureMapExpedition[]>([])

  useEffect(() => {
    if (!userId) {
      return
    }

    setTutorialStepState(getTutorialStep(userId))
  }, [userId])

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }

  const mapsSWRKey = session?.user.id ? ([`treasure-map-maps`, session.user.id] as const) : null
  const {
    data: maps,
    error: mapsError,
    isLoading: isMapsLoading,
    mutate: mutateMaps,
  } = useSWR(mapsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    return getTreasureMaps(session.access_token)
  })

  const currentSWRKey = session?.user.id ? ([`treasure-map-current`, session.user.id] as const) : null
  const {
    data: currentExpedition,
    error: currentError,
    mutate: mutateCurrent,
  } = useSWR(
    currentSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionMissing)
      }

      return getCurrentTreasureMapExpedition(session.access_token)
    },
    {
      refreshInterval: (data) => (data?.status === 'InProgress' ? 5000 : 0),
    },
  )

  const playerSWRKey = session?.user.id ? ([`treasure-map-player`, session.user.id] as const) : null
  const { data: player } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    return getPlayer(session.access_token)
  })

  useEffect(() => {
    const timerId = window.setInterval(() => {
      setNowMs(Date.now())
    }, 1000)

    return () => {
      window.clearInterval(timerId)
    }
  }, [])

  const remainSeconds = useMemo(() => {
    if (!currentExpedition || currentExpedition.status !== 'InProgress') {
      return 0
    }

    const diffMs = new Date(currentExpedition.endsAt).getTime() - nowMs
    return Math.max(0, Math.floor(diffMs / 1000))
  }, [currentExpedition, nowMs])

  const inProgress = currentExpedition?.status === 'InProgress'
  const canClaim = currentExpedition?.status === 'Completed' && !currentExpedition.rewardClaimed
  const progressRate = useMemo(() => getProgressRate(currentExpedition ?? null, nowMs), [currentExpedition, nowMs])

  const currentMap = useMemo(
    () => (maps ?? []).find((map) => map.mapId === currentExpedition?.mapId) ?? null,
    [maps, currentExpedition],
  )
  const playerImageSrc = resolveCharacterAssetPath(player?.imagePath)
  const showScene = inProgress || canClaim
  const companionImageSrcs = useMemo(() => {
    if (!showScene) {
      return []
    }

    return pickRandomDistinct(companionPlayerCandidates, 2).map((fileName) =>
      resolvePublicAssetPath(`image/character/${fileName}`),
    )
  }, [showScene])

  const rewardResult = currentExpedition?.reward
  const rewardLootNames = useMemo(
    () => resolveLootNames(currentExpedition ?? null, currentMap),
    [currentExpedition, currentMap],
  )

  useEffect(() => {
    if (!currentExpedition || currentExpedition.status !== 'Claimed' || !currentExpedition.rewardClaimed) {
      return
    }

    setClaimedHistory((current) => {
      if (current.some((entry) => entry.expeditionId === currentExpedition.expeditionId)) {
        return current
      }

      return [currentExpedition, ...current]
    })
  }, [currentExpedition])

  const historyExpeditions = useMemo(() => {
    const rows = [...claimedHistory]
    if (currentExpedition && currentExpedition.status === 'Claimed' && currentExpedition.rewardClaimed) {
      if (!rows.some((entry) => entry.expeditionId === currentExpedition.expeditionId)) {
        rows.unshift(currentExpedition)
      }
    }

    return rows
  }, [claimedHistory, currentExpedition])

  async function handleStart(mapId: number): Promise<void> {
    if (!session?.access_token) {
      return
    }

    setActionMapId(mapId)

    try {
      await startTreasureMapExpedition({ mapId }, session.access_token)
      await Promise.all([mutateMaps(), mutateCurrent()])
    } catch (error) {
      notify.error(error instanceof Error ? error.message : locale.sessionMissing)
    } finally {
      setActionMapId(null)
    }
  }

  async function handleClaim(): Promise<void> {
    if (!session?.access_token || !currentExpedition) {
      return
    }

    setIsClaiming(true)

    try {
      const response = await claimTreasureMapReward(currentExpedition.expeditionId, session.access_token)
      setClaimedHistory((current) => {
        if (current.some((entry) => entry.expeditionId === response.expedition.expeditionId)) {
          return current
        }

        return [response.expedition, ...current]
      })
      await Promise.all([mutateMaps(), mutateCurrent()])
    } catch (error) {
      notify.error(error instanceof Error ? error.message : locale.sessionMissing)
    } finally {
      setIsClaiming(false)
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

  const tutorialHomeButtonId = tutorialStep === 'treasure-map-to-home' ? 'tutorial-home-btn-treasure' : undefined

  // PC のヘッダーメニュー。ホームと同じ項目の先頭に「ホーム」を足し、今いる宝の地図を帯の色で示す
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

  // スマホはヘッダーがないので、いちばん上の箱の帯の左端にホームへ戻るボタンを置く
  const mobileHomeButton = isDesktop ? undefined : (
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

  const panelProps = {
    bandColor: colors.band,
    bandSelectedColor: colors.band,
  }

  // 探索中の地図の絵（背景＋自分と仲間＋敵）。探索完了のときは敵を出さずにメッセージウィンドウを重ねる
  function renderScene(mode: 'progress' | 'finished', children?: ReactNode): ReactNode {
    const background = resolvePublicAssetPath(
      currentMap ? mapBackgroundByGrade[currentMap.grade] : 'image/quest/dummy-battlefield.svg',
    )
    const flipped = { transform: 'scaleX(-1)' }
    const party =
      mode === 'progress'
        ? [
            { left: { xs: 12, md: 30 }, bottom: { xs: 10, md: 16 }, height: { xs: 82, md: 120 } },
            { left: { xs: 70, md: 130 }, bottom: { xs: 20, md: 30 }, height: { xs: 72, md: 108 } },
            { left: { xs: 128, md: 230 }, bottom: { xs: 8, md: 14 }, height: { xs: 76, md: 112 } },
          ]
        : [
            { left: { xs: 30, md: 60 }, top: { xs: 16, md: 24 }, height: { xs: 110, md: 150 } },
            { left: { xs: 130, md: 190 }, top: { xs: 26, md: 40 }, height: { xs: 96, md: 130 } },
            { left: { xs: 222, md: 310 }, top: { xs: 20, md: 30 }, height: { xs: 100, md: 136 } },
          ]
    const partySrcs = [playerImageSrc, ...companionImageSrcs]

    return (
      <Box
        sx={{
          position: 'relative',
          overflow: 'hidden',
          height: mode === 'progress' ? { xs: 200, md: 300 } : { xs: 360, md: 440 },
          backgroundColor: palette.gray300,
        }}
      >
        <SceneImage src={background} sx={{ inset: 0, width: '100%', height: '100%', objectFit: 'cover' }} />
        {mode === 'progress' ? (
          <>
            <SceneImage
              src={resolvePublicAssetPath('image/training/04_magic.png')}
              sx={{ right: { xs: 86, md: 170 }, bottom: { xs: 16, md: 26 }, height: { xs: 76, md: 112 } }}
            />
            <SceneImage
              src={resolvePublicAssetPath('image/training/01_heishi.png')}
              sx={{ right: { xs: 14, md: 40 }, bottom: { xs: 14, md: 22 }, height: { xs: 82, md: 120 } }}
            />
          </>
        ) : null}
        {partySrcs.map((src, index) =>
          src ? <SceneImage key={`${src}-${index}`} src={src} sx={{ ...party[index], ...flipped }} /> : null,
        )}
        {mode === 'progress' ? (
          <Typography
            sx={{
              position: 'absolute',
              left: { xs: 10, md: 16 },
              top: { xs: 10, md: 14 },
              px: 1.5,
              py: 0.5,
              borderRadius: '8px',
              backgroundColor: withAlpha(palette.white, 0.75),
              color: colors.text,
              fontSize: { xs: font.md, md: font.lg },
              fontWeight: 800,
            }}
          >
            {currentMap?.name ?? `mapId: ${currentExpedition?.mapId}`}
          </Typography>
        ) : null}
        {children}
      </Box>
    )
  }

  function renderProgressPanel(): ReactNode {
    return (
      <GamePanel
        title={locale.progressTitle}
        startAction={mobileHomeButton}
        disablePadding
        fillHeight={false}
        {...panelProps}
      >
        {renderScene('progress')}
        <Box sx={{ px: { xs: 2, md: 3 }, pt: { xs: 1.5, md: 2 }, pb: { xs: 2, md: 2.5 } }}>
          <Stack direction="row" justifyContent="space-between" alignItems="baseline">
            <Typography sx={{ fontSize: font.sm, fontWeight: 700, color: colors.label }}>
              {locale.remainingTime}
            </Typography>
            <Typography sx={{ fontSize: { xs: font.xl, md: font.xxl }, fontWeight: 800, color: colors.text }}>
              {formatSeconds(remainSeconds)}
            </Typography>
          </Stack>
          <Box
            role="progressbar"
            aria-label={locale.progressRate}
            aria-valuenow={progressRate}
            aria-valuemin={0}
            aria-valuemax={100}
            sx={{
              mt: 1,
              height: { xs: 10, md: 12 },
              borderRadius: 999,
              backgroundColor: palette.cream,
              overflow: 'hidden',
            }}
          >
            <Box sx={{ width: `${progressRate}%`, height: '100%', backgroundColor: colors.band }} />
          </Box>
        </Box>
      </GamePanel>
    )
  }

  // 探索完了：絵の上に RPG のメッセージウィンドウを重ね、見出し・戦利品・経験値とゴールドを出す
  function renderRewardPanel(): ReactNode {
    const statSx = { display: 'flex', flexDirection: 'column' } as const
    const labelSx = { fontSize: { xs: font.xs, md: font.sm }, color: colors.messageLabel } as const
    const valueSx = {
      fontSize: { xs: font.xxl, md: font.xxxl },
      fontWeight: 800,
      color: colors.messageHighlight,
      lineHeight: 1.2,
      whiteSpace: 'nowrap',
    } as const

    return (
      <GamePanel
        title={locale.rewardPreview}
        startAction={mobileHomeButton}
        disablePadding
        fillHeight={false}
        {...panelProps}
      >
        {renderScene(
          'finished',
          <Box
            sx={{
              position: 'absolute',
              left: { xs: 12, md: 20 },
              right: { xs: 12, md: 20 },
              bottom: { xs: 12, md: 20 },
              px: { xs: 2, md: 3 },
              pt: { xs: 1.5, md: 2 },
              pb: { xs: 2, md: 2.5 },
              backgroundColor: colors.messageWindow,
              border: `3px solid ${palette.white}`,
              borderRadius: '12px',
              boxShadow: `0 0 0 3px ${colors.messageWindow}`,
              color: palette.white,
            }}
          >
            <Typography sx={{ fontSize: { xs: font.lg, md: font.xl }, fontWeight: 800 }}>
              {locale.finishedMessage.replace('{mapName}', currentMap?.name ?? locale.title)}
            </Typography>
            <Box
              sx={{
                mt: { xs: 1.5, md: 1.5 },
                pt: { xs: 1.5, md: 1.5 },
                borderTop: `1px solid ${withAlpha(palette.white, 0.25)}`,
                display: 'grid',
                gap: { xs: 1.5, md: 4 },
                gridTemplateColumns: { xs: 'minmax(0, 1fr) auto', md: 'minmax(0, 1fr) auto auto' },
                alignItems: 'end',
              }}
            >
              <Box sx={{ ...statSx, gap: 0.25, minWidth: 0, gridColumn: { xs: '1 / -1', md: 'auto' } }}>
                <Typography component="span" sx={labelSx}>
                  {locale.lootLabel}
                </Typography>
                <Typography
                  component="span"
                  sx={{
                    fontSize: { xs: font.lg, md: font.xl },
                    fontWeight: 800,
                    color: colors.messageHighlight,
                    lineHeight: 1.5,
                  }}
                >
                  {rewardLootNames.length > 0 ? rewardLootNames.join('　') : locale.none}
                </Typography>
              </Box>
              <Box sx={statSx}>
                <Typography component="span" sx={labelSx}>
                  {locale.candidateExperience}
                </Typography>
                <Typography component="span" sx={valueSx}>
                  {formatExperience(rewardResult?.experiencePoints ?? 0)}
                </Typography>
              </Box>
              <Box sx={statSx}>
                <Typography component="span" sx={labelSx}>
                  {locale.candidateGold}
                </Typography>
                <Typography component="span" sx={valueSx}>
                  {formatGold(rewardResult?.gold ?? 0)}
                </Typography>
              </Box>
            </Box>
          </Box>,
        )}
        <Box
          sx={{
            px: { xs: 2, md: 3 },
            pt: { xs: 1.5, md: 2 },
            pb: { xs: 2, md: 2.5 },
            display: 'flex',
            justifyContent: 'flex-end',
          }}
        >
          <Button
            variant="contained"
            sx={{ ...primaryButtonSx, width: { xs: '100%', sm: 240 } }}
            disabled={isClaiming}
            onClick={() => {
              void handleClaim()
            }}
          >
            {isClaiming ? locale.claiming : locale.claimButton}
          </Button>
        </Box>
      </GamePanel>
    )
  }

  function renderMapAction(map: TreasureMapSummary, isExploringMap: boolean): ReactNode {
    if (isExploringMap) {
      return (
        <Box
          sx={{
            width: { xs: 120, lg: 160 },
            height: { xs: 44, lg: 48 },
            flex: 'none',
            display: 'grid',
            placeItems: 'center',
            borderRadius: '10px',
            backgroundColor: colors.soft,
            color: colors.softText,
            fontSize: { xs: font.md, lg: font.md },
            fontWeight: 800,
          }}
        >
          {locale.detailsLocked}
        </Box>
      )
    }

    if (map.ownedQuantity < 1) {
      return isLarge ? <Box aria-hidden="true" sx={{ width: 160, flex: 'none' }} /> : null
    }

    return (
      <Button
        variant="contained"
        sx={{
          ...primaryButtonSx,
          width: { xs: 120, lg: 160 },
          flex: 'none',
          '&&': { ...primaryButtonSx['&&'], minHeight: { xs: 44, lg: 48 } },
        }}
        disabled={isStartDisabled(map, currentExpedition ?? null) || actionMapId === map.mapId}
        onClick={() => {
          void handleStart(map.mapId)
        }}
      >
        {actionMapId === map.mapId ? locale.starting : locale.startButton}
      </Button>
    )
  }

  function renderMapRow(map: TreasureMapSummary, index: number): ReactNode {
    const isExploringMap = inProgress && currentExpedition?.mapId === map.mapId
    const dimSx = map.ownedQuantity < 1 ? { opacity: 0.55 } : {}
    const labelSx = { fontSize: { xs: font.sm, lg: font.sm }, color: colors.label } as const
    const strongSx = { fontWeight: 800, color: colors.text } as const
    const image = (
      <Box
        component="img"
        src={resolvePublicAssetPath(mapBackgroundByGrade[map.grade])}
        alt=""
        aria-hidden="true"
        sx={{
          width: { xs: 96, lg: 200 },
          height: { xs: 72, lg: 128 },
          flex: 'none',
          objectFit: 'cover',
          borderRadius: { xs: '8px', lg: '10px' },
          display: 'block',
        }}
      />
    )
    const description = (
      <Typography sx={labelSx}>
        {isExploringMap ? '……' : map.narrativeText || map.description || locale.none}
      </Typography>
    )
    const title = (
      <Stack direction="row" alignItems="baseline" spacing={1.5} useFlexGap flexWrap="wrap">
        <Typography component="h3" sx={{ fontSize: { xs: font.lg, lg: font.xl }, ...strongSx }}>
          {map.name}
        </Typography>
        <Typography component="span" sx={{ ...labelSx, fontWeight: 700 }}>
          {locale.rankLabel.replace('{grade}', map.grade)} <GradeStars grade={map.grade} />
        </Typography>
      </Stack>
    )
    const lootLine = (
      <Typography sx={labelSx}>
        {locale.lootLabel}：
        <Box component="span" sx={{ fontWeight: 700, color: colors.text }}>
          {describeLootKinds(map)}
        </Box>
      </Typography>
    )

    return (
      <Box
        key={map.mapId}
        sx={{ px: { xs: 2, lg: 2.5 }, py: { xs: 2, lg: 2 }, borderTop: index === 0 ? 'none' : dottedDivider }}
      >
        {isLarge ? (
          <Stack direction="row" alignItems="center" spacing={2.5}>
            <Box sx={dimSx}>{image}</Box>
            <Stack spacing={0.75} sx={{ flex: 1, minWidth: 0, ...dimSx }}>
              {title}
              {description}
              {lootLine}
            </Stack>
            <Stack spacing={0.75} sx={{ width: 150, flex: 'none', ...dimSx }}>
              <Stack direction="row" justifyContent="space-between">
                <Typography sx={labelSx}>{locale.duration}</Typography>
                <Typography sx={{ ...labelSx, ...strongSx }}>{formatDuration(map.durationSeconds)}</Typography>
              </Stack>
              <Stack direction="row" justifyContent="space-between">
                <Typography sx={labelSx}>{locale.ownedQuantity}</Typography>
                <Typography sx={{ ...labelSx, ...strongSx }}>{map.ownedQuantity}</Typography>
              </Stack>
            </Stack>
            {renderMapAction(map, isExploringMap)}
          </Stack>
        ) : (
          <>
            <Stack direction="row" spacing={1.5} sx={dimSx}>
              {image}
              <Stack spacing={0.5} sx={{ minWidth: 0 }}>
                {title}
                {description}
              </Stack>
            </Stack>
            <Stack
              direction="row"
              alignItems="center"
              justifyContent="space-between"
              spacing={1}
              sx={{ mt: 1.5, minHeight: 44 }}
            >
              <Stack spacing={0.25} sx={dimSx}>
                {lootLine}
                <Typography sx={labelSx}>
                  {locale.duration}{' '}
                  <Box component="span" sx={strongSx}>
                    {formatDuration(map.durationSeconds)}
                  </Box>
                  {'　'}
                  {locale.ownedQuantity}{' '}
                  <Box component="span" sx={strongSx}>
                    {map.ownedQuantity}
                  </Box>
                </Typography>
              </Stack>
              {renderMapAction(map, isExploringMap)}
            </Stack>
          </>
        )}
      </Box>
    )
  }

  function renderMapsPanel(): ReactNode {
    return (
      <GamePanel
        title={locale.mapListTitle}
        startAction={showScene ? undefined : mobileHomeButton}
        disablePadding
        fillHeight={isWide}
        {...panelProps}
      >
        {isMapsLoading ? (
          <Stack direction="row" spacing={1} alignItems="center" sx={{ p: 2.5 }}>
            <CircularProgress size={16} />
            <Typography variant="body2" sx={{ color: colors.label }}>
              {locale.mapsLoading}
            </Typography>
          </Stack>
        ) : mapsError ? (
          <Alert severity="warning" sx={{ m: 2 }}>
            {mapsError.message}
          </Alert>
        ) : (
          (maps ?? []).map((map, index) => renderMapRow(map, index))
        )}
      </GamePanel>
    )
  }

  function renderHistoryPanel(): ReactNode {
    const rows = historyExpeditions.map((expedition) => {
      const historyMap = (maps ?? []).find((map) => map.mapId === expedition.mapId) ?? null
      return {
        id: expedition.expeditionId,
        name: historyMap?.name ?? `mapId: ${expedition.mapId}`,
        loot: resolveLootNames(expedition, historyMap).join('、') || locale.none,
        experience: formatExperience(expedition.reward?.experiencePoints ?? 0),
        gold: formatGold(expedition.reward?.gold ?? 0),
      }
    })
    const numberSx = { whiteSpace: 'nowrap', fontVariantNumeric: 'tabular-nums' } as const

    let body: ReactNode
    if (rows.length === 0) {
      body = <Typography sx={{ py: 1.5, fontSize: font.sm, color: colors.label }}>{locale.noHistory}</Typography>
    } else if (isLarge) {
      // PC：地図／戦利品／経験値／ゴールドの表
      const headSx = {
        py: 1.5,
        px: 0,
        fontSize: font.sm,
        fontWeight: 700,
        color: colors.label,
        borderBottom: `1px solid ${colors.divider}`,
        textAlign: 'right',
      } as const
      const cellSx = {
        py: 1.5,
        px: 0,
        borderTop: dottedDivider,
        verticalAlign: 'top',
        color: colors.text,
        textAlign: 'right',
        fontSize: font.md,
        fontWeight: 700,
      } as const
      body = (
        <Box component="table" sx={{ width: '100%', borderCollapse: 'collapse' }}>
          <colgroup>
            <col style={{ width: 140 }} />
            <col />
            <col style={{ width: 100 }} />
            <col style={{ width: 100 }} />
          </colgroup>
          <thead>
            <tr>
              <Box component="th" scope="col" sx={{ ...headSx, textAlign: 'left' }}>
                {locale.historyMapColumn}
              </Box>
              <Box component="th" scope="col" sx={{ ...headSx, textAlign: 'left', pl: 2 }}>
                {locale.lootLabel}
              </Box>
              <Box component="th" scope="col" sx={headSx}>
                {locale.candidateExperience}
              </Box>
              <Box component="th" scope="col" sx={headSx}>
                {locale.candidateGold}
              </Box>
            </tr>
          </thead>
          <tbody>
            {rows.map((row, index) => (
              <tr key={row.id}>
                <Box
                  component="td"
                  sx={{
                    ...cellSx,
                    borderTop: index === 0 ? 'none' : dottedDivider,
                    textAlign: 'left',
                    fontSize: font.md,
                    fontWeight: 800,
                  }}
                >
                  {row.name}
                </Box>
                <Box
                  component="td"
                  sx={{
                    ...cellSx,
                    borderTop: index === 0 ? 'none' : dottedDivider,
                    textAlign: 'left',
                    pl: 2,
                    fontSize: font.sm,
                    lineHeight: 1.5,
                  }}
                >
                  {row.loot}
                </Box>
                <Box component="td" sx={{ ...cellSx, ...numberSx, borderTop: index === 0 ? 'none' : dottedDivider }}>
                  {row.experience}
                </Box>
                <Box component="td" sx={{ ...cellSx, ...numberSx, borderTop: index === 0 ? 'none' : dottedDivider }}>
                  {row.gold}
                </Box>
              </tr>
            ))}
          </tbody>
        </Box>
      )
    } else {
      // スマホ・狭い PC：1 件を 2 行（名前と経験値・ゴールド／戦利品）にする
      body = rows.map((row, index) => (
        <Stack key={row.id} spacing={0.5} sx={{ py: 1.5, borderTop: index === 0 ? 'none' : dottedDivider }}>
          <Stack direction="row" justifyContent="space-between" alignItems="baseline" spacing={1}>
            <Typography sx={{ fontSize: font.md, fontWeight: 800, color: colors.text }}>{row.name}</Typography>
            <Typography sx={{ fontSize: font.sm, fontWeight: 700, color: colors.text, ...numberSx }}>
              {row.experience}
              {'　'}
              {row.gold}
            </Typography>
          </Stack>
          <Typography sx={{ fontSize: font.sm, color: colors.label, lineHeight: 1.5 }}>
            {locale.lootLabel}：
            <Box component="span" sx={{ fontWeight: 700, color: colors.text }}>
              {row.loot}
            </Box>
          </Typography>
        </Stack>
      ))
    }

    return (
      <GamePanel title={locale.historyTitle} fillHeight={isWide} disablePadding {...panelProps}>
        <Box sx={{ px: { xs: 2, md: 3 }, py: 0.5 }}>{body}</Box>
      </GamePanel>
    )
  }

  const expeditionPanel = inProgress ? renderProgressPanel() : canClaim ? renderRewardPanel() : null

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        background: `linear-gradient(${colors.topBand}, ${colors.topBand}) top / 100% ${isDesktop ? 300 : 200}px no-repeat, ${colors.ground}`,
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
          currentKey="treasureMap"
          currentColor={colors.topBand}
        />
      ) : null}
      <Box
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
        {currentError ? <Alert severity="warning">{currentError.message}</Alert> : null}
        {/* PC（md 以上）：左に地図の一覧、右に探索中／戦利品と帰還記録。狭い画面は 探索中／戦利品 → 地図 → 帰還記録 の順に積む */}
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gap: { xs: 1.5, sm: 3 },
            gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1fr) minmax(0, 1fr)' },
            gridTemplateRows: { md: 'auto minmax(0, 1fr)' },
            // 狭い画面では箱を中身の高さのまま上から積む（行を画面の高さまで伸ばさない）
            alignContent: { xs: 'start', md: 'stretch' },
            gridTemplateAreas: {
              xs: '"expedition" "maps" "history"',
              md: expeditionPanel ? '"maps expedition" "maps history"' : '"maps history" "maps history"',
            },
          }}
        >
          {expeditionPanel ? <Box sx={{ gridArea: 'expedition', minWidth: 0 }}>{expeditionPanel}</Box> : null}
          <Box sx={{ gridArea: 'maps', minWidth: 0, minHeight: 0 }}>{renderMapsPanel()}</Box>
          <Box sx={{ gridArea: 'history', minWidth: 0, minHeight: 0 }}>{renderHistoryPanel()}</Box>
        </Box>
      </Box>
      {tutorialStep === 'treasure-map-info' && (
        <SpotlightTutorial
          guide="adventurer"
          message={tutorialLocale.steps.treasureMapInfo.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.treasureMapInfo.dismissLabel}
          onDismiss={() => advanceTutorial('treasure-map-to-home')}
        />
      )}
      {tutorialStep === 'treasure-map-to-home' && (
        <SpotlightTutorial
          guide="adventurer"
          targetId="tutorial-home-btn-treasure"
          message={tutorialLocale.steps.treasureMapToHome.message}
        />
      )}
    </Box>
  )
}
