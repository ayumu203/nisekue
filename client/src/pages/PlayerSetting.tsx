import { useEffect, useState } from 'react'
import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, TextField, Typography } from '@mui/material'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { getPlayer, updatePlayer } from '@/api/player'
import { useAuth } from '@/contexts/useAuth'
import { playerUserNameSchema } from '@/schema/player'
import locale from '../../locale/player/PlayerSetting.json'

function PlayerSetting() {
  const { session } = useAuth()
  const [userName, setUserName] = useState('')
  const [isInitialized, setIsInitialized] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const playerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading,
    mutate,
  } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getPlayer(session.access_token)
  })

  useEffect(() => {
    if (!isInitialized && player) {
      setUserName(player.userName ?? '')
      setIsInitialized(true)
    }
  }, [isInitialized, player])

  const normalizedUserName = userName.trim()
  const nameValidation = playerUserNameSchema.safeParse(normalizedUserName)
  const fieldError = normalizedUserName.length === 0 ? null : nameValidation.error?.issues[0]?.message ?? null
  const isUnchanged = normalizedUserName === (player?.userName ?? '')

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()

    if (!session?.access_token) {
      setErrorMessage(locale.sessionInfoMissing)
      return
    }

    const validation = playerUserNameSchema.safeParse(normalizedUserName)
    if (!validation.success) {
      setErrorMessage(validation.error.issues[0]?.message ?? locale.playerMissing)
      setSuccessMessage(null)
      return
    }

    setIsSubmitting(true)
    setErrorMessage(null)
    setSuccessMessage(null)

    try {
      const updated = await updatePlayer({ userName: validation.data }, session.access_token)
      await mutate(
        (current) =>
          current
            ? {
                ...current,
                userName: updated.userName ?? validation.data,
              }
            : current,
        { revalidate: false },
      )
      setUserName(updated.userName ?? validation.data)
      setSuccessMessage(updated.message || locale.success)
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : locale.playerMissing)
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <Stack spacing={3}>
          <Stack direction="row" spacing={1} justifyContent="space-between" alignItems="center">
            <Typography variant="h4">{locale.title}</Typography>
            <Button component={Link} to="/" variant="outlined">
              {locale.backToHome}
            </Button>
          </Stack>

          <Typography color="text.secondary">{locale.description}</Typography>

          {isLoading ? (
            <Box minHeight={120} display="grid" sx={{ placeItems: 'center' }}>
              <Stack direction="row" spacing={1} alignItems="center">
                <CircularProgress size={20} />
                <Typography>{locale.playerLoading}</Typography>
              </Stack>
            </Box>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : !player ? (
            <Alert severity="warning">{locale.playerMissing}</Alert>
          ) : (
            <Stack spacing={2} component="form" onSubmit={handleSubmit} noValidate>
              <Alert severity="info">
                {locale.currentName}: {player.userName ?? '-'}
              </Alert>
              {successMessage ? <Alert severity="success">{successMessage}</Alert> : null}
              {errorMessage ? <Alert severity="error">{errorMessage}</Alert> : null}
              <TextField
                label={locale.nameLabel}
                placeholder={locale.namePlaceholder}
                value={userName}
                onChange={(event) => {
                  setUserName(event.target.value)
                  setErrorMessage(null)
                  setSuccessMessage(null)
                }}
                disabled={isSubmitting}
                error={Boolean(fieldError)}
                helperText={fieldError ?? ' '}
                fullWidth
                inputProps={{ maxLength: 20 }}
              />
              <Button
                type="submit"
                variant="contained"
                disabled={isSubmitting || normalizedUserName.length === 0 || Boolean(fieldError) || isUnchanged}
              >
                {isSubmitting ? locale.submitting : locale.submit}
              </Button>
            </Stack>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}

export default PlayerSetting
