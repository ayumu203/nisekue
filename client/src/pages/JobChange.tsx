import { Alert, Box, Button, Chip, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayerJob } from '@/api/player'
import { innerSurfaceSx, mutedGreenButtonSx, outerPagePaperSx, softGreenButtonSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-job/JobChange.json'

const basicJobs = [
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
              <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                <Stack spacing={1.5}>
                  <Typography variant="h6">{locale.currentStatusTitle}</Typography>
                  <Typography>{locale.currentJob.replace('{{jobName}}', player.job.displayName)}</Typography>
                  <Typography>{locale.currentLevel.replace('{{level}}', String(player.level))}</Typography>
                  {player.job.code === 'Apprentice' ? (
                    <Typography color={player.level >= 5 ? 'success.main' : 'text.secondary'}>
                      {player.level >= 5 ? locale.canChange : locale.cannotChange}
                    </Typography>
                  ) : (
                    <Typography color="text.secondary">{locale.onlyApprentice}</Typography>
                  )}
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
                  {basicJobs.map((job) => {
                    const isCurrent = player.job.value === job.value
                    const isDisabled = player.job.code !== 'Apprentice' || player.level < 5 || isCurrent
                    const isSubmitting = isSubmittingJobValue === job.value

                    return (
                      <Paper
                        key={job.code}
                        variant="outlined"
                        sx={{ ...innerSurfaceSx, borderRadius: 2, p: 2, backgroundColor: '#fffdf8' }}
                      >
                        <Stack
                          direction={{ xs: 'column', sm: 'row' }}
                          spacing={1.5}
                          justifyContent="space-between"
                          alignItems={{ xs: 'stretch', sm: 'center' }}
                        >
                          <Stack spacing={0.5}>
                            <Typography variant="subtitle1" fontWeight={700}>
                              {job.displayName}
                            </Typography>
                            <Typography variant="body2" color="text.secondary">
                              {isCurrent ? locale.currentSelected : locale.jobDescription.replace('{{jobName}}', job.displayName)}
                            </Typography>
                          </Stack>
                          <Button
                            variant="contained"
                            sx={isDisabled ? mutedGreenButtonSx : softGreenButtonSx}
                            disabled={isDisabled || isSubmittingJobValue !== null}
                            onClick={() => {
                              void handleChangeJob(job.value)
                            }}
                          >
                            {isSubmitting ? locale.changing : isCurrent ? locale.currentJobButton : locale.changeButton}
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
