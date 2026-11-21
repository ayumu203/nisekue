import { Alert, Box, Button, Chip, CircularProgress, Container, Divider, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { createPlayer, getPlayer, getRebirthHistory, rebirthPlayer, resetPlayerData } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { RequirementMeter } from '@/components/rebirth/RequirementMeter'
import { SummaryStat } from '@/components/rebirth/SummaryStat'
import {
  rebirthPagePaperSx,
  rebirthPrimaryButtonSx,
  rebirthSurfaceSx,
  templeColumnSx,
  templeSectionTitleSx,
} from '@/components/rebirth/RebirthLayout'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-rebirth/Rebirth.json'
import { formatDateTime } from '@/components/quest/questFormat'

const DEFAULT_REQUIRED_LEVEL = 100
const DEFAULT_REQUIRED_GOLD = 100000
const DEFAULT_DATA_RESET_GOLD = 100000

const statKeys = [
  { key: 'maxHp', label: locale.labels.maxHp },
  { key: 'maxMp', label: locale.labels.maxMp },
  { key: 'strength', label: locale.labels.strength },
  { key: 'defense', label: locale.labels.defense },
  { key: 'intelligence', label: locale.labels.intelligence },
  { key: 'luck', label: locale.labels.luck },
  { key: 'speed', label: locale.labels.speed },
] as const

function formatInheritedMedian(value: number, minValue: number): string {
  return String(Math.max(minValue, Math.floor((value * 30) / 100)))
}

export default function Rebirth() {
  const { session, isLoading } = useAuth()
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [resultStatus, setResultStatus] = useState<{
    maxHp: number
    maxMp: number
    strength: number
    defense: number
    intelligence: number
    luck: number
    speed: number
  } | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [dataResetError, setDataResetError] = useState<string | null>(null)
  const [dataResetMessage, setDataResetMessage] = useState<string | null>(null)
  const [isResettingData, setIsResettingData] = useState(false)

  const playerSWRKey = session?.user.id ? (['rebirth', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
    mutate: mutatePlayer,
  } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
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

  const rebirthHistorySWRKey = session?.user.id ? (['rebirth-history', session.user.id] as const) : null
  const {
    data: rebirthHistory,
    error: rebirthHistoryError,
    isLoading: isRebirthHistoryLoading,
    mutate: mutateRebirthHistory,
  } = useSWR(rebirthHistorySWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    return getRebirthHistory(session.access_token)
  })

  const requiredLevel = player?.rebirthRequiredLevel ?? DEFAULT_REQUIRED_LEVEL
  const requiredGold = player?.rebirthGoldCost ?? DEFAULT_REQUIRED_GOLD
  const dataResetGold = player?.dataResetGoldCost ?? DEFAULT_DATA_RESET_GOLD
  const canRebirth = (player?.level ?? 0) >= requiredLevel && (player?.gold ?? 0) >= requiredGold
  const canResetData = (player?.gold ?? 0) >= dataResetGold
  const characterImageSrc =
    player?.imagePath && player.imagePath !== failedImagePath ? resolveCharacterAssetPath(player.imagePath) : null

  async function handleRebirth(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionMissing)
      return
    }

    setSubmitError(null)
    setResultStatus(null)
    setIsSubmitting(true)

    try {
      const response = await rebirthPlayer(session.access_token)
      setResultStatus(response.status.baseValues)
      await mutatePlayer()
      await mutateRebirthHistory()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.executeFailed)
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleDataReset(): Promise<void> {
    if (!session?.access_token) {
      setDataResetError(locale.sessionMissing)
      return
    }

    if (!window.confirm(locale.dataReset.confirm.replace('{{gold}}', String(dataResetGold)))) {
      return
    }

    setDataResetError(null)
    setDataResetMessage(null)
    setResultStatus(null)
    setIsResettingData(true)

    try {
      await resetPlayerData(session.access_token)
      setDataResetMessage(locale.dataReset.done)
      await mutatePlayer()
    } catch (error) {
      setDataResetError(error instanceof Error ? error.message : locale.dataReset.failed)
    } finally {
      setIsResettingData(false)
    }
  }

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.loadingAuth}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={0} sx={rebirthPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.backToHome} />
          </Stack>

          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.loadingPlayer}</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : !player ? (
            <Alert severity="warning">{locale.loadingPlayer}</Alert>
          ) : (
            <Stack spacing={2}>
              <Paper variant="outlined" sx={{ ...rebirthSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={0.5} sx={{ mb: 2 }}>
                  <Typography
                    variant="overline"
                    textAlign="center"
                    sx={{ letterSpacing: '0.16em', color: 'rgba(138, 106, 0, 0.72)', lineHeight: 1.6 }}
                  >
                    REBIRTH
                  </Typography>
                  <Typography variant="h4" textAlign="center" sx={{ color: '#8a6a00' }}>
                    {locale.title}
                  </Typography>
                </Stack>
                <Box
                  sx={{
                    display: 'grid',
                    gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 0.9fr) minmax(280px, 0.95fr) minmax(0, 0.9fr)' },
                    gap: { xs: 2, lg: 2.5 },
                    alignItems: 'stretch',
                  }}
                >
                  <Stack spacing={1.5} sx={templeColumnSx}>
                    <Box
                      sx={{
                        height: 12,
                        borderRadius: 999,
                        background:
                          'linear-gradient(90deg, rgba(227,216,165,0.3), rgba(255,232,137,0.9), rgba(227,216,165,0.3))',
                      }}
                    />
                    <Typography variant="h6" sx={templeSectionTitleSx}>
                      {locale.requirementTitle}
                    </Typography>
                    <Stack spacing={1}>
                      <RequirementMeter current={player.level} required={requiredLevel} prefix="Lv." tone="blue" />
                      <RequirementMeter current={player.gold} required={requiredGold} suffix="Gold" tone="gold" />
                    </Stack>
                  </Stack>

                  <Stack
                    spacing={1.75}
                    sx={{
                      ...templeColumnSx,
                      minWidth: 0,
                      justifyContent: 'center',
                      textAlign: 'center',
                      position: 'relative',
                      overflow: 'hidden',
                      background:
                        'linear-gradient(180deg, rgba(255,255,255,0.99) 0%, rgba(244,250,255,0.98) 45%, rgba(255,249,229,0.96) 100%)',
                    }}
                  >
                    <Box
                      sx={{
                        position: 'absolute',
                        inset: 0,
                        background:
                          'linear-gradient(90deg, rgba(233,224,180,0.75) 0, rgba(233,224,180,0.75) 8%, transparent 8%, transparent 92%, rgba(233,224,180,0.75) 92%, rgba(233,224,180,0.75) 100%)',
                        opacity: 0.55,
                        pointerEvents: 'none',
                      }}
                    />
                    <Box
                      sx={{
                        position: 'absolute',
                        top: 18,
                        left: '10%',
                        right: '10%',
                        height: 10,
                        borderRadius: 999,
                        background:
                          'linear-gradient(90deg, rgba(255,255,255,0.55), rgba(247,219,105,0.95), rgba(255,255,255,0.55))',
                        pointerEvents: 'none',
                      }}
                    />
                    <Stack spacing={1} alignItems="center" sx={{ position: 'relative' }}>
                      <Typography variant="h4" fontWeight={900} color="#8a6a00">
                        {player.userName}
                      </Typography>
                    </Stack>

                    <Box
                      sx={{
                        width: '100%',
                        maxWidth: 270,
                        aspectRatio: '338 / 350',
                        alignSelf: 'center',
                        borderRadius: '999px 999px 22px 22px',
                        border: '1px solid',
                        borderColor: '#efe1a6',
                        background:
                          'radial-gradient(circle at 50% 12%, rgba(255, 226, 111, 0.95) 0%, rgba(255, 243, 191, 0.78) 22%, rgba(232, 246, 255, 0.96) 48%, rgba(255,255,255,0.99) 72%)',
                        overflow: 'hidden',
                        boxShadow: '0 20px 44px rgba(225, 193, 83, 0.2)',
                        position: 'relative',
                      }}
                    >
                      {characterImageSrc ? (
                        <Box
                          component="img"
                          src={characterImageSrc}
                          alt={player.userName ? `${player.userName} character` : 'player character'}
                          onError={() => setFailedImagePath(player.imagePath ?? null)}
                          sx={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'contain',
                            objectPosition: 'center bottom',
                            display: 'block',
                            position: 'relative',
                            zIndex: 1,
                          }}
                        />
                      ) : (
                        <Box sx={{ width: '100%', height: '100%', display: 'grid', placeItems: 'center' }}>
                          <Typography variant="body2" color="text.secondary">
                            {player.userName}
                          </Typography>
                        </Box>
                      )}
                    </Box>

                    <Box
                      sx={{
                        width: '100%',
                        maxWidth: 320,
                        height: 14,
                        borderRadius: 999,
                        background:
                          'linear-gradient(90deg, rgba(225,211,150,0.35), rgba(247,219,105,0.98), rgba(225,211,150,0.35))',
                        alignSelf: 'center',
                        position: 'relative',
                      }}
                    />

                    <Box
                      sx={{
                        display: 'grid',
                        gridTemplateColumns: 'repeat(3, minmax(0, 1fr))',
                        gap: 1,
                        position: 'relative',
                      }}
                    >
                      <SummaryStat label={locale.labels.level} value={player.level} />
                      <SummaryStat label={locale.labels.jobLevel} value={player.jobLevel} />
                      <SummaryStat label={locale.labels.gold} value={player.gold} />
                    </Box>
                  </Stack>

                  <Stack spacing={1.5} sx={templeColumnSx}>
                    <Box
                      sx={{
                        height: 12,
                        borderRadius: 999,
                        background:
                          'linear-gradient(90deg, rgba(227,216,165,0.3), rgba(145,213,255,0.92), rgba(227,216,165,0.3))',
                      }}
                    />
                    <Stack spacing={1} alignItems="center" sx={{ pt: 0.5 }}>
                      <Typography variant="body2" color="text.secondary">
                        {locale.currentJobLabel}: {player.job.displayName}
                      </Typography>
                      {player.masteredJobs.length > 0 ? (
                        <Stack spacing={0.75} alignItems="center" sx={{ width: '100%', maxWidth: 360 }}>
                          <Typography variant="caption" color="text.secondary">
                            {locale.masteredJobsTitle}
                          </Typography>
                          <Stack direction="row" spacing={0.75} useFlexGap flexWrap="wrap" justifyContent="center">
                            {player.masteredJobs.map((job) => (
                              <Chip
                                key={job.code}
                                label={job.displayName}
                                size="small"
                                sx={{
                                  bgcolor: '#fff9e3',
                                  border: '1px solid #eedc90',
                                  color: '#8a6a00',
                                }}
                              />
                            ))}
                          </Stack>
                        </Stack>
                      ) : null}
                    </Stack>
                    <Button
                      variant="contained"
                      disabled={!canRebirth || isSubmitting}
                      onClick={() => {
                        void handleRebirth()
                      }}
                      sx={rebirthPrimaryButtonSx}
                    >
                      {isSubmitting ? locale.executing : canRebirth ? locale.executeButton : locale.lockedButton}
                    </Button>
                  </Stack>
                </Box>
              </Paper>

              {submitError ? <Alert severity="error">{submitError}</Alert> : null}

              <Paper variant="outlined" sx={{ ...rebirthSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{locale.statusPreviewTitle}</Typography>
                  <Box sx={{ overflowX: 'auto' }}>
                    <Box
                      sx={{
                        minWidth: 520,
                      }}
                    >
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: '1.1fr 0.9fr 1fr 0.9fr',
                          gap: 2,
                          px: 1,
                          pb: 1,
                        }}
                      >
                        <Typography variant="caption" color="text.secondary">
                          {locale.statusTableStat}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {locale.statusTableCurrent}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {locale.statusTableRange}
                        </Typography>
                        <Typography variant="caption" color="text.secondary">
                          {locale.statusTableActual}
                        </Typography>
                      </Box>

                      <Divider sx={{ borderColor: '#efe1a6' }} />

                      {statKeys.map((stat) => {
                        const currentValue = player.baseStatus[stat.key]
                        const actualValue = resultStatus?.[stat.key]
                        const minimumValue = stat.key === 'maxHp' ? 1 : 0

                        return (
                          <Box key={stat.key}>
                            <Box
                              sx={{
                                display: 'grid',
                                gridTemplateColumns: '1.1fr 0.9fr 1fr 0.9fr',
                                gap: 2,
                                px: 1,
                                py: 1.25,
                                alignItems: 'center',
                              }}
                            >
                              <Typography variant="body2" fontWeight={700} color="#8a6a00">
                                {stat.label}
                              </Typography>
                              <Typography variant="body2">{currentValue}</Typography>
                              <Typography variant="body2" color="text.secondary">
                                {formatInheritedMedian(currentValue, minimumValue)}
                              </Typography>
                              <Typography
                                variant="body2"
                                fontWeight={resultStatus ? 700 : 500}
                                color={resultStatus ? '#8a6a00' : 'text.secondary'}
                              >
                                {actualValue ?? ''}
                              </Typography>
                            </Box>
                            <Divider sx={{ borderColor: '#f2e8bc' }} />
                          </Box>
                        )
                      })}
                    </Box>
                  </Box>
                </Stack>
              </Paper>

              <Paper variant="outlined" sx={{ ...rebirthSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{locale.previousLifeTitle}</Typography>
                  {isRebirthHistoryLoading ? (
                    <Stack direction="row" spacing={1} alignItems="center">
                      <CircularProgress size={16} />
                      <Typography variant="body2">{locale.previousLifeLoading}</Typography>
                    </Stack>
                  ) : rebirthHistoryError ? (
                    <Alert severity="warning">{locale.previousLifeError}</Alert>
                  ) : !rebirthHistory || rebirthHistory.length === 0 ? (
                    <Typography variant="body2" color="text.secondary">
                      {locale.previousLifeEmpty}
                    </Typography>
                  ) : (
                    <Stack spacing={2}>
                      {[...rebirthHistory].reverse().map((entry) => (
                        <Box
                          key={entry.rebirthCount}
                          sx={{ border: '1px solid #efe1a6', borderRadius: 2, p: { xs: 1.5, sm: 2 } }}
                        >
                          <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'baseline', columnGap: 1, mb: 1 }}>
                            <Typography variant="subtitle2" fontWeight={700} color="#8a6a00">
                              {locale.previousLifeGeneration.replace('{{count}}', String(entry.rebirthCount))}
                            </Typography>
                            <Typography variant="caption" color="text.secondary">
                              {formatDateTime(entry.rebirthedAt)}
                            </Typography>
                          </Box>
                          <Box sx={{ overflowX: 'auto' }}>
                            <Box sx={{ minWidth: 480 }}>
                              <Box
                                sx={{
                                  display: 'grid',
                                  gridTemplateColumns: '1.1fr 0.9fr 0.9fr 0.9fr',
                                  gap: 2,
                                  px: 1,
                                  pb: 1,
                                }}
                              >
                                <Typography variant="caption" color="text.secondary">
                                  {locale.previousLifeStatLabel}
                                </Typography>
                                <Typography variant="caption" color="text.secondary">
                                  {locale.previousLifePastValue}
                                </Typography>
                                <Typography variant="caption" color="text.secondary">
                                  {locale.previousLifeCurrentValue}
                                </Typography>
                                <Typography variant="caption" color="text.secondary">
                                  {locale.previousLifeDiff}
                                </Typography>
                              </Box>
                              <Divider sx={{ borderColor: '#efe1a6' }} />
                              {statKeys.map((stat) => {
                                const pastValue = entry.status[stat.key]
                                const currentValue = player.baseStatus[stat.key]
                                const diff = currentValue - pastValue
                                const diffColor = diff > 0 ? '#1b7f4b' : diff < 0 ? '#b3261e' : 'text.secondary'
                                const diffLabel = diff > 0 ? `+${diff}` : String(diff)

                                return (
                                  <Box key={stat.key}>
                                    <Box
                                      sx={{
                                        display: 'grid',
                                        gridTemplateColumns: '1.1fr 0.9fr 0.9fr 0.9fr',
                                        gap: 2,
                                        px: 1,
                                        py: 1,
                                        alignItems: 'center',
                                      }}
                                    >
                                      <Typography variant="body2" fontWeight={700} color="#8a6a00">
                                        {stat.label}
                                      </Typography>
                                      <Typography variant="body2" color="text.secondary">
                                        {pastValue}
                                      </Typography>
                                      <Typography variant="body2">{currentValue}</Typography>
                                      <Typography variant="body2" fontWeight={700} sx={{ color: diffColor }}>
                                        {diffLabel}
                                      </Typography>
                                    </Box>
                                    <Divider sx={{ borderColor: '#f2e8bc' }} />
                                  </Box>
                                )
                              })}
                            </Box>
                          </Box>
                        </Box>
                      ))}
                    </Stack>
                  )}
                </Stack>
              </Paper>

              <Paper variant="outlined" sx={{ ...rebirthSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{locale.dataReset.title}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {locale.dataReset.description.replace('{{gold}}', String(dataResetGold))}
                  </Typography>
                  {dataResetError ? <Alert severity="error">{dataResetError}</Alert> : null}
                  {dataResetMessage ? <Alert severity="success">{dataResetMessage}</Alert> : null}
                  <Box>
                    <Button
                      variant="outlined"
                      color="error"
                      disabled={!canResetData || isResettingData}
                      onClick={() => {
                        void handleDataReset()
                      }}
                    >
                      {isResettingData
                        ? locale.dataReset.executing
                        : canResetData
                          ? locale.dataReset.button
                          : locale.dataReset.lockedButton}
                    </Button>
                  </Box>
                </Stack>
              </Paper>
            </Stack>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
