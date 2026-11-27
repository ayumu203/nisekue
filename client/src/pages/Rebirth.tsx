import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Divider,
  IconButton,
  Stack,
  Typography,
  useMediaQuery,
  type Theme,
} from '@mui/material'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, getRebirthHistory, rebirthPlayer, resetPlayerData } from '@/api/player'
import GamePageFrame from '@/components/common/GamePageFrame'
import GamePanel from '@/components/common/GamePanel'
import { HomeFilledIcon } from '@/components/home/HomeIcons'
import { RequirementMeter } from '@/components/rebirth/RequirementMeter'
import { SummaryStat } from '@/components/rebirth/SummaryStat'
import { rebirthPrimaryButtonSx, templeColumnSx, templeSectionTitleSx } from '@/components/rebirth/RebirthLayout'
import { rebirthPanelColors as colors } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-rebirth/Rebirth.json'
import { formatDateTime } from '@/components/quest/questFormat'
import { palette, withAlpha } from '@/theme/tokens'

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
  const isDesktop = useMediaQuery((theme: Theme) => theme.breakpoints.up('sm'), { noSsr: true })
  const isWide = useMediaQuery((theme: Theme) => theme.breakpoints.up('md'), { noSsr: true })
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

  // 全画面化した他の画面と同じく、上部に転生の色の帯、その下は地の色
  const pageBackground = `linear-gradient(${colors.topBand}, ${colors.topBand}) top / 100% ${isDesktop ? 300 : 200}px no-repeat, ${colors.ground}`
  const panelProps = { bandColor: colors.band, bandSelectedColor: colors.band }

  // スマホはヘッダーがないので、いちばん上の箱の帯の左端にホームへ戻るボタンを置く
  const mobileHomeButton = isDesktop ? undefined : (
    <IconButton
      component={Link}
      to="/"
      aria-label={locale.backToHome}
      sx={{ color: palette.white, width: 44, height: 44 }}
    >
      <HomeFilledIcon />
    </IconButton>
  )

  // データリセット（PC は左の列の下、狭い画面はいちばん下）
  const dataResetPanel = (
    <GamePanel title={locale.dataReset.title} fillHeight={false} {...panelProps}>
      <Stack spacing={1.5}>
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
    </GamePanel>
  )

  return (
    <GamePageFrame currentKey="rebirth" currentColor={colors.topBand} background={pageBackground} fillHeight>
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
        // PC（md 以上）：左に 転生・データリセット、右に ステータス予測・前世の記録。狭い画面は 1 列に積む
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gap: { xs: 1.5, sm: 3 },
            gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1fr) minmax(0, 1fr)' },
            alignContent: { xs: 'start', md: 'stretch' },
          }}
        >
          <Box sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column', gap: { xs: 1.5, sm: 3 } }}>
            <Box sx={{ flex: { md: 1 }, minHeight: 0 }}>
              <GamePanel title={locale.title} startAction={mobileHomeButton} fillHeight={isWide} {...panelProps}>
                <Box
                  sx={{
                    display: 'grid',
                    // 左の列に収まるよう、キャラを上に、条件と現在の職業を下に左右に並べる
                    gridTemplateColumns: { xs: '1fr', sm: 'minmax(0, 1fr) minmax(0, 1fr)' },
                    gridTemplateAreas: {
                      xs: '"requirement" "character" "job"',
                      sm: '"character character" "requirement job"',
                    },
                    gap: { xs: 2, lg: 2.5 },
                    alignItems: 'stretch',
                  }}
                >
                  <Stack spacing={1.5} sx={{ ...templeColumnSx, gridArea: 'requirement' }}>
                    <Box
                      sx={{
                        height: 12,
                        borderRadius: 999,
                        background: `linear-gradient(90deg, ${withAlpha(palette.brown200, 0.25)}, ${withAlpha(palette.gold300, 0.9)}, ${withAlpha(palette.brown200, 0.25)})`,
                      }}
                    />
                    <Typography variant="h6" sx={templeSectionTitleSx}>
                      {locale.requirementTitle}
                    </Typography>
                    <Stack spacing={1}>
                      <RequirementMeter current={player.level} required={requiredLevel} prefix="Lv." tone="blue" />
                      <RequirementMeter current={player.gold} required={requiredGold} suffix="Gold" tone="gold" />
                    </Stack>
                    <Box sx={{ flexGrow: 1 }} />
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

                  <Stack
                    spacing={2}
                    sx={{
                      ...templeColumnSx,
                      gridArea: 'character',
                      minWidth: 0,
                      justifyContent: 'center',
                      textAlign: 'center',
                      position: 'relative',
                      overflow: 'hidden',
                      background: `linear-gradient(180deg, ${palette.white} 0%, ${palette.sky} 45%, ${palette.cream} 100%)`,
                    }}
                  >
                    <Box
                      sx={{
                        position: 'absolute',
                        inset: 0,
                        background: `linear-gradient(90deg, ${withAlpha(palette.brown200, 0.75)} 0, ${withAlpha(palette.brown200, 0.75)} 8%, transparent 8%, transparent 92%, ${withAlpha(palette.brown200, 0.75)} 92%, ${withAlpha(palette.brown200, 0.75)} 100%)`,
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
                        background: `linear-gradient(90deg, ${withAlpha(palette.white, 0.5)}, ${palette.gold300}, ${withAlpha(palette.white, 0.5)})`,
                        pointerEvents: 'none',
                      }}
                    />
                    <Stack spacing={1} alignItems="center" sx={{ position: 'relative' }}>
                      <Typography variant="h4" fontWeight={900} color={palette.gold700}>
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
                        borderColor: palette.brown200,
                        background: `radial-gradient(circle at 50% 12%, ${palette.gold300} 0%, ${withAlpha(palette.cream, 0.75)} 22%, ${palette.sky} 48%, ${palette.white} 72%)`,
                        overflow: 'hidden',
                        boxShadow: `0 20px 44px ${withAlpha(palette.gold300, 0.25)}`,
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
                        background: `linear-gradient(90deg, ${withAlpha(palette.gold300, 0.25)}, ${palette.gold300}, ${withAlpha(palette.gold300, 0.25)})`,
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
                        background: `linear-gradient(90deg, ${withAlpha(palette.brown200, 0.25)}, ${withAlpha(palette.blue300, 0.9)}, ${withAlpha(palette.brown200, 0.25)})`,
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
                                  bgcolor: palette.cream,
                                  border: `1px solid ${palette.gold300}`,
                                  color: palette.gold700,
                                }}
                              />
                            ))}
                          </Stack>
                        </Stack>
                      ) : null}
                    </Stack>
                  </Stack>
                </Box>
              </GamePanel>
            </Box>
            {submitError ? <Alert severity="error">{submitError}</Alert> : null}
            {isWide ? dataResetPanel : null}
          </Box>
          <Box sx={{ minWidth: 0, minHeight: 0, display: 'flex', flexDirection: 'column', gap: { xs: 1.5, sm: 3 } }}>
            <GamePanel title={locale.statusPreviewTitle} fillHeight={false} {...panelProps}>
              <Stack spacing={1.5}>
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

                    <Divider sx={{ borderColor: palette.brown200 }} />

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
                              py: 1.5,
                              alignItems: 'center',
                            }}
                          >
                            <Typography variant="body2" fontWeight={700} color={palette.gold700}>
                              {stat.label}
                            </Typography>
                            <Typography variant="body2">{currentValue}</Typography>
                            <Typography variant="body2" color="text.secondary">
                              {formatInheritedMedian(currentValue, minimumValue)}
                            </Typography>
                            <Typography
                              variant="body2"
                              fontWeight={resultStatus ? 700 : 500}
                              color={resultStatus ? palette.gold700 : 'text.secondary'}
                            >
                              {actualValue ?? ''}
                            </Typography>
                          </Box>
                          <Divider sx={{ borderColor: palette.brown200 }} />
                        </Box>
                      )
                    })}
                  </Box>
                </Box>
              </Stack>
            </GamePanel>
            <Box sx={{ flex: { md: 1 }, minHeight: 0 }}>
              <GamePanel title={locale.previousLifeTitle} fillHeight={isWide} fitContent {...panelProps}>
                <Stack spacing={1.5}>
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
                          sx={{ border: `1px solid ${palette.brown200}`, borderRadius: 2, p: { xs: 1.5, sm: 2 } }}
                        >
                          <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'baseline', columnGap: 1, mb: 1 }}>
                            <Typography variant="subtitle2" fontWeight={700} color={palette.gold700}>
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
                              <Divider sx={{ borderColor: palette.brown200 }} />
                              {statKeys.map((stat) => {
                                const pastValue = entry.status[stat.key]
                                const currentValue = player.baseStatus[stat.key]
                                const diff = currentValue - pastValue
                                const diffColor =
                                  diff > 0 ? palette.green700 : diff < 0 ? palette.red700 : 'text.secondary'
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
                                      <Typography variant="body2" fontWeight={700} color={palette.gold700}>
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
                                    <Divider sx={{ borderColor: palette.brown200 }} />
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
              </GamePanel>
            </Box>
            {isWide ? null : dataResetPanel}
          </Box>
        </Box>
      )}
    </GamePageFrame>
  )
}
