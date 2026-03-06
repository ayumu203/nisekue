import { useState } from 'react'
import type { FormEvent } from 'react'
import { Alert, Box, Button, Stack, TextField } from '@mui/material'
import { supabase } from '@/lib/supabase'
import locale from '../../../locale/auth/SignIn.json'

type SignInProps = {
  onMessage?: (message: string) => void
}

function SignIn({ onMessage }: SignInProps) {
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    const { error: signInError } = await supabase.auth.signInWithPassword({
      email,
      password,
    })

    if (signInError) {
      const normalized = signInError.message.trim().toLowerCase()
      setError(locale.errorMessages[normalized as keyof typeof locale.errorMessages] ?? locale.defaultError)
      onMessage?.(locale.toastFailed)
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
          id="sign-in-email"
          label={locale.emailLabel}
          type="email"
          autoComplete="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          fullWidth
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
        />
        <Button type="submit" variant="contained" disabled={isSubmitting}>
          {isSubmitting ? locale.submitting : locale.submit}
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : null}
      </Stack>
    </Box>
  )
}

export default SignIn
