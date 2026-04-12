import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, TextField, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayer, updatePlayerImage } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import {
  greenOutlinedInputSx,
  innerSurfaceSx,
  mutedGreenButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
} from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { hasAnonymousIdentity } from '@/lib/auth'
import { supabase } from '@/lib/supabase'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { PLAYER_IMAGE_COUNT, resolvePlayerImageFileName, resolvePlayerImageNo } from '@/lib/playerImages'
import locale from '../../locale/player-setting/PlayerSetting.json'

export default function PlayerSetting() {
  const { session, isLoading } = useAuth()
  const [accountEmail, setAccountEmail] = useState<string | null>(session?.user.email ?? null)
  const [accountIsAnonymous, setAccountIsAnonymous] = useState(hasAnonymousIdentity(session?.user ?? null))
  const [userName, setUserName] = useState('')
  const [imageNoInput, setImageNoInput] = useState('')
  const [submitError, setSubmitError] = useState<string | null>(null)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [accountEmailInput, setAccountEmailInput] = useState(session?.user.email ?? '')
  const [accountPasswordInput, setAccountPasswordInput] = useState('')
  const [accountError, setAccountError] = useState<string | null>(null)
  const [accountSuccess, setAccountSuccess] = useState<string | null>(null)
  const [accountAction, setAccountAction] = useState<'account' | null>(null)
  const settingInputSx = {
    ...greenOutlinedInputSx,
    '& .MuiInputLabel-root.Mui-focused': {
      color: '#f3eedc',
    },
    '& .MuiFormHelperText-root': {
      color: 'rgba(243, 238, 220, 0.72)',
    },
  } as const

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
    setImageNoInput(imageNo ? String(imageNo) : '')
  }, [player?.imagePath])

  useEffect(() => {
    setAccountEmail(session?.user.email ?? null)
    setAccountIsAnonymous(hasAnonymousIdentity(session?.user ?? null))
  }, [session?.user])

  useEffect(() => {
    if (session?.user.email) {
      setAccountEmailInput(session.user.email)
    }
  }, [session?.user.email])

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
      const normalizedUserName = userName.trim()
      let normalizedImageNo: number | null = null

      if (imageNoInput.trim() !== '') {
        const parsedImageNo = Number.parseInt(imageNoInput, 10)
        if (!Number.isInteger(parsedImageNo) || parsedImageNo < 1 || parsedImageNo > PLAYER_IMAGE_COUNT) {
          throw new Error(locale.imageNoRange.replace('{{max}}', String(PLAYER_IMAGE_COUNT)))
        }

        normalizedImageNo = parsedImageNo
      }

      const currentImageNo = resolvePlayerImageNo(player?.imagePath)
      const shouldUpdateName = normalizedUserName !== (player?.userName ?? '')
      const shouldUpdateImage = normalizedImageNo !== null && normalizedImageNo !== currentImageNo

      if (!shouldUpdateName && !shouldUpdateImage) {
        setSuccessMessage(locale.saved)
        return
      }

      const errors: string[] = []
      let anySuccess = false

      if (shouldUpdateName) {
        try {
          await updatePlayer({ userName: normalizedUserName }, session.access_token)
          anySuccess = true
        } catch (error) {
          errors.push(error instanceof Error ? error.message : locale.saveFailed)
        }
      }

      if (shouldUpdateImage && normalizedImageNo !== null) {
        try {
          await updatePlayerImage({ imageNo: normalizedImageNo }, session.access_token)
          anySuccess = true
        } catch (error) {
          errors.push(error instanceof Error ? error.message : locale.saveFailed)
        }
      }

      if (anySuccess) {
        await mutatePlayer()
      }

      if (errors.length === 0) {
        setSuccessMessage(locale.saved)
      } else if (anySuccess) {
        setSubmitError(`${locale.partialSaveFailed} ${errors.join(' ')}`)
      } else {
        setSubmitError(errors.join(' ') || locale.saveFailed)
      }
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
  const anonymousIdentity = accountIsAnonymous

  async function handleAccountSubmit(event: FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault()

    const trimmedEmail = accountEmailInput.trim()
    const trimmedPassword = accountPasswordInput.trim()

    if (!trimmedEmail) {
      setAccountError(locale.accountEmailRequired)
      setAccountSuccess(null)
      return
    }

    if (trimmedPassword.length < 6) {
      setAccountError(locale.passwordMinLength)
      setAccountSuccess(null)
      return
    }

    setAccountAction('account')
    setAccountError(null)
    setAccountSuccess(null)

    const emailRedirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()
    const { data, error } = await supabase.auth.updateUser(
      {
        email: trimmedEmail,
        password: trimmedPassword,
      },
      { emailRedirectTo },
    )

    if (error) {
      const normalized = error.message.trim().toLowerCase()
      const existingAccountMessages = new Set([
        'user already registered',
        'email address already in use',
        'email exists',
      ])
      setAccountError(
        existingAccountMessages.has(normalized) ? locale.linkEmailExistingAccount : locale.setAccountDefaultError,
      )
      setAccountAction(null)
      return
    }

    const latestEmail = data.user?.email?.trim().toLowerCase()
    const nextAnonymousIdentity = hasAnonymousIdentity(data.user ?? null)
    setAccountEmail(data.user?.email ?? trimmedEmail)
    setAccountIsAnonymous(nextAnonymousIdentity)
    setAccountEmailInput(data.user?.email ?? trimmedEmail)
    setAccountPasswordInput('')
    setAccountSuccess(
      latestEmail === trimmedEmail.toLowerCase()
        ? locale.setAccountImmediateSuccess
        : locale.setAccountEmailPendingSuccess,
    )
    setAccountAction(null)
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
    <Container maxWidth="lg" sx={{ py: '1%' }}>
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
          ) : (
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              <Paper
                variant="outlined"
                sx={{
                  ...innerSurfaceSx,
                  borderRadius: 3,
                  p: { xs: 2, sm: 2.5 },
                  backgroundColor: '#44644a',
                  borderColor: '#b8ab7a',
                  color: '#fff8ea',
                }}
              >
                <Box component="form" onSubmit={handleSubmit}>
                  <Stack spacing={2}>
                    <Stack spacing={0.25}>
                      <Typography
                        variant="overline"
                        sx={{ letterSpacing: '0.18em', color: 'rgba(243, 238, 220, 0.72)' }}
                      >
                        {locale.titleRuby}
                      </Typography>
                      <Typography variant="h4" fontWeight={900} color="#fff8ea">
                        {locale.title}
                      </Typography>
                    </Stack>
                    <Stack spacing={0.75}>
                      <Typography id="player-name-label" variant="subtitle1" sx={{ color: 'rgba(243, 238, 220, 0.9)' }}>
                        {locale.playerNameLabel}
                      </Typography>
                      <TextField
                        fullWidth
                        placeholder={locale.playerNamePlaceholder}
                        value={userName}
                        onChange={(event) => {
                          setUserName(event.target.value)
                        }}
                        inputProps={{
                          'aria-labelledby': 'player-name-label',
                        }}
                        sx={settingInputSx}
                      />
                    </Stack>
                    <Stack spacing={1.5}>
                      <Typography variant="subtitle1" sx={{ color: 'rgba(243, 238, 220, 0.9)' }}>
                        {locale.playerImageLabel}
                      </Typography>
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
                      <Stack spacing={0.75}>
                        <Typography
                          id="player-image-no-label"
                          variant="subtitle1"
                          sx={{ color: 'rgba(243, 238, 220, 0.9)' }}
                        >
                          {locale.playerImageNoLabel}
                        </Typography>
                        <TextField
                          fullWidth
                          type="number"
                          value={imageNoInput}
                          onChange={(event) => {
                            setImageNoInput(event.target.value)
                          }}
                          inputProps={{
                            min: 1,
                            max: PLAYER_IMAGE_COUNT,
                            'aria-labelledby': 'player-image-no-label',
                          }}
                          helperText={locale.imageNoRange.replace('{{max}}', String(PLAYER_IMAGE_COUNT))}
                          sx={settingInputSx}
                        />
                      </Stack>
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

              <Paper
                variant="outlined"
                sx={{
                  ...innerSurfaceSx,
                  borderRadius: 3,
                  p: { xs: 2, sm: 2.5 },
                  backgroundColor: '#44644a',
                  borderColor: '#b8ab7a',
                  color: '#fff8ea',
                }}
              >
                <Stack spacing={2}>
                  <Stack spacing={0.5}>
                    <Typography variant="h5" fontWeight={800} color="#fff8ea">
                      {locale.accountLinkTitle}
                    </Typography>
                    <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.82)' }}>
                      {anonymousIdentity ? locale.accountLinkDescription : locale.accountLinkedDescription}
                    </Typography>
                    {accountEmail ? (
                      <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.72)' }}>
                        {locale.accountCurrentEmail.replace('{email}', accountEmail)}
                      </Typography>
                    ) : null}
                  </Stack>

                  <Box component="form" onSubmit={handleAccountSubmit} noValidate>
                    <Stack spacing={2}>
                      <Stack spacing={0.75}>
                        <Typography variant="subtitle1" sx={{ color: 'rgba(243, 238, 220, 0.9)' }}>
                          {locale.accountEmailLabel}
                        </Typography>
                        <TextField
                          fullWidth
                          type="email"
                          placeholder={locale.accountEmailPlaceholder}
                          value={accountEmailInput}
                          onChange={(event) => {
                            setAccountEmailInput(event.target.value)
                          }}
                          sx={settingInputSx}
                        />
                      </Stack>

                      <Stack spacing={0.75}>
                        <Typography variant="subtitle1" sx={{ color: 'rgba(243, 238, 220, 0.9)' }}>
                          {locale.setPasswordLabel}
                        </Typography>
                        <Typography variant="body2" sx={{ color: 'rgba(243, 238, 220, 0.82)' }}>
                          {locale.setAccountDescription}
                        </Typography>
                        <TextField
                          fullWidth
                          type="password"
                          placeholder={locale.setPasswordPlaceholder}
                          value={accountPasswordInput}
                          onChange={(event) => {
                            setAccountPasswordInput(event.target.value)
                          }}
                          sx={settingInputSx}
                        />
                      </Stack>

                      <Button
                        type="submit"
                        variant="contained"
                        disabled={accountAction !== null}
                        sx={softGreenButtonSx}
                      >
                        {accountAction === 'account' ? locale.setAccountSubmitting : locale.setAccountSubmit}
                      </Button>
                    </Stack>
                  </Box>

                  {accountError ? <Alert severity="warning">{accountError}</Alert> : null}
                  {accountSuccess ? <Alert severity="success">{accountSuccess}</Alert> : null}
                </Stack>
              </Paper>
            </Stack>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
