import { useState } from 'react'
import type { FormEvent } from 'react'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { greenOutlinedInputSx, softGoldButtonSx, softGreenButtonSx } from '@/constants/styles'
import { supabase } from '@/lib/supabase'
import locale from '../../../locale/auth/SignUp.json'

type SignUpProps = {
  onMessage?: (message: string) => void
}

function SignUp({ onMessage }: SignUpProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [submitMode, setSubmitMode] = useState<'email' | 'anonymous' | null>(null)
  const [error, setError] = useState<string | null>(null)
  const minPasswordErrorMessage = locale.errorMessages['password should be at least 6 characters']

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setSubmitMode('email')
    setError(null)

    if (password.length < 6) {
      setError(minPasswordErrorMessage)
      onMessage?.(locale.toastFailed)
      setSubmitMode(null)
      return
    }

    const emailRedirectTo = new URL(import.meta.env.BASE_URL, window.location.origin).toString()

    const { data, error: signUpError } = await supabase.auth.signUp({
      email,
      password,
      options: {
        emailRedirectTo,
      },
    })

    if (signUpError) {
      const normalized = signUpError.message.trim().toLowerCase()
      setError(locale.errorMessages[normalized as keyof typeof locale.errorMessages] ?? locale.defaultError)
      onMessage?.(locale.toastFailed)
      setSubmitMode(null)
      return
    }

    if (!data.session?.access_token) {
      onMessage?.(locale.toastNeedsEmailConfirm)
      setSubmitMode(null)
      return
    }

    onMessage?.(locale.toastSuccess)

    setSubmitMode(null)
  }

  const handleAnonymousSignIn = async () => {
    setSubmitMode('anonymous')
    setError(null)

    const { error: anonymousError } = await supabase.auth.signInAnonymously()

    if (anonymousError) {
      const normalized = anonymousError.message.trim().toLowerCase()
      setError(locale.errorMessages[normalized as keyof typeof locale.errorMessages] ?? locale.defaultError)
      onMessage?.(locale.anonymousToastFailed)
      setSubmitMode(null)
      return
    }

    onMessage?.(locale.anonymousToastSuccess)
    setSubmitMode(null)
  }

  return (
    <Box component="form" onSubmit={handleSubmit} noValidate>
      <Stack spacing={2}>
        <TextField
          id="sign-up-email"
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
          id="sign-up-password"
          label={locale.passwordLabel}
          type="password"
          autoComplete="new-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          fullWidth
          sx={greenOutlinedInputSx}
        />
        <Button type="submit" variant="contained" disabled={submitMode !== null} sx={softGreenButtonSx}>
          {submitMode === 'email' ? locale.submitting : locale.submit}
        </Button>
        <Typography variant="body2" color="text.secondary" sx={{ whiteSpace: 'pre-line', textAlign: 'center' }}>
          {locale.anonymousDescription}
        </Typography>
        <Button
          type="button"
          variant="contained"
          disabled={submitMode !== null}
          onClick={handleAnonymousSignIn}
          sx={softGoldButtonSx}
        >
          {submitMode === 'anonymous' ? locale.anonymousSubmitting : locale.anonymousSubmit}
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : null}
      </Stack>
    </Box>
  )
}

export default SignUp
