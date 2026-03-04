import { useState } from 'react'
import { Alert, Box, CircularProgress, Container, Divider, Paper, Stack, Typography } from '@mui/material'
import { Navigate } from 'react-router-dom'
import SignIn from '../components/auth/SignIn'
import SignUp from '../components/auth/SignUp'
import { useAuth } from '../contexts/useAuth'

function Auth() {
  const { user, isLoading } = useAuth()
  const [message, setMessage] = useState<string | null>(null)

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>Loading auth state...</Typography>
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
          <Typography variant="h4">Authentication</Typography>
          {message ? <Alert severity="info">{message}</Alert> : null}
          <Stack spacing={3}>
            <SignUp onMessage={setMessage} />
            <Divider />
            <SignIn onMessage={setMessage} />
          </Stack>
        </Stack>
      </Paper>
    </Container>
  )
}

export default Auth
