import { useState } from 'react'
import type { FormEvent } from 'react'
import { Alert, Box, Button, Stack, TextField } from '@mui/material'
import { greenOutlinedInputSx, softGreenButtonSx } from '@/constants/styles'
import { supabase } from '@/lib/supabase'
import locale from '../../../locale/auth/SignUp.json'

type SignUpProps = {
  onMessage?: (message: string) => void
}

function SignUp({ onMessage }: SignUpProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

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
      setIsSubmitting(false)
      return
    }

    if (!data.session?.access_token) {
      onMessage?.(locale.toastNeedsEmailConfirm)
      setIsSubmitting(false)
      return
    }

    onMessage?.(locale.toastSuccess)

    setIsSubmitting(false)
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
        <Button type="submit" variant="contained" disabled={isSubmitting} sx={softGreenButtonSx}>
          {isSubmitting ? locale.submitting : locale.submit}
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : null}
      </Stack>
    </Box>
  )
}

export default SignUp
