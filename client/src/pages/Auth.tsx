import { useState } from 'react'
import type { FormEvent } from 'react'
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
  Typography,
} from '@mui/material'
import { Navigate } from 'react-router-dom'
import {
  greenOutlinedInputSx,
  innerSurfaceSx,
  outerPagePaperSx,
  softGoldButtonSx,
  softGreenButtonSx,
} from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { supabase } from '@/lib/supabase'
import locale from '../../locale/auth/Auth.json'

type SubmitMode = 'google' | 'signIn' | 'signUp' | 'anonymous' | null

function Auth() {
  const { user, isLoading } = useAuth()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
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

  const handleSignIn = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSubmitMode('signIn')
    setError(null)
    setMessage(null)

    const { error: signInError } = await supabase.auth.signInWithPassword({ email, password })

    if (signInError) {
      setError(resolveError(signInError.message))
      setSubmitMode(null)
      return
    }

    setSubmitMode(null)
  }

  const handleSignUp = async () => {
    if (password.length < 6) {
      setError(locale.errorMessages['password should be at least 6 characters'])
      return
    }

    setSubmitMode('signUp')
    setError(null)
    setMessage(null)

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
      setSubmitMode(null)
      return
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
                sx={softGreenButtonSx}
              >
                {submitMode === 'google' ? locale.googleSignInSubmitting : locale.googleSignIn}
              </Button>

              <Divider>
                <Typography variant="body2" color="text.secondary">
                  {locale.orDivider}
                </Typography>
              </Divider>

              <Box component="form" onSubmit={handleSignIn} noValidate>
                <Stack spacing={2}>
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
                  <Stack direction="row" spacing={1}>
                    <Button
                      type="submit"
                      variant="contained"
                      disabled={isAnySubmitting}
                      fullWidth
                      sx={softGreenButtonSx}
                    >
                      {submitMode === 'signIn' ? locale.signInSubmitting : locale.signIn}
                    </Button>
                    <Button
                      type="button"
                      variant="contained"
                      disabled={isAnySubmitting}
                      fullWidth
                      onClick={handleSignUp}
                      sx={softGreenButtonSx}
                    >
                      {submitMode === 'signUp' ? locale.signUpSubmitting : locale.signUp}
                    </Button>
                  </Stack>
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
