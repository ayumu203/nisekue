import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  LinearProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayerJob } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { innerSurfaceSx, outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolveJobAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-job/JobChange.json'

function formatExpProgress(currentExp: number, level: number): { current: number; required: number; ratio: number } {
  const required = Math.max(1, level * 10)
  const current = Math.max(0, currentExp)
  return {
    current,
    required,
    ratio: Math.max(0, Math.min(100, (current / required) * 100)),
  }
}

export default function JobChange() {
  const { session, isLoading } = useAuth()
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [learnedMoveNames, setLearnedMoveNames] = useState<string[]>([])
  const [isSubmittingJobValue, setIsSubmittingJobValue] = useState<number | null>(null)

  const playerSWRKey = session?.user.id ? (['job-change', session.user.id] as const) : null
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
  const currentJobImageSrc = resolveJobAssetPath(player?.job.code)
  const currentJobs = (player?.jobProfiles ?? []).filter((job) => job.code !== 'Apprentice')
  const unlockThreshold = 5
  const jobExpProgress = formatExpProgress(player?.jobExp ?? 0, player?.jobLevel ?? 1)

  async function handleChangeJob(nextJobValue: number): Promise<void> {
    if (!session?.access_token) {
      setSubmitError(locale.sessionMissing)
      return
    }

    setSubmitError(null)
    setSuccessMessage(null)
    setLearnedMoveNames([])
    setIsSubmittingJobValue(nextJobValue)

    try {
      const response = await updatePlayerJob({ job: nextJobValue }, session.access_token)
      await mutatePlayer()
      setLearnedMoveNames(response.newlyLearnedMoves.map((move) => move.moveName))
      setSuccessMessage(
        response.newlyLearnedMoves.length > 0
          ? locale.changedWithMoves.replace('{{jobName}}', response.job.displayName)
          : locale.changed.replace('{{jobName}}', response.job.displayName),
      )
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.changeFailed)
    } finally {
      setIsSubmittingJobValue(null)
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
      <Paper elevation={2} sx={outerPagePaperSx}>
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
              <Paper
                variant="outlined"
                sx={{
                  ...innerSurfaceSx,
                  borderRadius: 3,
                  p: { xs: 2, sm: 2.5 },
                  backgroundColor: '#44644a',
                  borderColor: '#b8ab7a',
                }}
              >
                <Stack spacing={2}>
                  <Stack
                    direction={{ xs: 'column', sm: 'row' }}
                    spacing={2}
                    alignItems={{ xs: 'stretch', sm: 'center' }}
                    justifyContent="space-between"
                  >
                    <Stack spacing={1}>
                      <Chip
                        label={locale.currentBadge}
                        sx={{
                          fontWeight: 700,
                          alignSelf: 'flex-start',
                          bgcolor: 'rgba(255, 249, 232, 0.92)',
                          color: '#35513a',
                          border: '1px solid #cbb783',
                        }}
                      />
                      <Stack spacing={0.75}>
                        <Typography variant="h4" fontWeight={900} lineHeight={1.1} color="#fff8ea">
                          {player.job.displayName}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.84)' }}>
                          {player.job.description}
                        </Typography>
                      </Stack>
                    </Stack>
                  </Stack>

                  <Box
                    sx={{
                      display: 'grid',
                      gridTemplateColumns: { xs: '1fr', sm: '220px minmax(0, 1fr)' },
                      gap: 2,
                      alignItems: 'center',
                    }}
                  >
                    {currentJobImageSrc ? (
                      <Box
                        sx={{
                          width: '100%',
                          maxWidth: 240,
                          aspectRatio: '1 / 1',
                          justifySelf: { sm: 'start' },
                          borderRadius: 3,
                          border: '2px solid',
                          borderColor: '#bda86f',
                          bgcolor: '#fff8ea',
                          boxShadow: '0 10px 24px rgba(45, 61, 39, 0.12)',
                          overflow: 'hidden',
                          display: 'grid',
                          placeItems: 'center',
                          p: 1.5,
                          backgroundImage:
                            'radial-gradient(circle at 50% 35%, rgba(232, 242, 221, 0.95), rgba(255, 248, 234, 0.88) 58%, rgba(219, 231, 199, 0.92))',
                        }}
                      >
                        <Box
                          component="img"
                          src={currentJobImageSrc}
                          alt={player.job.displayName}
                          sx={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'contain',
                            display: 'block',
                            filter: 'drop-shadow(0 10px 14px rgba(91, 63, 16, 0.18))',
                            transform: 'scale(1.04)',
                          }}
                        />
                      </Box>
                    ) : null}

                    <Stack spacing={1.25}>
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
                          gap: 1,
                        }}
                      >
                        <Paper
                          variant="outlined"
                          sx={{ borderRadius: 2, p: 1.5, borderColor: '#d2c08b', bgcolor: 'rgba(255, 249, 232, 0.92)' }}
                        >
                          <Typography variant="caption" color="text.secondary">
                            {locale.currentLevel.replace('{{level}}', '')}
                          </Typography>
                          <Typography variant="h6" fontWeight={800} color="#324c36">
                            Lv.{player.level}
                          </Typography>
                        </Paper>
                        <Paper
                          variant="outlined"
                          sx={{ borderRadius: 2, p: 1.5, borderColor: '#d2c08b', bgcolor: 'rgba(255, 249, 232, 0.92)' }}
                        >
                          <Typography variant="caption" color="text.secondary">
                            {locale.currentJobLevel.replace('{{level}}', '')}
                          </Typography>
                          <Typography variant="h6" fontWeight={800} color="#324c36">
                            Lv.{player.jobLevel}
                          </Typography>
                        </Paper>
                      </Box>

                      <Stack spacing={0.75}>
                        <Stack direction="row" justifyContent="space-between" spacing={1}>
                          <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.84)' }}>
                            {locale.jobExpProgress
                              .replace('{{current}}', String(jobExpProgress.current))
                              .replace('{{required}}', String(jobExpProgress.required))}
                          </Typography>
                          <Typography variant="body2" fontWeight={700} color="#f0ddb0">
                            {Math.round(jobExpProgress.ratio)}%
                          </Typography>
                        </Stack>
                        <LinearProgress
                          variant="determinate"
                          value={jobExpProgress.ratio}
                          sx={{
                            height: 10,
                            borderRadius: 999,
                            backgroundColor: 'rgba(255, 249, 232, 0.35)',
                            '& .MuiLinearProgress-bar': {
                              backgroundColor: '#f0ddb0',
                            },
                          }}
                        />
                      </Stack>
                    </Stack>
                  </Box>

                  <Alert severity="warning" sx={{ alignItems: 'center', borderRadius: 2 }}>
                    {locale.resetNoticeBody}
                  </Alert>
                </Stack>
              </Paper>

              {successMessage ? <Alert severity="success">{successMessage}</Alert> : null}
              {submitError ? <Alert severity="error">{submitError}</Alert> : null}
              {learnedMoveNames.length > 0 ? (
                <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2 }}>
                  <Stack spacing={1}>
                    <Typography variant="subtitle1" fontWeight={700}>
                      {locale.learnedMoves}
                    </Typography>
                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      {learnedMoveNames.map((moveName) => (
                        <Chip key={moveName} label={moveName} color="info" variant="outlined" />
                      ))}
                    </Stack>
                  </Stack>
                </Paper>
              ) : null}

              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{locale.jobListTitle}</Typography>
                  {currentJobs.map((job) => {
                    const isCurrent = player.job.value === job.value
                    const isLocked = player.level < unlockThreshold
                    const isDisabled = isLocked || isCurrent
                    const isSubmitting = isSubmittingJobValue === job.value
                    const jobImageSrc = resolveJobAssetPath(job.code)
                    const statusLabel = isCurrent
                      ? locale.currentBadge
                      : isLocked
                        ? locale.jobLocked
                        : locale.jobAvailable
                    const description = isCurrent
                      ? locale.currentDetail
                      : isLocked
                        ? locale.jobLockedDetail
                        : locale.jobResetDetail.replace('{{jobName}}', job.displayName)

                    return (
                      <Paper
                        key={job.code}
                        variant="outlined"
                        sx={{
                          ...innerSurfaceSx,
                          borderRadius: 2,
                          p: 2,
                          backgroundColor: isCurrent ? '#f8f3e3' : '#fffdf8',
                          borderColor: isCurrent ? '#bca56d' : innerSurfaceSx.borderColor,
                        }}
                      >
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={{ xs: 1.5, sm: 2.5 }}
                          justifyContent="space-between"
                          alignItems={{ xs: 'stretch', sm: 'center' }}
                        >
                          <Stack
                            direction={{ xs: 'column', sm: 'row' }}
                            spacing={1.5}
                            alignItems={{ xs: 'stretch', sm: 'center' }}
                          >
                            {jobImageSrc ? (
                              <Box
                                sx={{
                                  width: { xs: '100%', sm: 132 },
                                  minWidth: { sm: 132 },
                                  aspectRatio: '1 / 1',
                                  borderRadius: 2.5,
                                  border: '2px solid',
                                  borderColor: isCurrent ? '#bca56d' : '#d1c19a',
                                  bgcolor: '#fff9ef',
                                  overflow: 'hidden',
                                  display: 'grid',
                                  placeItems: 'center',
                                  p: 1.25,
                                  boxShadow: isCurrent
                                    ? '0 10px 18px rgba(52, 74, 45, 0.14)'
                                    : '0 6px 12px rgba(79, 71, 47, 0.08)',
                                  backgroundImage: isCurrent
                                    ? 'radial-gradient(circle at 50% 35%, rgba(231, 241, 219, 0.95), rgba(255, 248, 238, 0.88) 58%, rgba(220, 230, 201, 0.9))'
                                    : 'radial-gradient(circle at 50% 35%, rgba(246, 240, 214, 0.94), rgba(255, 250, 240, 0.86) 60%, rgba(233, 225, 195, 0.84))',
                                }}
                              >
                                <Box
                                  component="img"
                                  src={jobImageSrc}
                                  alt={job.displayName}
                                  loading="lazy"
                                  sx={{
                                    width: '100%',
                                    height: '100%',
                                    objectFit: 'contain',
                                    display: 'block',
                                    filter: 'drop-shadow(0 8px 12px rgba(91, 63, 16, 0.14))',
                                    transform: isCurrent ? 'scale(1.05)' : 'scale(1.02)',
                                  }}
                                />
                              </Box>
                            ) : null}
                            <Stack spacing={0.5}>
                              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                <Typography variant="subtitle1" fontWeight={700} sx={{ fontSize: '1.2rem' }}>
                                  {job.displayName}
                                </Typography>
                                <Chip
                                  label={statusLabel}
                                  size="small"
                                  variant={isLocked ? 'outlined' : 'filled'}
                                  sx={{
                                    fontWeight: 700,
                                    ...(isCurrent
                                      ? {
                                          bgcolor: 'rgba(255, 249, 232, 0.92)',
                                          color: '#35513a',
                                          border: '1px solid #cbb783',
                                        }
                                      : isLocked
                                        ? {
                                            bgcolor: 'transparent',
                                            color: '#6c6246',
                                            border: '1px solid #c7ba96',
                                          }
                                        : {
                                            bgcolor: '#4a6c51',
                                            color: '#fff8e8',
                                            border: '1px solid #bfa86e',
                                          }),
                                  }}
                                />
                              </Stack>
                              <Typography variant="body2" color="text.secondary">
                                {job.description}
                              </Typography>
                              <Typography variant="body2" color="text.secondary">
                                {description}
                              </Typography>
                            </Stack>
                          </Stack>
                          {isCurrent ? null : (
                            <Button
                              variant="contained"
                              sx={{
                                ...(isDisabled
                                  ? {
                                      '&&': {
                                        backgroundColor: '#efe7cf',
                                        borderColor: '#d5c49a',
                                        color: '#8a7d5d',
                                      },
                                      '&&:hover': {
                                        backgroundColor: '#efe7cf',
                                        borderColor: '#d5c49a',
                                        boxShadow: 'none',
                                      },
                                      '&&.Mui-disabled': {
                                        backgroundColor: '#efe7cf',
                                        borderColor: '#d5c49a',
                                        color: '#8a7d5d',
                                      },
                                    }
                                  : {
                                      '&&': {
                                        backgroundColor: '#4a6c51',
                                        borderColor: '#bfa86e',
                                        color: '#fff8e8',
                                      },
                                      '&&:hover': {
                                        backgroundColor: '#3f5f46',
                                        borderColor: '#b59d63',
                                        boxShadow: 'none',
                                      },
                                      '&&.Mui-disabled': {
                                        backgroundColor: '#7f967f',
                                        borderColor: '#c4b07c',
                                        color: '#f5efe0',
                                      },
                                    }),
                                minWidth: { sm: 116 },
                                alignSelf: { sm: 'center' },
                                whiteSpace: 'nowrap',
                              }}
                              disabled={isDisabled || isSubmittingJobValue !== null}
                              fullWidth={false}
                              onClick={() => {
                                void handleChangeJob(job.value)
                              }}
                            >
                              {isSubmitting ? locale.changing : isLocked ? locale.lockedButton : locale.changeButton}
                            </Button>
                          )}
                        </Stack>
                      </Paper>
                    )
                  })}
                </Stack>
              </Paper>
            </Stack>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
