import { useState } from 'react'
import type { FormEvent, MouseEvent } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Divider,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import { Navigate } from 'react-router-dom'
import {
  googleButtonSx,
  greenOutlinedInputSx,
  innerSurfaceSx,
  outerPagePaperSx,
  softGoldButtonSx,
  softGreenButtonSx,
} from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { supabase } from '@/lib/supabase'
import GoogleColorIcon from '@/components/common/GoogleColorIcon'
import locale from '../../locale/auth/Auth.json'

type SubmitMode = 'google' | 'signIn' | 'signUp' | 'anonymous' | null
type EmailAuthMode = 'signIn' | 'signUp'

function Auth() {
  const { user, isLoading } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [emailAuthMode, setEmailAuthMode] = useState<EmailAuthMode>('signIn')
  const [submitMode, setSubmitMode] = useState<SubmitMode>(null)
  const [error, setError] = useState<string | null>(null)
  const [message, setMessage] = useState<string | null>(null)

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  if (user) {
    return <Navigate to="/" replace />
  }

  const resolveError = (msg: string): string => {
    const normalized = msg.trim().toLowerCase()
    return locale.errorMessages[normalized as keyof typeof locale.errorMessages] ?? locale.defaultError
  }

  const handleGoogleSignIn = async () => {
    setSubmitMode('google')
    setError(null)
    setMessage(null)

    const redirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()
    const { error: oauthError } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo },
    })

    if (oauthError) {
      setError(resolveError(oauthError.message))
      setSubmitMode(null)
    }
  }

  const handleEmailAuthModeChange = (_event: MouseEvent<HTMLElement>, nextMode: EmailAuthMode | null) => {
    if (!nextMode) {
      return
    }

    setEmailAuthMode(nextMode)
    setError(null)
    setMessage(null)
  }

  const handleEmailAuth = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setError(null)
    setMessage(null)

    if (emailAuthMode === 'signIn') {
      setSubmitMode('signIn')
      const { error: signInError } = await supabase.auth.signInWithPassword({ email, password })

      if (signInError) {
        setError(resolveError(signInError.message))
        setSubmitMode(null)
        return
      }

      setSubmitMode(null)
      return
    }

    if (password.length < 6) {
      setError(locale.errorMessages['password should be at least 6 characters'])
      setSubmitMode(null)
      return
    }

    setSubmitMode('signUp')

    const emailRedirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()
    const { data, error: signUpError } = await supabase.auth.signUp({
      email,
      password,
      options: { emailRedirectTo },
    })

    if (signUpError) {
      setError(resolveError(signUpError.message))
      setSubmitMode(null)
      return
    }

    if (!data.session?.access_token) {
      setMessage(locale.toastNeedsEmailConfirm)
    }

    setSubmitMode(null)
  }

  const handleAnonymousSignIn = async () => {
    setSubmitMode('anonymous')
    setError(null)
    setMessage(null)

    const { error: anonymousError } = await supabase.auth.signInAnonymously()

    if (anonymousError) {
      setError(resolveError(anonymousError.message))
      setSubmitMode(null)
    }
  }

  const isAnySubmitting = submitMode !== null

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={2}>
          {message ? <Alert severity="info">{message}</Alert> : null}

          <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={2}>
              <Button
                variant="contained"
                disabled={isAnySubmitting}
                onClick={handleGoogleSignIn}
                sx={googleButtonSx}
                startIcon={<GoogleColorIcon />}
              >
                {submitMode === 'google' ? locale.googleSignInSubmitting : locale.googleSignIn}
              </Button>

              <Divider>
                <Typography variant="body2" color="text.secondary">
                  {locale.orDivider}
                </Typography>
              </Divider>

              <Box component="form" onSubmit={handleEmailAuth} noValidate>
                <Stack spacing={2}>
                  <ToggleButtonGroup
                    value={emailAuthMode}
                    exclusive
                    onChange={handleEmailAuthModeChange}
                    fullWidth
                    disabled={isAnySubmitting}
                    sx={{
                      p: 0.5,
                      borderRadius: 2,
                      backgroundColor: 'rgba(71, 125, 59, 0.1)',
                      border: 'none',
                      '& .MuiToggleButtonGroup-grouped': {
                        border: 0,
                        borderRadius: 1.5,
                        py: 0.9,
                        fontWeight: 700,
                        color: 'rgba(36, 58, 31, 0.85)',
                        textTransform: 'none',
                        transition: 'none',
                        '&.Mui-focusVisible': {
                          outline: 'none',
                        },
                      },
                      '& .MuiToggleButtonGroup-grouped.Mui-selected': {
                        color: '#ffffff',
                        backgroundColor: '#4e7f42',
                        boxShadow: 'none',
                      },
                      '& .MuiToggleButtonGroup-grouped.Mui-selected:hover': {
                        backgroundColor: '#4e7f42',
                      },
                    }}
                  >
                    <ToggleButton value="signIn" disableRipple>
                      {locale.signIn}
                    </ToggleButton>
                    <ToggleButton value="signUp" disableRipple>
                      {locale.signUp}
                    </ToggleButton>
                  </ToggleButtonGroup>
                  <TextField
                    id="auth-email"
                    label={locale.emailLabel}
                    type="email"
                    autoComplete="email"
                    value={email}
                    onChange={(event) => setEmail(event.target.value)}
                    required
                    fullWidth
                    sx={greenOutlinedInputSx}
                  />
                  <TextField
                    id="auth-password"
                    label={locale.passwordLabel}
                    type="password"
                    autoComplete="current-password"
                    value={password}
                    onChange={(event) => setPassword(event.target.value)}
                    required
                    fullWidth
                    sx={greenOutlinedInputSx}
                  />
                  <Button type="submit" variant="contained" disabled={isAnySubmitting} fullWidth sx={softGreenButtonSx}>
                    {submitMode === emailAuthMode
                      ? emailAuthMode === 'signIn'
                        ? locale.signInSubmitting
                        : locale.signUpSubmitting
                      : emailAuthMode === 'signIn'
                        ? locale.signIn
                        : locale.signUp}
                  </Button>
                  {error ? <Alert severity="error">{error}</Alert> : null}
                </Stack>
              </Box>

              <Divider>
                <Typography variant="body2" color="text.secondary">
                  {locale.orDivider}
                </Typography>
              </Divider>

              <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line', textAlign: 'center' }}>
                {locale.anonymousDescription}
              </Typography>
              <Button
                type="button"
                variant="contained"
                disabled={isAnySubmitting}
                onClick={handleAnonymousSignIn}
                sx={softGoldButtonSx}
              >
                {submitMode === 'anonymous' ? locale.anonymousSubmitting : locale.anonymousSubmit}
              </Button>
            </Stack>
          </Paper>
        </Stack>
      </Paper>
    </Container>
  )
}

export default Auth
