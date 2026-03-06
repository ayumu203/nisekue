import { useState } from 'react'
import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { Navigate } from 'react-router-dom'
import SignIn from '@/components/auth/SignIn'
import SignUp from '@/components/auth/SignUp'
import { useAuth } from '@/contexts/useAuth'
import commonLocale from '../../locale/auth/Common.json'
import signInLocale from '../../locale/auth/SignIn.json'
import signUpLocale from '../../locale/auth/SignUp.json'

function Auth() {
  const { user, isLoading } = useAuth()
  const [message, setMessage] = useState<string | null>(null)
  const [mode, setMode] = useState<'signIn' | 'signUp'>('signIn')

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{commonLocale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  if (user) {
    return <Navigate to="/" replace />
  }

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <Stack spacing={3}>
          <Typography variant="h4">{mode === 'signIn' ? signInLocale.title : signUpLocale.title}</Typography>
          {message ? <Alert severity="info">{message}</Alert> : null}

          {mode === 'signIn' ? <SignIn onMessage={setMessage} /> : <SignUp onMessage={setMessage} />}

          <Button
            variant="text"
            onClick={() => {
              setMessage(null)
              setMode((current) => (current === 'signIn' ? 'signUp' : 'signIn'))
            }}
            sx={{ alignSelf: 'flex-start', p: 0 }}
          >
            {mode === 'signIn' ? signInLocale.switchLink : signUpLocale.switchLink}
          </Button>
        </Stack>
      </Paper>
    </Container>
  )
}

export default Auth
