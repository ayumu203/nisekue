import { Alert, Box, Button, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useMemo, useState } from 'react'
import useSWR from 'swr'
import { claimTreasureMapReward, getCurrentTreasureMapExpedition, getTreasureMaps, startTreasureMapExpedition } from '@/api/treasureMap'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { innerSurfaceSx, mutedGreenButtonSx, outerPagePaperSx, softGreenButtonSx, twoColumnContentGridSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import locale from '../../locale/treasure-map/TreasureMap.json'
import type { TreasureMapExpedition, TreasureMapSummary } from '@/schema/treasureMap'

function formatSeconds(seconds: number): string {
  if (seconds <= 0) {
    return locale.ended
  }

  const minutes = Math.floor(seconds / 60)
  const remainSeconds = seconds % 60
  return `${minutes}:${String(remainSeconds).padStart(2, '0')}`
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

function rewardSummary(expedition: TreasureMapExpedition | null): string {
  if (!expedition?.reward) {
    return locale.noReward
  }

  const chunks: string[] = []
  if (expedition.reward.itemIds.length > 0) {
    chunks.push(`Item x${expedition.reward.itemIds.length}`)
  }

  if (expedition.reward.equipmentIds.length > 0) {
    chunks.push(`Equipment x${expedition.reward.equipmentIds.length}`)
  }

  if (expedition.reward.experiencePoints > 0) {
    chunks.push(`EXP +${expedition.reward.experiencePoints}`)
  }

  if (expedition.reward.gold > 0) {
    chunks.push(`Gold +${expedition.reward.gold}`)
  }

  return chunks.length > 0 ? chunks.join(' / ') : locale.noReward
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

export default function TreasureMap() {
  const { session, isLoading } = useAuth()
  const [actionMessage, setActionMessage] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [actionMapId, setActionMapId] = useState<number | null>(null)
  const [isClaiming, setIsClaiming] = useState(false)

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

  const remainSeconds = useMemo(() => {
    if (!currentExpedition || currentExpedition.status !== 'InProgress') {
      return 0
    }

    const diffMs = new Date(currentExpedition.endsAt).getTime() - Date.now()
    return Math.max(0, Math.floor(diffMs / 1000))
  }, [currentExpedition])

  async function handleStart(mapId: number): Promise<void> {
    if (!session?.access_token) {
      return
    }

    setActionMapId(mapId)
    setActionError(null)
    setActionMessage(null)

    try {
      await startTreasureMapExpedition({ mapId }, session.access_token)
      setActionMessage(locale.messages.started)
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
    setActionMessage(null)

    try {
      await claimTreasureMapReward(currentExpedition.expeditionId, session.access_token)
      setActionMessage(locale.messages.claimed)
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
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h4">{locale.title}</Typography>
            <HomeNavIconButton ariaLabel={locale.backToHome} />
          </Stack>

          {actionMessage ? <Alert severity="success">{actionMessage}</Alert> : null}
          {actionError ? <Alert severity="warning">{actionError}</Alert> : null}

          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={1.5}>
              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{locale.expeditionInfo}</Typography>
                  {isCurrentLoading ? (
                    <Stack direction="row" spacing={1} alignItems="center">
                      <CircularProgress size={16} />
                      <Typography variant="body2">{locale.currentLoading}</Typography>
                    </Stack>
                  ) : currentError ? (
                    <Alert severity="warning">{currentError.message}</Alert>
                  ) : !currentExpedition ? (
                    <Typography variant="body2">{locale.noCurrentExpedition}</Typography>
                  ) : (
                    <Stack spacing={1}>
                      <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap">
                        <Chip color="primary" label={formatStatus(currentExpedition.status)} />
                        <Typography variant="body2">mapId: {currentExpedition.mapId}</Typography>
                      </Stack>
                      <Typography variant="body2">
                        {locale.remainingTime}: {formatSeconds(remainSeconds)}
                      </Typography>
                      <Typography variant="body2">{locale.rewardPreview}: {rewardSummary(currentExpedition)}</Typography>
                      <Button
                        variant="contained"
                        sx={softGreenButtonSx}
                        disabled={
                          isClaiming
                          || currentExpedition.status === 'InProgress'
                          || currentExpedition.rewardClaimed
                        }
                        onClick={handleClaim}
                      >
                        {isClaiming ? locale.claiming : locale.claimButton}
                      </Button>
                    </Stack>
                  )}
                </Stack>
              </Paper>
            </Stack>

            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
              <Stack spacing={1.5}>
                {isMapsLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.mapsLoading}</Typography>
                  </Stack>
                ) : mapsError ? (
                  <Alert severity="warning">{mapsError.message}</Alert>
                ) : (
                  <Stack spacing={1.5}>
                    {(maps ?? []).map((map) => (
                      <Paper key={map.mapId} variant="outlined" sx={{ p: 1.5, borderRadius: 2 }}>
                        <Stack spacing={1}>
                          <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                            <Typography variant="subtitle1" fontWeight={700}>
                              [{map.grade}] {map.name}
                            </Typography>
                            <Chip size="small" label={`${locale.ownedQuantity}: ${map.ownedQuantity}`} />
                          </Stack>
                          <Typography variant="body2" color="text.secondary">
                            {map.description}
                          </Typography>
                          <Typography variant="caption" color="text.secondary">
                            {locale.duration}: {map.durationSeconds}{locale.seconds}
                          </Typography>
                          <Button
                            variant="outlined"
                            sx={mutedGreenButtonSx}
                            disabled={isStartDisabled(map, currentExpedition ?? null) || actionMapId === map.mapId}
                            onClick={() => {
                              void handleStart(map.mapId)
                            }}
                          >
                            {actionMapId === map.mapId ? locale.starting : locale.startButton}
                          </Button>
                        </Stack>
                      </Paper>
                    ))}
                  </Stack>
                )}
              </Stack>
            </Paper>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
