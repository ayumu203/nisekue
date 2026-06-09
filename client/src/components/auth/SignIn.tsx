import { useState } from 'react'
import type { FormEvent } from 'react'
import { Alert, Box, Button, Divider, Stack, TextField, Typography } from '@mui/material'
import GoogleIcon from '@mui/icons-material/Google'
import { greenOutlinedInputSx, softGreenButtonSx } from '@/constants/styles'
import { supabase } from '@/lib/supabase'
import locale from '../../../locale/auth/SignIn.json'

type SignInProps = {
  onMessage?: (message: string) => void
}

function SignIn({ onMessage }: SignInProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitMode, setSubmitMode] = useState<'email' | 'google' | null>(null)
  const [error, setError] = useState<string | null>(null)

  const handleGoogleSignIn = async () => {
    setSubmitMode('google')
    setError(null)

    const redirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()

    const { error: googleError } = await supabase.auth.signInWithOAuth({
      provider: 'google',
      options: { redirectTo },
    })

    if (googleError) {
      setError(locale.defaultError)
      onMessage?.(locale.toastFailed)
    }

    setSubmitMode(null)
  }

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSubmitMode('email')
    setError(null)

    const { error: signInError } = await supabase.auth.signInWithPassword({
      email,
      password,
    })

    if (signInError) {
      const normalized = signInError.message.trim().toLowerCase()
      setError(locale.errorMessages[normalized as keyof typeof locale.errorMessages] ?? locale.defaultError)
      onMessage?.(locale.toastFailed)
      setSubmitMode(null)
      return
    }

    onMessage?.(locale.toastSuccess)
    setSubmitMode(null)
  }

  return (
    <Box component="form" onSubmit={handleSubmit} noValidate>
      <Stack spacing={2}>
        <TextField
          id="sign-in-email"
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
          id="sign-in-password"
          label={locale.passwordLabel}
          type="password"
          autoComplete="current-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          fullWidth
          sx={greenOutlinedInputSx}
        />
        <Button type="submit" variant="contained" disabled={submitMode !== null} sx={softGreenButtonSx}>
          {submitMode === 'email' ? locale.submitting : locale.submit}
        </Button>
        <Divider>
          <Typography variant="caption" color="text.secondary">
            または
          </Typography>
        </Divider>
        <Button
          type="button"
          variant="outlined"
          disabled={submitMode !== null}
          startIcon={<GoogleIcon />}
          onClick={handleGoogleSignIn}
          sx={{
            borderColor: '#dadce0',
            color: '#3c4043',
            backgroundColor: '#ffffff',
            textTransform: 'none',
            fontWeight: 500,
            '&:hover': { backgroundColor: '#f8f9fa', borderColor: '#dadce0' },
          }}
        >
          {submitMode === 'google' ? locale.googleSubmitting : locale.googleSubmit}
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : null}
      </Stack>
    </Box>
  )
}

export default SignIn
