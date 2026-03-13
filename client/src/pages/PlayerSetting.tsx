import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayer, updatePlayerImage } from '@/api/player'
import {
  greenOutlinedInputSx,
  innerSurfaceSx,
  mutedGreenButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
} from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { PLAYER_IMAGE_COUNT, resolvePlayerImageFileName, resolvePlayerImageNo } from '@/lib/playerImages'
import locale from '../../locale/player-setting/PlayerSetting.json'

export default function PlayerSetting() {
  const { session, isLoading } = useAuth()
  const [userName, setUserName] = useState('')
  const [imageNoInput, setImageNoInput] = useState('')
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

  useEffect(() => {
    const imageNo = resolvePlayerImageNo(player?.imagePath)
    setImageNoInput(imageNo ? String(imageNo) : '1')
  }, [player?.imagePath])

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
      const normalizedImageNo = Number.parseInt(imageNoInput, 10)
      if (!Number.isInteger(normalizedImageNo) || normalizedImageNo < 1 || normalizedImageNo > PLAYER_IMAGE_COUNT) {
        throw new Error(locale.imageNoRange.replace('{{max}}', String(PLAYER_IMAGE_COUNT)))
      }

      await updatePlayer({ userName }, session.access_token)
      await updatePlayerImage({ imageNo: normalizedImageNo }, session.access_token)
      await mutatePlayer()
      setSuccessMessage(locale.saved)
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.saveFailed)
    } finally {
      setIsSubmitting(false)
    }
  }

  const selectedImageNo = Number.parseInt(imageNoInput, 10)
  const previewImagePath =
    Number.isInteger(selectedImageNo) && selectedImageNo >= 1 && selectedImageNo <= PLAYER_IMAGE_COUNT
      ? resolveCharacterAssetPath(resolvePlayerImageFileName(selectedImageNo))
      : null

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
            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
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
                    sx={greenOutlinedInputSx}
                  />
                  <Stack spacing={1.5}>
                    <Typography variant="subtitle1">{locale.playerImageLabel}</Typography>
                    <Box
                      sx={{
                        width: '100%',
                        maxWidth: 237,
                        aspectRatio: '338 / 350',
                        borderRadius: 2,
                        border: '1px solid',
                        borderColor: 'divider',
                        bgcolor: 'rgba(255,255,255,0.72)',
                        overflow: 'hidden',
                        alignSelf: 'center',
                      }}
                    >
                      {previewImagePath ? (
                        <Box
                          component="img"
                          src={previewImagePath}
                          alt={locale.playerImagePreviewAlt}
                          sx={{
                            width: '100%',
                            height: '100%',
                            objectFit: 'contain',
                            objectPosition: 'center bottom',
                            display: 'block',
                          }}
                        />
                      ) : (
                        <Box sx={{ width: '100%', height: '100%', display: 'grid', placeItems: 'center', px: 2 }}>
                          <Typography variant="body2" color="text.secondary">
                            {locale.imageNoRange.replace('{{max}}', String(PLAYER_IMAGE_COUNT))}
                          </Typography>
                        </Box>
                      )}
                    </Box>
                    <TextField
                      fullWidth
                      type="number"
                      label={locale.playerImageNoLabel}
                      value={imageNoInput}
                      onChange={(event) => {
                        setImageNoInput(event.target.value)
                      }}
                      inputProps={{
                        min: 1,
                        max: PLAYER_IMAGE_COUNT,
                      }}
                      helperText={locale.imageNoRange.replace('{{max}}', String(PLAYER_IMAGE_COUNT))}
                      sx={greenOutlinedInputSx}
                    />
                    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
                      <Button
                        type="button"
                        variant="contained"
                        sx={mutedGreenButtonSx}
                        onClick={() => {
                          const randomImageNo = Math.floor(Math.random() * PLAYER_IMAGE_COUNT) + 1
                          setImageNoInput(String(randomImageNo))
                        }}
                      >
                        {locale.randomSelect}
                      </Button>
                      <Button component={Link} to="/player-images" variant="contained" sx={mutedGreenButtonSx}>
                        {locale.openImageList}
                      </Button>
                    </Stack>
                  </Stack>
                  {submitError ? <Alert severity="warning">{submitError}</Alert> : null}
                  {successMessage ? <Alert severity="success">{successMessage}</Alert> : null}
                  <Button type="submit" variant="contained" disabled={isSubmitting} sx={softGreenButtonSx}>
                    {isSubmitting ? locale.saving : locale.submitButton}
                  </Button>
                </Stack>
              </Box>
            </Paper>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
