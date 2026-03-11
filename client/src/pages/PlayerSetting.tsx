import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayer } from '@/api/player'
import { outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-setting/PlayerSetting.json'

export default function PlayerSetting() {
  const { session, isLoading } = useAuth()
  const [userName, setUserName] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  const playerSWRKey = session?.user.id ? ([`player-setting`, session.user.id] as const) : null
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
      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  useEffect(() => {
    setUserName(player?.userName ?? '')
  }, [player?.userName])

  async function handleSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    if (!session?.access_token) {
      setSubmitError(locale.sessionMissing)
      return
    }

    setSubmitError(null)
    setSuccessMessage(null)
    setIsSubmitting(true)

    try {
      await updatePlayer({ userName }, session.access_token)
      await mutatePlayer()
      setSuccessMessage(locale.saved)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.saveFailed)
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
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Stack spacing={1}>
            <Typography variant="h4" textAlign="center">
              {locale.title}
            </Typography>
            <Button component={Link} to="/" variant="outlined" sx={{ alignSelf: 'flex-end' }}>
              {locale.backToHome}
            </Button>
          </Stack>

          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.loadingPlayer}</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : (
            <Box component="form" onSubmit={handleSubmit}>
              <Stack spacing={2}>
                <TextField
                  fullWidth
                  label={locale.playerNameLabel}
                  placeholder={locale.playerNamePlaceholder}
                  value={userName}
                  onChange={(event) => {
                    setUserName(event.target.value)
                  }}
                />
                {submitError ? <Alert severity="warning">{submitError}</Alert> : null}
                {successMessage ? <Alert severity="success">{successMessage}</Alert> : null}
                <Button type="submit" variant="contained" disabled={isSubmitting}>
                  {isSubmitting ? locale.saving : locale.submitButton}
                </Button>
              </Stack>
            </Box>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
