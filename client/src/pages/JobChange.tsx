import { Alert, Box, Button, Chip, CircularProgress, Container, Divider, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayerJob } from '@/api/player'
import { innerSurfaceSx, mutedGreenButtonSx, outerPagePaperSx, softGreenButtonSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolveJobAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-job/JobChange.json'

const currentJobs = [
  { value: 2, code: 'Warrior', displayName: '戦士' },
  { value: 3, code: 'Guardian', displayName: '盾使い' },
  { value: 4, code: 'Mage', displayName: '魔法使い' },
  { value: 5, code: 'Priest', displayName: '僧侶' },
  { value: 6, code: 'Ranger', displayName: 'レンジャー' },
] as const

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
  const unlockThreshold = 5
  const playerLevel = player?.level ?? 1
  const remainingToUnlock = Math.max(0, unlockThreshold - playerLevel)
  const unlockProgress = Math.max(0, Math.min(100, (playerLevel / unlockThreshold) * 100))
  const canChangeAnyCurrentJob = playerLevel >= unlockThreshold

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
          <Stack spacing={1}>
            <Typography variant="h4" textAlign="center">
              {locale.title}
            </Typography>
            <Typography variant="body2" color="text.secondary" textAlign="center">
              {locale.subtitle}
            </Typography>
            <Button component={Link} to="/move-setting" variant="outlined" sx={{ alignSelf: 'flex-end' }}>
              {locale.backToMoveSetting}
            </Button>
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
              <Box
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', lg: 'minmax(0, 1.2fr) minmax(320px, 0.8fr)' },
                  gap: 2,
                }}
              >
                <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                  <Stack spacing={2}>
                    <Stack
                      direction={{ xs: 'column', sm: 'row' }}
                      spacing={2}
                      alignItems={{ xs: 'stretch', sm: 'center' }}
                      justifyContent="space-between"
                    >
                      <Stack spacing={0.75}>
                        <Typography variant="overline" color="text.secondary">
                          {locale.currentJob.replace('{{jobName}}', player.job.displayName)}
                        </Typography>
                        <Typography variant="h5" fontWeight={800}>
                          {player.job.displayName}
                        </Typography>
                      </Stack>
                      <Chip
                        color={canChangeAnyCurrentJob ? 'success' : 'default'}
                        label={canChangeAnyCurrentJob ? locale.canChange : locale.cannotChange}
                        variant={canChangeAnyCurrentJob ? 'filled' : 'outlined'}
                      />
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
                            maxWidth: 220,
                            aspectRatio: '1 / 1',
                            justifySelf: { sm: 'start' },
                            borderRadius: 2,
                            border: '1px solid',
                            borderColor: 'divider',
                            bgcolor: '#fffdf8',
                            overflow: 'hidden',
                            display: 'grid',
                            placeItems: 'center',
                            p: 1,
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
                          <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 2, p: 1.5 }}>
                            <Typography variant="caption" color="text.secondary">
                              {locale.currentLevel.replace('{{level}}', '')}
                            </Typography>
                            <Typography variant="h6" fontWeight={800}>
                              Lv.{player.level}
                            </Typography>
                          </Paper>
                          <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 2, p: 1.5 }}>
                            <Typography variant="caption" color="text.secondary">
                              {locale.currentJobLevel.replace('{{level}}', '')}
                            </Typography>
                            <Typography variant="h6" fontWeight={800}>
                              Lv.{player.jobLevel}
                            </Typography>
                          </Paper>
                        </Box>

                        <Stack spacing={0.75}>
                          <Stack direction="row" justifyContent="space-between" spacing={1}>
                            <Typography variant="body2" fontWeight={700}>
                              {canChangeAnyCurrentJob ? locale.unlockReady : locale.cannotChange}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              {player.level} / {unlockThreshold}
                            </Typography>
                          </Stack>
                          <LinearProgress
                            variant="determinate"
                            value={unlockProgress}
                            sx={{
                              height: 10,
                              borderRadius: 999,
                              backgroundColor: '#ecdca8',
                              '& .MuiLinearProgress-bar': {
                                backgroundColor: canChangeAnyCurrentJob ? '#78c27d' : '#d3a93a',
                              },
                            }}
                          />
                          {!canChangeAnyCurrentJob ? (
                            <Typography variant="body2" color="text.secondary">
                              {locale.unlockProgress.replace('{{remaining}}', String(remainingToUnlock))}
                            </Typography>
                          ) : null}
                        </Stack>
                      </Stack>
                    </Box>
                  </Stack>
                </Paper>

                <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                  <Stack spacing={1.5}>
                    <Typography variant="h6">{locale.resetNoticeTitle}</Typography>
                    <Alert severity="warning" sx={{ alignItems: 'center' }}>
                      {locale.resetNoticeBody}
                    </Alert>
                    <Divider />
                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      <Chip label={`Player Lv.${player.level}`} variant="outlined" />
                      <Chip label={`${player.job.displayName} Lv.${player.jobLevel}`} color="primary" variant="outlined" />
                    </Stack>
                  </Stack>
                </Paper>
              </Box>

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
                      ? locale.currentSelected
                      : isLocked
                        ? locale.jobLocked
                        : locale.jobAvailable
                    const statusColor = isCurrent ? 'info' : isLocked ? 'default' : 'success'
                    const description = isCurrent
                      ? locale.currentSelected
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
                          backgroundColor: isCurrent ? '#fff9e8' : '#fffdf8',
                          borderColor: isCurrent ? '#d3a93a' : innerSurfaceSx.borderColor,
                        }}
                      >
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1.5}
                          justifyContent="space-between"
                          alignItems={{ xs: 'stretch', sm: 'center' }}
                        >
                          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ xs: 'stretch', sm: 'center' }}>
                            {jobImageSrc ? (
                              <Box
                                sx={{
                                  width: { xs: '100%', sm: 112 },
                                  minWidth: { sm: 112 },
                                  aspectRatio: '1 / 1',
                                  borderRadius: 2,
                                  border: '1px solid',
                                  borderColor: 'divider',
                                  bgcolor: '#fff',
                                  overflow: 'hidden',
                                  display: 'grid',
                                  placeItems: 'center',
                                  p: 1,
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
                                  }}
                                />
                              </Box>
                            ) : null}
                            <Stack spacing={0.5}>
                              <Stack direction="row" spacing={1} alignItems="center" flexWrap="wrap" useFlexGap>
                                <Typography variant="subtitle1" fontWeight={700}>
                                  {job.displayName}
                                </Typography>
                                <Chip label={statusLabel} color={statusColor} size="small" variant={isLocked ? 'outlined' : 'filled'} />
                              </Stack>
                              <Typography variant="body2" color="text.secondary">
                                {locale.jobDescription.replace('{{jobName}}', job.displayName)}
                              </Typography>
                              <Typography variant="body2" color="text.secondary">
                                {description}
                              </Typography>
                            </Stack>
                          </Stack>
                          <Button
                            variant="contained"
                            sx={isDisabled ? mutedGreenButtonSx : softGreenButtonSx}
                            disabled={isDisabled || isSubmittingJobValue !== null}
                            onClick={() => {
                              void handleChangeJob(job.value)
                            }}
                          >
                            {isSubmitting
                              ? locale.changing
                              : isCurrent
                                ? locale.currentJobButton
                                : isLocked
                                  ? locale.lockedButton
                                  : locale.changeButton}
                          </Button>
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
