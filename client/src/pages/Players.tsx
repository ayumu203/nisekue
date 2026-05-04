import { Alert, Avatar, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { alpha } from '@mui/material/styles'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, listPlayers } from '@/api/player'
import { PLAYER_DEDUPING_INTERVAL } from '@/constants/swr'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { resolveRankColor } from '@/lib/playerRank'
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
      throw new Error(locale.sessionInfoMissing)
    }

    try {
      return await getPlayer(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''

      if (!message.includes(locale.playerNotFoundMessage)) {
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
  } = useSWR(
    playersSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionInfoMissing)
      }

      return listPlayers(session.access_token)
    },
    { dedupingInterval: PLAYER_DEDUPING_INTERVAL },
  )

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
      <Paper
        elevation={2}
        sx={{
          ...outerPagePaperSx,
          backgroundColor: '#bfe5dd',
        }}
      >
        <Stack spacing={2}>
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.backToHome} />
          </Stack>
          <Paper
            variant="outlined"
            sx={{
              ...innerSurfaceSx,
              borderRadius: 3,
              p: { xs: 1.75, sm: 2.25 },
              color: '#274d54',
              backgroundColor: '#f8fffd',
              borderColor: 'rgba(126, 189, 181, 0.42)',
            }}
          >
            <Stack spacing={2}>
              <Stack spacing={0.5} alignItems="flex-start">
                <Typography variant="overline" sx={{ color: 'rgba(57, 103, 109, 0.72)', letterSpacing: '0.18em' }}>
                  PLAYERS ROOM
                </Typography>
                <Typography variant="h4" fontWeight={900} sx={{ color: '#2f646c' }}>
                  {locale.title}
                </Typography>
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
                  {visitTargets.map((player) =>
                    (() => {
                      const rankColor = resolveRankColor(player.combatIndexRank) ?? '#68b7a7'

                      return (
                        <Paper
                          key={player.userId}
                          variant="outlined"
                          sx={{
                            ...innerSurfaceSx,
                            p: 2,
                            borderRadius: 3,
                            backgroundColor: alpha(rankColor, 0.12),
                            borderColor: alpha(rankColor, 0.34),
                          }}
                        >
                          <Stack direction="row" spacing={1.5} alignItems="center" justifyContent="space-between">
                            <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
                              <Avatar
                                src={resolveCharacterAssetPath(player.imagePath) ?? undefined}
                                alt={player.userName ?? player.userId}
                                sx={{
                                  width: 56,
                                  height: 56,
                                  bgcolor: alpha(rankColor, 0.14),
                                  color: '#2f646c',
                                  border: '1px solid',
                                  borderColor: alpha(rankColor, 0.28),
                                  '& .MuiAvatar-img': {
                                    objectFit: 'cover',
                                    objectPosition: 'center top',
                                  },
                                }}
                              >
                                {(player.userName ?? '?').slice(0, 1)}
                              </Avatar>
                              <Box sx={{ minWidth: 0 }}>
                                <Stack
                                  direction="row"
                                  spacing={1}
                                  alignItems="center"
                                  useFlexGap
                                  flexWrap="wrap"
                                  sx={{ minWidth: 0 }}
                                >
                                  <Typography
                                    variant="h6"
                                    fontWeight={800}
                                    sx={{ wordBreak: 'break-word', color: '#2f646c' }}
                                  >
                                    {player.userName ?? '-'}
                                  </Typography>
                                  <Box
                                    component="span"
                                    sx={{
                                      minWidth: 52,
                                      px: 1.25,
                                      py: 0.65,
                                      display: 'inline-flex',
                                      alignItems: 'center',
                                      justifyContent: 'center',
                                      borderRadius: 999,
                                      border: '1px solid',
                                      borderColor: alpha(rankColor, 0.38),
                                      backgroundColor: alpha(rankColor, 0.14),
                                      color: rankColor,
                                      fontSize: '0.95rem',
                                      fontWeight: 900,
                                      lineHeight: 1,
                                    }}
                                  >
                                    {player.combatIndexRank}
                                  </Box>
                                </Stack>
                                <Typography variant="body2" sx={{ color: 'rgba(47, 100, 108, 0.76)' }}>
                                  {`${player.job.displayName} / Lv.${player.level}`}
                                </Typography>
                              </Box>
                            </Stack>
                            <Stack spacing={1.25} alignItems="flex-end" sx={{ flexShrink: 0 }}>
                              <Button
                                component={Link}
                                to={`/players/${player.userId}/visit`}
                                variant="contained"
                                sx={{
                                  ...softGreenButtonSx,
                                  color: '#fff',
                                  backgroundColor: '#68b7a7',
                                  boxShadow: 'none',
                                  '&:hover': {
                                    backgroundColor: '#75c3b4',
                                    boxShadow: 'none',
                                  },
                                }}
                              >
                                {locale.visit}
                              </Button>
                            </Stack>
                          </Stack>
                        </Paper>
                      )
                    })(),
                  )}
                </Box>
              )}
            </Stack>
          </Paper>
        </Stack>
      </Paper>
    </Container>
  )
}

export default Players
