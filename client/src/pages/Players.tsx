import { Alert, Avatar, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, listPlayers } from '@/api/player'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { resolveCharacterAssetPath } from '@/lib/assets'
import locale from '../../locale/players/Players.json'
import { innerSurfaceSx, outerPagePaperSx, softGreenButtonSx } from '@/constants/styles'

function Players() {
  const { session, isLoading } = useAuth()
  const playerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const {
    data: currentPlayer,
    error: currentPlayerError,
    isLoading: isCurrentPlayerLoading,
  } = useSWR(playerSWRKey, async () => {
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

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  const playersSWRKey = session?.access_token ? (['players'] as const) : null
  const {
    data: players,
    error: playersError,
    isLoading: isPlayersLoading,
  } = useSWR(playersSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error('セッションが無効です')
    }

    return listPlayers(session.access_token)
  })

  const visitTargets = players?.filter((player) => player.userId !== currentPlayer?.userId) ?? []

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.loading}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={2}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
            <Typography variant="h4">{locale.title}</Typography>
            <Button component={Link} to="/" variant="outlined">
              {locale.backToHome}
            </Button>
          </Stack>

          {isCurrentPlayerLoading || isPlayersLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.loading}</Typography>
            </Stack>
          ) : currentPlayerError ? (
            <Alert severity="warning">{currentPlayerError.message}</Alert>
          ) : playersError ? (
            <Alert severity="warning">{playersError.message}</Alert>
          ) : visitTargets.length === 0 ? (
            <Alert severity="info">{locale.empty}</Alert>
          ) : (
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
                gap: 2,
              }}
            >
              {visitTargets.map((player) => (
                <Paper key={player.userId} variant="outlined" sx={{ ...innerSurfaceSx, p: 2, borderRadius: 3 }}>
                  <Stack direction="row" spacing={1.5} alignItems="center" justifyContent="space-between">
                    <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
                      <Avatar
                        src={resolveCharacterAssetPath(player.imagePath) ?? undefined}
                        alt={player.userName ?? player.userId}
                        sx={{
                          width: 56,
                          height: 56,
                          bgcolor: 'grey.300',
                          '& .MuiAvatar-img': {
                            objectFit: 'cover',
                            objectPosition: 'center top',
                          },
                        }}
                      >
                        {(player.userName ?? '?').slice(0, 1)}
                      </Avatar>
                      <Box sx={{ minWidth: 0 }}>
                        <Typography variant="h6" sx={{ wordBreak: 'break-word' }}>
                          {player.userName ?? '-'}
                        </Typography>
                      </Box>
                    </Stack>
                    <Button
                      component={Link}
                      to={`/players/${player.userId}/visit`}
                      variant="contained"
                      sx={softGreenButtonSx}
                    >
                      {locale.visit}
                    </Button>
                  </Stack>
                </Paper>
              ))}
            </Box>
          )}
        </Stack>
      </Paper>
    </Container>
  )
}

export default Players
