import { useState } from 'react'
import type { FormEvent, MouseEvent } from 'react'
import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Divider,
  Paper,
  Stack,
  TextField,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import { Navigate } from 'react-router-dom'
import { googleButtonSx, greenOutlinedInputSx, softGoldButtonSx, softGreenButtonSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolvePublicAssetPath } from '@/lib/assets'
import { supabase } from '@/lib/supabase'
import GoogleColorIcon from '@/components/common/GoogleColorIcon'
import locale from '../../locale/auth/Auth.json'

type SubmitMode = 'google' | 'signIn' | 'signUp' | 'anonymous' | null
type EmailAuthMode = 'signIn' | 'signUp'

const heroBackgroundUrl = resolvePublicAssetPath('image/quest/enchanted-forest-battlefield.svg')
const heroCharacterUrl = resolvePublicAssetPath('image/character/ch109_hero.png')
const heroCompanionUrl = resolvePublicAssetPath('image/character/ch001_bmnpc.png')
const heroEnemyUrl = resolvePublicAssetPath('image/battle/Enemy100.png')

const heroTitleSx = {
  fontSize: 'clamp(3rem, 6.5vw, 4.75rem)',
  lineHeight: 1.1,
  color: '#ffffff',
  letterSpacing: '0.06em',
  textShadow: [
    '-3px -3px 0 #2e5232',
    '3px -3px 0 #2e5232',
    '-3px 3px 0 #2e5232',
    '3px 3px 0 #2e5232',
    '6px 8px 0 rgba(46, 82, 50, 0.4)',
  ].join(', '),
} as const

const heroTaglineSx = {
  px: 2,
  py: 0.5,
  borderRadius: 999,
  backgroundColor: 'rgba(255, 255, 255, 0.82)',
  color: '#2e5232',
  fontWeight: 700,
} as const

const heroSpriteRowSx = {
  position: 'absolute',
  bottom: { xs: 12, md: 28 },
  left: 0,
  right: 0,
  display: 'flex',
  justifyContent: 'center',
  alignItems: 'flex-end',
  gap: { xs: 2, md: 3 },
  pointerEvents: 'none',
} as const

const authCardSx = {
  width: '100%',
  maxWidth: 440,
  borderRadius: 4,
  border: '1px solid #e7d9b6',
  backgroundColor: '#ffffff',
  boxShadow: '0 12px 32px rgba(79, 70, 56, 0.12)',
  p: { xs: 2.5, sm: 3.5 },
} as const

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
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'grid',
        gridTemplateColumns: { xs: '1fr', md: '1.15fr 1fr' },
        gridTemplateRows: { xs: 'auto 1fr', md: '1fr' },
      }}
    >
      <Box
        sx={{
          position: 'relative',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          overflow: 'hidden',
          minHeight: { xs: 300, md: '100dvh' },
          backgroundImage: `url(${heroBackgroundUrl})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center bottom',
        }}
      >
        <Stack
          alignItems="center"
          spacing={2}
          sx={{ px: 3, pb: { xs: 14, md: 20 }, pt: { xs: 6, md: 0 }, textAlign: 'center' }}
        >
          <Typography component="h1" sx={heroTitleSx}>
            {locale.heroTitle}
          </Typography>
          <Typography variant="body1" sx={heroTaglineSx}>
            {locale.heroTagline}
          </Typography>
        </Stack>

        <Box sx={heroSpriteRowSx}>
          <Box
            component="img"
            src={heroEnemyUrl}
            alt=""
            sx={{ height: { xs: 48, md: 64 }, imageRendering: 'pixelated' }}
          />
          <Box component="img" src={heroCharacterUrl} alt="" sx={{ height: { xs: 96, md: 136 } }} />
          <Box component="img" src={heroCompanionUrl} alt="" sx={{ height: { xs: 84, md: 120 } }} />
        </Box>
      </Box>

      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          px: { xs: 2, sm: 4 },
          py: { xs: 4, md: 6 },
          background: 'linear-gradient(180deg, #fffdf8 0%, #f5edd8 100%)',
        }}
      >
        <Paper elevation={0} sx={authCardSx}>
          <Stack spacing={2}>
            <Typography variant="h5" component="h2" textAlign="center" fontWeight={700}>
              {locale.cardTitle}
            </Typography>

            {message ? <Alert severity="info">{message}</Alert> : null}

            <Button
              type="button"
              variant="contained"
              size="large"
              disabled={isAnySubmitting}
              onClick={handleAnonymousSignIn}
              sx={{ ...softGoldButtonSx, py: 1.3 }}
            >
              {submitMode === 'anonymous' ? locale.anonymousSubmitting : locale.anonymousSubmit}
            </Button>
            <Typography variant="caption" color="text.secondary" sx={{ whiteSpace: 'pre-line', textAlign: 'center' }}>
              {locale.anonymousDescription}
            </Typography>

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
          </Stack>
        </Paper>
      </Box>
    </Box>
  )
}

export default Auth
