import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { Link, useParams } from 'react-router-dom'
import useSWR from 'swr'
import { getChatRoom, postChatMessage } from '@/api/chat'
import { createPlayer, getPlayer, getPlayerById } from '@/api/player'
import ChatForm from '@/components/chat/ChatForm'
import ChatMessages from '@/components/chat/ChatMessages'
import Status from '@/components/home/Status'
import { useAuth } from '@/contexts/useAuth'
import { innerSurfaceSx, outerPagePaperSx, twoColumnContentGridSx } from '@/constants/styles'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/visit-player/VisitPlayer.json'

function VisitPlayer() {
  const { playerId } = useParams<{ playerId: string }>()
  const { session, isLoading } = useAuth()

  const currentPlayerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const {
    data: currentPlayer,
    error: currentPlayerError,
    isLoading: isCurrentPlayerLoading,
  } = useSWR(currentPlayerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
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

  const visitedPlayerSWRKey = session?.access_token && playerId ? (['player-visit', playerId] as const) : null
  const {
    data: visitedPlayer,
    error: visitedPlayerError,
    isLoading: isVisitedPlayerLoading,
  } = useSWR(visitedPlayerSWRKey, async () => {
    if (!session?.access_token || !playerId) {
      throw new Error(locale.playerNotFound)
    }

    return getPlayerById(playerId, session.access_token)
  })

  const chatSWRKey = session?.access_token && playerId ? (['chat-room-visit', playerId] as const) : null
  const {
    data: chatRoom,
    error: chatError,
    isLoading: isChatLoading,
    isValidating: isChatValidating,
    mutate: mutateChatRoom,
  } = useSWR(chatSWRKey, async () => {
    if (!session?.access_token || !playerId) {
      throw new Error(locale.playerNotFound)
    }

    return getChatRoom({ ownerId: playerId }, session.access_token)
  })

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
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} justifyContent="space-between" alignItems={{ xs: 'stretch', sm: 'center' }}>
            <Typography variant="h4">
              {visitedPlayer?.userName ? `${visitedPlayer.userName}${locale.chatTitleSuffix}` : locale.loading}
            </Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <Button component={Link} to="/players" variant="outlined">
                {locale.backToPlayers}
              </Button>
              <Button component={Link} to="/" variant="outlined">
                {locale.backToHome}
              </Button>
            </Stack>
          </Stack>

          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              {isVisitedPlayerLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.loading}</Typography>
                </Stack>
              ) : visitedPlayerError ? (
                <Alert severity="warning">{visitedPlayerError.message}</Alert>
              ) : (
                <Status player={visitedPlayer} />
              )}
            </Stack>

            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Typography variant="h5">
                  {visitedPlayer?.userName ? `${visitedPlayer.userName}${locale.chatTitleSuffix}` : locale.loading}
                </Typography>
                {isCurrentPlayerLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.loading}</Typography>
                  </Stack>
                ) : currentPlayerError ? (
                  <Alert severity="warning">{currentPlayerError.message}</Alert>
                ) : isChatLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.loading}</Typography>
                  </Stack>
                ) : chatError ? (
                  <Alert severity="warning">{chatError.message}</Alert>
                ) : !currentPlayer || !playerId ? (
                  <Alert severity="warning">{locale.sessionInfoMissing}</Alert>
                ) : (
                  <Stack spacing={2}>
                    <ChatForm
                      isSubmitting={isChatValidating}
                      onSubmit={async (text) => {
                        if (!session?.access_token) {
                          throw new Error(locale.sessionInfoMissing)
                        }

                        const updated = await postChatMessage(
                          {
                            ownerId: playerId,
                            text,
                          },
                          session.access_token,
                        )
                        await mutateChatRoom(updated, { revalidate: false })
                      }}
                    />
                    <ChatMessages messages={chatRoom?.messages ?? []} currentPlayerId={currentPlayer.userId} />
                  </Stack>
                )}
              </Stack>
            </Paper>
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}

export default VisitPlayer
