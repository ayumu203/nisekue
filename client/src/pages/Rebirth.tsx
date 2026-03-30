import { Alert, Box, Button, Chip, CircularProgress, Container, Divider, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { createPlayer, getPlayer, rebirthPlayer } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-rebirth/Rebirth.json'

const REQUIRED_LEVEL = 100
const REQUIRED_GOLD = 100000
const rebirthPagePaperSx = {
  background:
    'linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(250,252,255,0.98) 38%, rgba(255,248,220,0.98) 100%)',
  border: '1px solid',
  borderColor: '#efe1a6',
  borderRadius: { xs: 3, sm: 4 },
  boxShadow: '0 20px 60px rgba(224, 196, 94, 0.18)',
  p: { xs: 2, sm: 4 },
} as const

const rebirthSurfaceSx = {
  background:
    'linear-gradient(180deg, rgba(255,255,255,0.99) 0%, rgba(247,251,255,0.97) 56%, rgba(255,251,235,0.97) 100%)',
  borderColor: '#eadb9f',
  boxShadow: '0 8px 24px rgba(214, 186, 92, 0.12)',
} as const

const rebirthPrimaryButtonSx = {
  '&&': {
    background: 'linear-gradient(135deg, #6ec8ff 0%, #7fb3ff 46%, #f5d96b 100%)',
    color: '#ffffff',
    border: '1px solid #cfe6ff',
    boxShadow: '0 10px 24px rgba(113, 176, 237, 0.28)',
  },
  '&&:hover': {
    background: 'linear-gradient(135deg, #5dbcf7 0%, #729ff0 46%, #efcd52 100%)',
    boxShadow: '0 12px 28px rgba(113, 176, 237, 0.34)',
  },
  '&&.Mui-disabled': {
    background: 'linear-gradient(135deg, #d7eaf8 0%, #dae6f6 55%, #f4ebc2 100%)',
    color: '#8ca3b9',
    border: '1px solid #d9e8f6',
  },
} as const

const rebirthHintBadgeSx = {
  px: 1.5,
  py: 0.75,
  borderRadius: 999,
  border: '1px solid #eadb9f',
  background: 'linear-gradient(135deg, rgba(255, 234, 151, 0.3), rgba(150, 218, 255, 0.18))',
} as const

const rebirthHintTextSx = {
  color: '#8a6a00',
  fontWeight: 700,
} as const

const templeColumnSx = {
  position: 'relative',
  borderRadius: 3,
  border: '1px solid #e8dcab',
  background:
    'linear-gradient(180deg, rgba(255,255,255,0.98) 0%, rgba(248,251,255,0.95) 40%, rgba(255,249,230,0.94) 100%)',
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.9), 0 10px 24px rgba(200, 179, 92, 0.08)',
  px: { xs: 1.5, sm: 2 },
  py: { xs: 2, sm: 2.5 },
} as const

const templeSectionTitleSx = {
  color: '#8a6a00',
  letterSpacing: '0.08em',
} as const

const templeBodyTextSx = {
  color: 'text.secondary',
  fontSize: { xs: '0.875rem', md: '0.8125rem' },
  lineHeight: 1.55,
  whiteSpace: 'normal',
  textWrap: 'pretty',
  overflowWrap: 'anywhere',
} as const

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

function SummaryStat({ label, value }: { label: string; value: number | string }) {
  return (
    <Box
      sx={{
        px: 1.5,
        py: 1.25,
        border: '1px solid #eadb9f',
        borderRadius: 2,
        bgcolor: 'rgba(255, 250, 230, 0.88)',
      }}
    >
      <Typography variant="caption" color="text.secondary">
        {label}
      </Typography>
      <Typography variant="h6" fontWeight={800} color="#8a6a00">
        {value}
      </Typography>
    </Box>
  )
}

