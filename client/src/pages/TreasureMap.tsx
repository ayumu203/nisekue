import { Alert, Box, Button, Chip, CircularProgress, Container, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import { useEffect, useMemo, useState } from 'react'
import useSWR from 'swr'
import { claimTreasureMapReward, getCurrentTreasureMapExpedition, getTreasureMaps, startTreasureMapExpedition } from '@/api/treasureMap'
import { getPlayer } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { mutedGreenButtonSx, outerPagePaperSx, softGreenButtonSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import locale from '../../locale/treasure-map/TreasureMap.json'
import type { TreasureMapExpedition, TreasureMapSummary } from '@/schema/treasureMap'

const deepGreen = '#1f4a33'
const deepGreenBorder = '#2e6a49'
const sectionTitleColor = '#365f3c'
const sectionBodyColor = '#5b4523'
const sectionHeadingSx = {
  color: sectionTitleColor,
  fontSize: { xs: '1.42rem', sm: '1.56rem' },
  lineHeight: 1.2,
} as const
const sectionPanelSx = {
  borderColor: '#c69a43',
  background: 'linear-gradient(180deg, #fff3c9 0%, #f5e2b5 100%)',
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.72)',
} as const

const mapBackgroundByGrade: Record<TreasureMapSummary['grade'], string> = {
  S: 'image/quest/moonlit-castle-battlefield.svg',
  A: 'image/quest/floating-islands-battlefield.svg',
  B: 'image/quest/crystal-cavern-battlefield.svg',
  C: 'image/quest/riverbank-battlefield.svg',
  D: 'image/quest/enchanted-forest-battlefield.svg',
}

const companionPlayerCandidates = [
  { fileName: 'ch109_hero.png', widthXs: 54, widthSm: 68 },
  { fileName: 'ch110_hero.png', widthXs: 52, widthSm: 66 },
  { fileName: 'ch111_hero.png', widthXs: 60, widthSm: 74 },
  { fileName: 'ch112_hero.png', widthXs: 62, widthSm: 76 },
  { fileName: 'ch118_hero.png', widthXs: 56, widthSm: 70 },
  { fileName: 'ch119_hero.png', widthXs: 55, widthSm: 69 },
  { fileName: 'ch122_hero.png', widthXs: 64, widthSm: 78 },
  { fileName: 'ch129_hero.png', widthXs: 57, widthSm: 71 },
  { fileName: 'ch130_hero.png', widthXs: 61, widthSm: 75 },
  { fileName: 'ch171_hero.png', widthXs: 58, widthSm: 72 },
] as const

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

function formatStatus(status: TreasureMapExpedition['status']): string {
  switch (status) {
    case 'InProgress':
      return locale.inProgressLabel
    case 'Completed':
      return locale.completedLabel
    case 'Claimed':
      return locale.claimedLabel
    case 'Failed':
      return locale.failedLabel
  }
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

  return currentExpedition.status === 'InProgress' || (currentExpedition.status === 'Completed' && !currentExpedition.rewardClaimed)
}

function buildTendencyChips(map: TreasureMapSummary): Array<{ key: string; label: string }> {
  const chips: Array<{ key: string; label: string }> = []

  if (map.rewardTendency.itemRate > 0) {
    chips.push({ key: 'item', label: `${locale.candidateItems} ${map.rewardTendency.itemRate}%` })
  }

  if (map.rewardTendency.equipmentRate > 0) {
    chips.push({ key: 'equipment', label: `${locale.candidateEquipments} ${map.rewardTendency.equipmentRate}%` })
  }

  return chips
}

function getDominantTendency(map: TreasureMapSummary): string {
  const entries = [
    { label: locale.candidateItems, rate: map.rewardTendency.itemRate },
    { label: locale.candidateEquipments, rate: map.rewardTendency.equipmentRate },
  ]
  const top = entries.sort((a, b) => b.rate - a.rate)[0]
  return top && top.rate > 0 ? `${top.label} ${top.rate}%` : locale.none
}

function gradeStars(grade: TreasureMapSummary['grade']): string {
  const count = grade === 'S' ? 5 : grade === 'A' ? 4 : grade === 'B' ? 3 : grade === 'C' ? 2 : 1
  return '★'.repeat(count)
}

function aggregateCounts(values: string[]): Array<{ label: string; count: number }> {
  const counts = new Map<string, number>()
  values.forEach((value) => {
    counts.set(value, (counts.get(value) ?? 0) + 1)
  })

  return Array.from(counts.entries()).map(([label, count]) => ({ label, count }))
}

function resolveRewardItemNames(expedition: TreasureMapExpedition | null, map: TreasureMapSummary | null): Array<{ label: string; count: number }> {
  if (!expedition?.reward || !map) {
    return []
  }

  const nameById = new Map(map.rewardCandidates.items.map((entry) => [entry.itemId, entry.name]))
  const names = expedition.reward.itemIds.map((id) => nameById.get(id) ?? `Item ${id}`)
  return aggregateCounts(names)
}

function resolveRewardEquipmentNames(expedition: TreasureMapExpedition | null, map: TreasureMapSummary | null): Array<{ label: string; count: number }> {
  if (!expedition?.reward || !map) {
    return []
  }

  const nameById = new Map(map.rewardCandidates.equipments.map((entry) => [entry.equipmentId, entry.name]))
  const names = expedition.reward.equipmentIds.map((id) => nameById.get(id) ?? `Equipment ${id}`)
  return aggregateCounts(names)
}

function buildLootStory(
  map: TreasureMapSummary | null,
  rewardItems: Array<{ label: string; count: number }>,
  rewardEquipments: Array<{ label: string; count: number }>,
): string {
  const mapName = map?.name ?? '冒険者'
  const names = [
    ...rewardItems.map((entry) => `${entry.label}${entry.count > 1 ? ` ${entry.count}個` : ''}`),
    ...rewardEquipments.map((entry) => `${entry.label}${entry.count > 1 ? ` ${entry.count}個` : ''}`),
  ]
  const gained = names.length > 0 ? names.slice(0, 2).join('、') : '戦利品'
  return `${mapName}の探索を終え、${gained}を獲得した。`
}

export default function TreasureMap() {
  const { session, isLoading } = useAuth()
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionMapId, setActionMapId] = useState<number | null>(null)
  const [isClaiming, setIsClaiming] = useState(false)
  const [nowMs, setNowMs] = useState(() => Date.now())
  const [claimedHistory, setClaimedHistory] = useState<TreasureMapExpedition[]>([])

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
    isLoading: isCurrentLoading,
    mutate: mutateCurrent,
  } = useSWR(currentSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    return getCurrentTreasureMapExpedition(session.access_token)
  }, {
    refreshInterval: 5000,
  })

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
  const progressRate = useMemo(() => getProgressRate(currentExpedition ?? null, nowMs), [currentExpedition, nowMs])

  const currentMap = useMemo(
    () => (maps ?? []).find((map) => map.mapId === currentExpedition?.mapId) ?? null,
    [maps, currentExpedition],
  )
  const playerImageSrc = resolveCharacterAssetPath(player?.imagePath)
  const companionPlayers = useMemo(() => {
    if (!inProgress) {
      return []
    }

    return pickRandomDistinct(companionPlayerCandidates, 2).map((candidate) => ({
      src: resolvePublicAssetPath(`image/character/${candidate.fileName}`),
      widthXs: candidate.widthXs,
      widthSm: candidate.widthSm,
    }))
  }, [inProgress, currentExpedition?.expeditionId])

  const rewardResult = currentExpedition?.reward
  const rewardItemCount = rewardResult?.itemIds.length ?? 0
  const rewardEquipmentCount = rewardResult?.equipmentIds.length ?? 0
  const rewardExp = rewardResult?.experiencePoints ?? 0
  const rewardGold = rewardResult?.gold ?? 0
  const rewardItemNames = useMemo(
    () => resolveRewardItemNames(currentExpedition ?? null, currentMap),
    [currentExpedition, currentMap],
  )
  const rewardEquipmentNames = useMemo(
    () => resolveRewardEquipmentNames(currentExpedition ?? null, currentMap),
    [currentExpedition, currentMap],
  )
  const lootStory = useMemo(
    () => buildLootStory(currentMap, rewardItemNames, rewardEquipmentNames),
    [currentMap, rewardItemNames, rewardEquipmentNames],
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
    setActionError(null)

    try {
      await startTreasureMapExpedition({ mapId }, session.access_token)
      await Promise.all([mutateMaps(), mutateCurrent()])
    } catch (error) {
      setActionError(error instanceof Error ? error.message : locale.sessionMissing)
    } finally {
      setActionMapId(null)
    }
  }

  async function handleClaim(): Promise<void> {
    if (!session?.access_token || !currentExpedition) {
      return
    }

    setIsClaiming(true)
    setActionError(null)

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
      setActionError(error instanceof Error ? error.message : locale.sessionMissing)
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

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 6 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack direction="row" justifyContent="flex-start" sx={{ mb: 1.5 }}>
          <HomeNavIconButton ariaLabel={locale.backToHome} />
        </Stack>
        <Box
          sx={{
            borderRadius: 3,
            border: `1px solid ${deepGreenBorder}`,
            backgroundColor: deepGreen,
            p: { xs: 1.5, sm: 2 },
          }}
        >
          <Stack spacing={2}>
            <Box>
              <Typography variant="overline" sx={{ letterSpacing: '0.16em', color: 'rgba(255,255,255,0.7)' }}>
                TREASURE MAP
              </Typography>
              <Typography variant="h4" fontWeight={900} color="#ffffff">
                {locale.title}
              </Typography>
            </Box>

            {actionError ? <Alert severity="warning">{actionError}</Alert> : null}

            {isCurrentLoading ? (
              <Paper
                variant="outlined"
                sx={{
                  p: { xs: 2, sm: 2.5 },
                  borderRadius: 3,
                  ...sectionPanelSx,
                }}
              >
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2" sx={{ color: sectionBodyColor }}>{locale.currentLoading}</Typography>
                </Stack>
              </Paper>
            ) : currentError ? (
              <Alert severity="warning">{currentError.message}</Alert>
            ) : currentExpedition && inProgress ? (
              <Paper
                variant="outlined"
                sx={{
                  p: { xs: 2, sm: 2.5 },
                  borderRadius: 3,
                  ...sectionPanelSx,
                }}
              >
                <Stack spacing={1.5}>
                  <Typography variant="h6" fontWeight={900} sx={{ color: sectionTitleColor }}>{locale.progressTitle}</Typography>
                  <Paper
                    variant="outlined"
                    sx={{
                      p: 1,
                      borderRadius: 2,
                      borderColor: '#d9c79d',
                      backgroundColor: 'rgba(255,255,255,0.92)',
                    }}
                  >
                    <Stack spacing={0.6}>
                      <Typography variant="caption" fontWeight={700}>
                        {locale.remainingTime}: {formatSeconds(remainSeconds)}
                      </Typography>
                      <LinearProgress variant="determinate" value={progressRate} sx={{ height: 7, borderRadius: 999 }} />
                    </Stack>
                  </Paper>
                  <Paper
                    variant="outlined"
                    sx={{
                      borderRadius: 2,
                      overflow: 'hidden',
                      position: 'relative',
                      minHeight: { xs: 268, sm: 240 },
                      borderColor: '#d1ba83',
                      backgroundColor: '#d9e6df',
                    }}
                  >
                    <Box
                      component="img"
                      src={resolvePublicAssetPath(currentMap ? mapBackgroundByGrade[currentMap.grade] : 'image/quest/dummy-battlefield.svg')}
                      alt=""
                      aria-hidden="true"
                      sx={{
                        position: 'absolute',
                        inset: 0,
                        width: '100%',
                        height: '100%',
                        objectFit: 'cover',
                        objectPosition: 'center',
                        zIndex: 0,
                        pointerEvents: 'none',
                        userSelect: 'none',
                      }}
                    />
                    <Box
                      sx={{
                        position: 'absolute',
                        inset: 0,
                        zIndex: 1,
                        background: 'linear-gradient(180deg, rgba(17,35,23,0.08) 0%, rgba(255,247,232,0.92) 76%)',
                      }}
                    />

                    <Box
                      component="img"
                      src={resolvePublicAssetPath('image/training/01_heishi.png')}
                      alt=""
                      aria-hidden="true"
                      sx={{
                        position: 'absolute',
                        right: { xs: 12, sm: 44 },
                        top: { xs: '20%', sm: 'auto' },
                        bottom: { xs: 'auto', sm: 10 },
                        width: { xs: 82, sm: 102 },
                        opacity: 0.9,
                        zIndex: 2,
                        pointerEvents: 'none',
                        userSelect: 'none',
                      }}
                    />
                    <Box
                      component="img"
                      src={resolvePublicAssetPath('image/training/04_magic.png')}
                      alt=""
                      aria-hidden="true"
                      sx={{
                        position: 'absolute',
                        right: { xs: 102, sm: 226 },
                        top: { xs: '24%', sm: 'auto' },
                        bottom: { xs: 'auto', sm: 10 },
                        width: { xs: 80, sm: 100 },
                        opacity: 0.86,
                        zIndex: 2,
                        pointerEvents: 'none',
                        userSelect: 'none',
                      }}
                    />
                    {playerImageSrc ? (
                      <Box
                        component="img"
                        src={playerImageSrc}
                        alt=""
                        aria-hidden="true"
                        sx={{
                          position: 'absolute',
                          left: { xs: 10, sm: 8 },
                          bottom: { xs: 7, sm: 9 },
                          width: { xs: 58, sm: 72 },
                          opacity: 0.96,
                          zIndex: 2,
                          objectFit: 'contain',
                          objectPosition: 'center bottom',
                          transform: 'scaleX(-1) rotate(-1.5deg)',
                          transformOrigin: 'center bottom',
                          pointerEvents: 'none',
                          userSelect: 'none',
                        }}
                      />
                    ) : null}
                    {companionPlayers.map((companion, index) => (
                      <Box
                        key={`companion-player-${companion.src}`}
                        component="img"
                        src={companion.src}
                        alt=""
                        aria-hidden="true"
                        sx={{
                          position: 'absolute',
                          left: index === 0 ? { xs: 64, sm: 92 } : { xs: 126, sm: 192 },
                          bottom: index === 0 ? { xs: 16, sm: 20 } : { xs: 4, sm: 7 },
                          width: { xs: companion.widthXs, sm: companion.widthSm },
                          opacity: 0.96,
                          zIndex: 2,
                          objectFit: 'contain',
                          objectPosition: 'center bottom',
                          transform: 'scaleX(-1) rotate(-1.5deg)',
                          transformOrigin: 'center bottom',
                          pointerEvents: 'none',
                          userSelect: 'none',
                        }}
                      />
                    ))}

                    <Stack spacing={1} sx={{ position: 'relative', zIndex: 2, p: { xs: 1.5, sm: 2 } }}>
                      <Typography variant="h6" fontWeight={900}>{currentMap?.name ?? `mapId: ${currentExpedition.mapId}`}</Typography>
                      <Typography variant="body2" sx={{ maxWidth: 560 }}>
                        {currentMap?.narrativeText ?? currentMap?.description ?? locale.none}
                      </Typography>

                    </Stack>
                  </Paper>
                </Stack>
              </Paper>
            ) : null}

            {currentExpedition && currentExpedition.status === 'Completed' && !currentExpedition.rewardClaimed ? (
              <Paper
                variant="outlined"
                sx={{
                  p: { xs: 1.5, sm: 2 },
                  borderRadius: 3,
                  ...sectionPanelSx,
                }}
              >
                <Stack spacing={0.9}>
                  <Stack
                    direction="row"
                    justifyContent="space-between"
                    alignItems="flex-start"
                    spacing={1}
                  >
                    <Typography variant="h6" fontWeight={900} sx={sectionHeadingSx}>{locale.rewardPreview}</Typography>
                    <Button
                      variant="contained"
                      sx={{ ...softGreenButtonSx, flexShrink: 0 }}
                      disabled={isClaiming}
                      onClick={handleClaim}
                    >
                      {isClaiming ? locale.claiming : locale.claimButton}
                    </Button>
                  </Stack>
                  <Typography
                    variant="h6"
                    sx={{
                      color: '#6c4a16',
                      fontWeight: 900,
                      letterSpacing: '0.01em',
                      textShadow: '0 1px 0 rgba(255,255,255,0.6)',
                      fontSize: { xs: '1.22rem', sm: '1.34rem' },
                      mt: { xs: 3.2, sm: 3.8 },
                    }}
                  >
                    {lootStory}
                  </Typography>
                  <Box
                    sx={{
                      borderRadius: 2,
                      border: '1px solid #e2cfa1',
                      background: 'linear-gradient(140deg, #fffdf6 0%, #fff4d9 100%)',
                      p: { xs: 1, sm: 1.1 },
                    }}
                  >
                    <Box
                      sx={{
                        display: 'grid',
                        gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(4, minmax(0, 1fr))' },
                        gap: 0.7,
                      }}
                    >
                      <Paper variant="outlined" sx={{ p: 0.8, borderRadius: 1.2, borderColor: '#78b97b', backgroundColor: '#dcf4dd', boxShadow: '0 0 0 2px rgba(120,185,123,0.2) inset' }}>
                        <Typography variant="caption" color="#2d6d33">{locale.candidateItems}</Typography>
                        <Typography variant="subtitle1" fontWeight={800} color="#1f5b26">x{rewardItemCount}</Typography>
                      </Paper>
                      <Paper variant="outlined" sx={{ p: 0.8, borderRadius: 1.2, borderColor: '#b9ccee', backgroundColor: '#edf3ff' }}>
                        <Typography variant="caption" color="#2f4f88">{locale.candidateEquipments}</Typography>
                        <Typography variant="subtitle1" fontWeight={800} color="#1e3f78">x{rewardEquipmentCount}</Typography>
                      </Paper>
                      <Paper variant="outlined" sx={{ p: 0.8, borderRadius: 1.2, borderColor: '#efc78e', backgroundColor: '#fff3e5' }}>
                        <Typography variant="caption" color="#9a5a00">{locale.candidateExperience}</Typography>
                        <Typography variant="subtitle1" fontWeight={800} color="#8a4e00">+{rewardExp}</Typography>
                      </Paper>
                      <Paper variant="outlined" sx={{ p: 0.8, borderRadius: 1.2, borderColor: '#e3cf83', backgroundColor: '#fff8d9' }}>
                        <Typography variant="caption" color="#7f6300">{locale.candidateGold}</Typography>
                        <Typography variant="subtitle1" fontWeight={800} color="#7a5c00">+{rewardGold}</Typography>
                      </Paper>
                    </Box>
                  </Box>
                </Stack>
              </Paper>
            ) : null}

            <Paper
              variant="outlined"
              sx={{
                p: { xs: 2, sm: 2.5 },
                borderRadius: 3,
                ...sectionPanelSx,
              }}
            >
              <Stack spacing={1.5}>
                <Typography variant="h6" fontWeight={900} sx={sectionHeadingSx}>{locale.mapListTitle}</Typography>
                {isMapsLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2" sx={{ color: sectionBodyColor }}>{locale.mapsLoading}</Typography>
                  </Stack>
                ) : mapsError ? (
                  <Alert severity="warning">{mapsError.message}</Alert>
                ) : (
                  <Stack spacing={1.25}>
                    {(maps ?? []).map((map) => {
                      const tendencyChips = buildTendencyChips(map)
                      const narrativeLocked = inProgress && currentExpedition?.mapId === map.mapId
                      return (
                        <Paper
                          key={map.mapId}
                          variant="outlined"
                          sx={{
                            borderRadius: 2,
                            overflow: 'hidden',
                            borderColor: '#c6d6ca',
                            backgroundColor: '#ffffff',
                          }}
                        >
                          <Box
                            sx={{
                              position: 'relative',
                              p: { xs: 1.25, sm: 1.5 },
                              minHeight: 156,
                            }}
                          >
                            <Box
                              component="img"
                              src={resolvePublicAssetPath(mapBackgroundByGrade[map.grade])}
                              alt=""
                              aria-hidden="true"
                              sx={{
                                position: 'absolute',
                                inset: 0,
                                width: '100%',
                                height: '100%',
                                objectFit: 'cover',
                                opacity: 0.18,
                                pointerEvents: 'none',
                                userSelect: 'none',
                              }}
                            />
                            <Stack
                              direction={{ xs: 'column', sm: 'row' }}
                              spacing={{ xs: 1, sm: 1.5 }}
                              justifyContent="space-between"
                              alignItems={{ xs: 'stretch', sm: 'stretch' }}
                              sx={{ position: 'relative', zIndex: 1 }}
                            >
                              <Stack spacing={1} flex={1} minWidth={0}>
                                <Chip size="small" sx={{ alignSelf: 'flex-start' }} label={`ランク${map.grade}`} />
                                <Typography variant="h6" fontWeight={900}>{map.name}</Typography>
                                <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                                  {tendencyChips.length > 0
                                    ? tendencyChips.map((chip) => (
                                      <Chip
                                        key={`${map.mapId}-${chip.key}`}
                                        size="small"
                                        variant="outlined"
                                        sx={{
                                          borderColor: chip.key === 'item'
                                            ? '#7eb67e'
                                            : chip.key === 'equipment'
                                              ? '#7f9bcf'
                                              : chip.key === 'experience'
                                                ? '#d8a05b'
                                                : '#d2b35f',
                                          backgroundColor: chip.key === 'item'
                                            ? '#e8f6e8'
                                            : chip.key === 'equipment'
                                              ? '#edf3ff'
                                              : chip.key === 'experience'
                                                ? '#fff3e5'
                                                : '#fff9dd',
                                        }}
                                        label={chip.label}
                                      />
                                    ))
                                    : <Chip size="small" variant="outlined" label={locale.none} />}
                                </Stack>
                                <Typography variant="body2" color="text.secondary">
                                  {narrativeLocked ? '……' : (map.narrativeText || map.description || locale.none)}
                                </Typography>
                                <Box sx={{ minHeight: 40, display: 'flex', alignItems: 'flex-start' }}>
                                  {map.ownedQuantity > 0 ? (
                                    <Button
                                      variant="outlined"
                                      sx={{ ...mutedGreenButtonSx, alignSelf: 'flex-start' }}
                                      disabled={isStartDisabled(map, currentExpedition ?? null) || actionMapId === map.mapId}
                                      onClick={() => {
                                        void handleStart(map.mapId)
                                      }}
                                    >
                                      {actionMapId === map.mapId ? locale.starting : locale.startButton}
                                    </Button>
                                  ) : (
                                    <Box aria-hidden="true" sx={{ width: 1, minHeight: 38 }} />
                                  )}
                                </Box>
                              </Stack>

                              <Box
                                sx={{
                                  minWidth: { xs: '100%', sm: 182 },
                                  display: 'flex',
                                  alignItems: { xs: 'stretch', sm: 'center' },
                                }}
                              >
                                <Paper
                                  variant="outlined"
                                  sx={{
                                    width: '100%',
                                    height: { xs: 'auto', sm: 126 },
                                    p: 1,
                                    borderRadius: 1.5,
                                    borderColor: '#d8c89f',
                                    backgroundColor: 'rgba(255,253,246,0.96)',
                                  }}
                                >
                                  <Stack spacing={0.75} sx={{ height: '100%', justifyContent: 'space-between' }}>
                                    <Typography variant="caption" sx={{ color: '#7d5f20', fontWeight: 700 }}>
                                      {gradeStars(map.grade)}
                                    </Typography>
                                    <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                                      <Chip size="small" label={`${formatDuration(map.durationSeconds)}`} />
                                      <Chip size="small" label={`${map.ownedQuantity}`} />
                                    </Stack>
                                    <Typography variant="caption" color="text.secondary">{locale.rewardTendency}</Typography>
                                    <Typography variant="body2" fontWeight={700}>{getDominantTendency(map)}</Typography>
                                  </Stack>
                                </Paper>
                              </Box>
                            </Stack>
                          </Box>
                        </Paper>
                      )
                    })}
                  </Stack>
                )}
              </Stack>
            </Paper>

            <Paper
              variant="outlined"
              sx={{
                p: { xs: 2, sm: 2.5 },
                borderRadius: 3,
                ...sectionPanelSx,
              }}
            >
              <Stack spacing={1}>
                <Typography variant="h6" fontWeight={900} sx={sectionHeadingSx}>{locale.historyTitle}</Typography>
                {historyExpeditions.length === 0 ? (
                  <Typography variant="body2" sx={{ color: sectionBodyColor }}>{locale.noHistory}</Typography>
                ) : (
                  historyExpeditions.map((expedition) => {
                    const historyMap = (maps ?? []).find((map) => map.mapId === expedition.mapId)
                    const historyItemNames = resolveRewardItemNames(expedition, historyMap ?? null)
                    const historyEquipmentNames = resolveRewardEquipmentNames(expedition, historyMap ?? null)
                    return (
                      <Paper key={expedition.expeditionId} variant="outlined" sx={{ p: 1.5, borderRadius: 2 }}>
                        <Stack spacing={0.75}>
                          <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                            <Typography variant="subtitle2" fontWeight={900}>{historyMap?.name ?? `mapId: ${expedition.mapId}`}</Typography>
                            <Chip size="small" label={formatStatus(expedition.status)} />
                          </Stack>
                          {expedition.reward ? (
                            <>
                              <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                                {expedition.reward.itemIds.length > 0 ? <Chip size="small" sx={{ backgroundColor: '#e6f5e6', color: '#245c2a' }} label={`${locale.candidateItems} x${expedition.reward.itemIds.length}`} /> : null}
                                {expedition.reward.equipmentIds.length > 0 ? <Chip size="small" sx={{ backgroundColor: '#e8efff', color: '#1e3f78' }} label={`${locale.candidateEquipments} x${expedition.reward.equipmentIds.length}`} /> : null}
                                {expedition.reward.experiencePoints > 0 ? <Chip size="small" sx={{ backgroundColor: '#fff0db', color: '#8a4e00' }} label={`${locale.candidateExperience} +${expedition.reward.experiencePoints}`} /> : null}
                                {expedition.reward.gold > 0 ? <Chip size="small" sx={{ backgroundColor: '#fff6cc', color: '#7a5c00' }} label={`${locale.candidateGold} +${expedition.reward.gold}`} /> : null}
                              </Stack>
                            </>
                          ) : null}
                        </Stack>
                      </Paper>
                    )
                  })
                )}
              </Stack>
            </Paper>
          </Stack>
        </Box>
      </Paper>
    </Container>
  )
}
