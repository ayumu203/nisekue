import { useState } from 'react'
import type { FormEvent } from 'react'
import { Alert, Box, Button, Stack, TextField, Typography } from '@mui/material'
import { supabase } from '../../lib/supabase'
import { createPlayer } from '../../api/player'

type SignUpProps = {
  onMessage?: (message: string) => void
}

function SignUp({ onMessage }: SignUpProps) {
  const [userName, setUserName] = useState('')
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSubmit = async (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    setIsSubmitting(true)
    setError(null)

    const { data, error: signUpError } = await supabase.auth.signUp({
      email,
      password,
    })

    if (signUpError) {
      setError(signUpError.message)
      onMessage?.('ユーザー登録に失敗しました')
      setIsSubmitting(false)
      return
    }

    const accessToken = data.session?.access_token

    if (!accessToken) {
      onMessage?.('ユーザー登録が完了しました。ログイン後にユーザー名を登録してください。')
      setIsSubmitting(false)
      return
    }

    try {
      await createPlayer({ userName }, accessToken)
      onMessage?.('ユーザー登録とユーザー名登録が完了しました。')
    } catch (createPlayerError) {
      const message =
        createPlayerError instanceof Error ? createPlayerError.message : 'ユーザー名の登録に失敗しました'
      setError(message)
      onMessage?.('ユーザー登録後のユーザー名登録に失敗しました。')
    }

    setIsSubmitting(false)
  }

  return (
    <Box component="form" onSubmit={handleSubmit} noValidate>
      <Stack spacing={2}>
        <Typography variant="h6">Sign Up</Typography>
        <TextField
          id="sign-up-user-name"
          label="User Name"
          autoComplete="username"
          value={userName}
          onChange={(event) => setUserName(event.target.value)}
          required
          fullWidth
        />
        <TextField
          id="sign-up-email"
          label="Email"
          type="email"
          autoComplete="email"
          value={email}
          onChange={(event) => setEmail(event.target.value)}
          required
          fullWidth
        />
        <TextField
          id="sign-up-password"
          label="Password"
          type="password"
          autoComplete="new-password"
          value={password}
          onChange={(event) => setPassword(event.target.value)}
          required
          fullWidth
        />
        <Button type="submit" variant="contained" disabled={isSubmitting}>
          {isSubmitting ? 'Creating...' : 'Create account'}
        </Button>
        {error ? <Alert severity="error">{error}</Alert> : null}
      </Stack>
    </Box>
  )
}

export default SignUp