export default function Rebirth() {
  const { session, isLoading } = useAuth()
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
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

  const canRebirth = (player?.level ?? 0) >= REQUIRED_LEVEL && (player?.gold ?? 0) >= REQUIRED_GOLD
  const characterImageSrc =
    player?.imagePath && player.imagePath !== failedImagePath ? resolveCharacterAssetPath(player.imagePath) : null

  async function handleRebirth(): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionMissing)
      return
    }

    setSubmitError(null)
    setSuccessMessage(null)
    setResultStatus(null)
    setIsSubmitting(true)

    try {
      const response = await rebirthPlayer(session.access_token)
      setResultStatus(response.status.baseValues)
      setSuccessMessage(locale.executed)
      await mutatePlayer()
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.executeFailed)
    } finally {
      setIsSubmitting(false)
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

          <Stack spacing={1}>
            <Typography variant="h4" textAlign="center">
              <Box component="span" sx={{ color: '#8a6a00' }}>
                {locale.title}
              </Box>
            </Typography>
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
                    <Stack spacing={1} flexWrap="wrap" useFlexGap>
                      <Chip
                        color={player.level >= REQUIRED_LEVEL ? 'success' : 'default'}
                        label={
                          player.level >= REQUIRED_LEVEL
                            ? locale.requirementLevelMet.replace('{{level}}', String(player.level))
                            : locale.requirementLevelNotMet.replace('{{level}}', String(player.level))
                        }
                        sx={{
                          bgcolor: player.level >= REQUIRED_LEVEL ? '#edf9ff' : '#f6f9fc',
                          border: '1px solid',
                          borderColor: player.level >= REQUIRED_LEVEL ? '#a8ddff' : '#d8e6f2',
                          color: player.level >= REQUIRED_LEVEL ? '#20618f' : '#5f7080',
                        }}
                      />
                      <Chip
                        color={player.gold >= REQUIRED_GOLD ? 'success' : 'default'}
                        label={
                          player.gold >= REQUIRED_GOLD
                            ? locale.requirementGoldMet.replace('{{gold}}', String(player.gold))
                            : locale.requirementGoldNotMet.replace('{{gold}}', String(player.gold))
                        }
                        sx={{
                          bgcolor: player.gold >= REQUIRED_GOLD ? '#fffbe9' : '#f6f9fc',
                          border: '1px solid',
                          borderColor: player.gold >= REQUIRED_GOLD ? '#f0db7f' : '#d8e6f2',
                          color: player.gold >= REQUIRED_GOLD ? '#8a6a00' : '#5f7080',
                        }}
                      />
                    </Stack>
                    <Divider sx={{ borderColor: '#efe1a6' }} />
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.inheritMasteredJobs}
                    </Typography>
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.inheritMoves}
                    </Typography>
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.inheritStatus}
                    </Typography>
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
                    <Typography variant="h6" sx={templeSectionTitleSx}>
                      {locale.resetTitle}
                    </Typography>
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.resetLevel}
                    </Typography>
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.resetJobLevel}
                    </Typography>
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.resetExp}
                    </Typography>
                    <Typography variant="body2" sx={templeBodyTextSx}>
                      {locale.resetGold}
                    </Typography>

                    <Divider sx={{ borderColor: '#efe1a6' }} />

                    <Box
                      sx={{
                        p: 1.5,
                        borderRadius: 2,
                        border: '1px solid #efe1a6',
                        background: canRebirth
                          ? 'linear-gradient(135deg, rgba(255, 249, 213, 0.96), rgba(232, 247, 255, 0.92))'
                          : 'linear-gradient(135deg, rgba(255, 252, 240, 0.96), rgba(248, 252, 255, 0.92))',
                      }}
                    >
                      <Stack spacing={1.25}>
                        <Box sx={canRebirth ? rebirthHintBadgeSx : undefined}>
                          <Typography variant="body2" sx={canRebirth ? rebirthHintTextSx : { color: 'text.secondary' }}>
                            {canRebirth ? locale.executeHintReady : locale.executeHintLocked}
                          </Typography>
                        </Box>
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
                  </Stack>
                </Box>
              </Paper>

              {successMessage ? <Alert severity="success">{successMessage}</Alert> : null}
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
                                {actualValue ?? locale.statusTablePending}
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
            </Stack>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
