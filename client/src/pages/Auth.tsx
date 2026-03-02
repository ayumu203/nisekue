import { useState } from 'react'
import { Alert, Box, CircularProgress, Container, Divider, Paper, Stack, Typography } from '@mui/material'
import useSWR from 'swr'
import SignIn from '../components/auth/SignIn'
import SignOut from '../components/auth/SignOut'
import SignUp from '../components/auth/SignUp'
import { useAuth } from '../contexts/useAuth'
import { getPlayer } from '../api/player'

function Auth() {
  const { session, user, isLoading } = useAuth()
  const [message, setMessage] = useState<string | null>(null)
  const playerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const { data: player, error: playerError, isLoading: isPlayerLoading } = useSWR(
    playerSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error('セッションが無効です')
      }
      return getPlayer(session.access_token)
    },
  )

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

  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <Stack spacing={3}>
          <Typography variant="h4">Authentication</Typography>
          {message ? <Alert severity="info">{message}</Alert> : null}

          {session ? (
            <Stack spacing={2}>
              <Alert severity="success">Signed in as: {user?.email}</Alert>
              {isPlayerLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">Loading player profile...</Typography>
                </Stack>
              ) : playerError ? (
                <Alert severity="warning">{playerError.message}</Alert>
              ) : (
                <Alert severity="info">User Name: {player?.userName ?? player?.userId ?? '未設定'}</Alert>
              )}
              <SignOut onMessage={setMessage} />
            </Stack>
          ) : (
            <Stack spacing={3}>
              <SignUp onMessage={setMessage} />
              <Divider />
              <SignIn onMessage={setMessage} />
            </Stack>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}

export default Auth
