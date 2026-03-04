import { Alert, Box, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import useSWR from 'swr'
import SignOut from '../components/auth/SignOut'
import { createPlayer, getPlayer } from '../api/player'
import { useAuth } from '../contexts/useAuth'

function toDefaultUserName(email: string | undefined): string {
  const fallback = 'player'
  if (!email) {
    return fallback
  }

  const local = email.split('@')[0]?.trim() ?? ''
  const normalized = local.replace(/\s+/g, '').slice(0, 20)
  return normalized.length > 0 ? normalized : fallback
}

function Home() {
  const { session, user, isLoading } = useAuth()
  const playerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const { data: player, error: playerError, isLoading: isPlayerLoading } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error('セッションが無効です')
    }

    try {
      return await getPlayer(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''

      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: toDefaultUserName(user?.email) }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

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
        <Stack spacing={2}>
          <Typography variant="h4">Player</Typography>
          <Alert severity="success">Signed in as: {user?.email}</Alert>
          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">Loading player profile...</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : (
            <Alert severity="info">
              User Name: {player?.userName ?? '未設定'}
              <br />
              Level: {player?.level ?? '-'}
              <br />
              Exp: {player?.exp ?? '-'}
              <br />
              MaxHp: {player?.status.maxHp ?? '-'}
              <br />
              MaxMp: {player?.status.maxMp ?? '-'}
              <br />
              Strength: {player?.status.strength ?? '-'}
              <br />
              Defense: {player?.status.defense ?? '-'}
              <br />
              Intelligence: {player?.status.intelligence ?? '-'}
              <br />
              Luck: {player?.status.luck ?? '-'}
              <br />
              Speed: {player?.status.speed ?? '-'}
            </Alert>
          )}
          <SignOut />
        </Stack>
      </Paper>
    </Container>
  )
}

export default Home
