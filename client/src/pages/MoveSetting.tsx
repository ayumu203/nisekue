import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import MoveItemBox from '@/components/moveSetting/MoveItemBox'
import { outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/player-setting/PlayerSetting.json'

export default function MoveSetting() {
  const { session, isLoading } = useAuth()

  const playerSWRKey = session?.user.id ? (['move-setting', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
  } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    try {
      return await getPlayer(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''
      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.loadingAuth}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Stack spacing={1} alignItems="flex-end">
            <Button component={Link} to="/" variant="outlined" sx={{ alignSelf: 'flex-end' }}>
              {locale.backToHome}
            </Button>
          </Stack>

          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.loadingPlayer}</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : !player ? (
            <Alert severity="warning">{locale.loadingPlayer}</Alert>
          ) : (
            <MoveItemBox player={player} />
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
