import { Alert, Box, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayerJob } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import JobChangeTab from '@/components/player-job/JobChangeTab'
import JobRoadmapTab from '@/components/player-job/JobRoadmapTab'
import { innerSurfaceSx, outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { getTutorialStep, setTutorialStep } from '@/lib/tutorial'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import locale from '../../locale/player-job/JobChange.json'

export default function JobChange() {
  const { session, isLoading } = useAuth()
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [learnedMoveNames, setLearnedMoveNames] = useState<string[]>([])
  const [isSubmittingJobValue, setIsSubmittingJobValue] = useState<number | null>(null)
  const [tabIndex, setTabIndex] = useState(0)

  useEffect(() => {
    if (!userId) {
      return
    }

    const step = getTutorialStep(userId)
    if (step === 'home-job-change') {
      setTutorialStep(userId, 'job-change-info')
      setTutorialStepState('job-change-info')
    } else {
      setTutorialStepState(step)
    }
  }, [userId])

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }

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
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <HomeNavIconButton
              id={tutorialStep === 'training-to-lv7' ? 'tutorial-home-btn' : undefined}
              ariaLabel={locale.backToHome}
            />
            <Box
              role="tablist"
              sx={{
                display: 'flex',
                border: '2px solid #8f6b2f',
                borderRadius: 1.5,
                overflow: 'hidden',
              }}
            >
              {[locale.title, locale.roadmapTab].map((label, i) => (
                <Box
                  key={i}
                  id={i === 1 && tutorialStep === 'job-change-roadmap' ? 'tutorial-roadmap-tab' : undefined}
                  role="tab"
                  aria-selected={tabIndex === i}
                  tabIndex={0}
                  onClick={() => {
                    setTabIndex(i)
                    if (i === 1 && tutorialStep === 'job-change-roadmap') {
                      advanceTutorial('training-to-lv7')
                    }
                  }}
                  onKeyDown={(e) => (e.key === 'Enter' || e.key === ' ') && setTabIndex(i)}
                  sx={{
                    px: { xs: 2, sm: 3 },
                    py: 0.75,
                    cursor: 'pointer',
                    fontWeight: 900,
                    fontSize: { xs: '0.8rem', sm: '0.9rem' },
                    userSelect: 'none',
                    borderRight: i === 0 ? '2px solid #8f6b2f' : 'none',
                    background: tabIndex === i ? 'linear-gradient(90deg, #e8c84a 0%, #f2d27a 100%)' : '#f5efd8',
                    color: tabIndex === i ? '#4a2e0a' : '#8a7a5a',
                    transition: 'background 0.1s, color 0.1s',
                    '&:hover': tabIndex !== i ? { background: '#ede4c5', color: '#6a5a3a' } : {},
                  }}
                >
                  {tabIndex === i ? `▶ ${label}` : label}
                </Box>
              ))}
            </Box>
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

              {tabIndex === 0 && (
                <JobChangeTab
                  player={player}
                  isSubmittingJobValue={isSubmittingJobValue}
                  onJobChange={handleChangeJob}
                />
              )}
              {tabIndex === 1 && <JobRoadmapTab />}
            </Stack>
          )}
        </Stack>
      </Paper>
      {tutorialStep === 'training-to-lv7' && successMessage && (
        <SpotlightTutorial targetId="tutorial-home-btn" message={tutorialLocale.steps.backToTraining.message} />
      )}
      {tutorialStep === 'job-change-info' && (
        <SpotlightTutorial
          message={tutorialLocale.steps.jobChangeInfo.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.jobChangeInfo.dismissLabel}
          onDismiss={() => advanceTutorial('job-change-roadmap')}
        />
      )}
      {tutorialStep === 'job-change-roadmap' && (
        <SpotlightTutorial targetId="tutorial-roadmap-tab" message={tutorialLocale.steps.jobChangeRoadmap.message} />
      )}
    </Container>
  )
}
