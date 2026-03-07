import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Container,
  Paper,
  Stack,
  SvgIcon,
  Typography,
  type SvgIconProps,
} from '@mui/material'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import SignOut from '@/components/auth/SignOut'
import { createPlayer, getPlayer } from '@/api/player'
import { getChatRoom, postChatMessage } from '@/api/chat'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/home/Home.json'
import ChatMessages from '@/components/chat/ChatMessages'
import ChatForm from '@/components/chat/ChatForm'
import Status from '@/components/home/Status'

function TrainingIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M11 21h-1l1-7H7.5a.5.5 0 0 1-.39-.81L13 3h1l-1 7h3.5c.4 0 .64.45.39.76z" />
    </SvgIcon>
  )
}

function PlayerSettingIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M12 12a4 4 0 1 0-4-4 4 4 0 0 0 4 4m0 2c-3.31 0-6 1.79-6 4v2h12v-2c0-2.21-2.69-4-6-4" />
    </SvgIcon>
  )
}

function SpecialThanksIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="m12 17.27 6.18 3.73-1.64-7.03L22 9.24l-7.19-.61L12 2 9.19 8.63 2 9.24l5.46 4.73L5.82 21z" />
    </SvgIcon>
  )
}

function Home() {
  const { session, isLoading } = useAuth()
  const menuButtonSx = {
    width: '100%',
    minHeight: 48,
    whiteSpace: 'nowrap',
  }
  const playerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
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
  const chatSWRKey = session?.access_token && player?.userId ? (['chat-room', player.userId] as const) : null
  const {
    data: chatRoom,
    error: chatError,
    isLoading: isChatLoading,
    isValidating: isChatValidating,
    mutate: mutateChatRoom,
  } = useSWR(chatSWRKey, async () => {
    if (!session?.access_token || !player?.userId) {
      throw new Error(locale.chatFetchInfoMissing)
    }

    return getChatRoom({ ownerId: player.userId }, session.access_token)
  })

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={{ p: { xs: 2, sm: 4 } }}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: '1fr',
                sm: 'minmax(180px, 260px) minmax(0, 1fr)',
                md: 'minmax(280px, 360px) minmax(0, 1fr)',
              },
              gap: { xs: 1.5, sm: 3 },
              alignItems: 'start',
            }}
          >
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              {isPlayerLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.playerLoading}</Typography>
                </Stack>
              ) : playerError ? (
                <Alert severity="warning">{playerError.message}</Alert>
              ) : (
                <Status player={player} />
              )}
              <Button component={Link} to="/training" variant="contained" startIcon={<TrainingIcon />} sx={menuButtonSx}>
                {locale.training}
              </Button>
              <Button
                component={Link}
                to="/player-setting"
                variant="outlined"
                startIcon={<PlayerSettingIcon />}
                sx={menuButtonSx}
              >
                {locale.playerSetting}
              </Button>
              <Button
                component={Link}
                to="/thanks"
                variant="outlined"
                startIcon={<SpecialThanksIcon />}
                sx={menuButtonSx}
              >
                {locale.specialThanks}
              </Button>
              <SignOut buttonSx={menuButtonSx} />
            </Stack>

            <Paper variant="outlined" sx={{ borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Typography variant="h5">{locale.chatTitle}</Typography>
                {isChatLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.chatLoading}</Typography>
                  </Stack>
                ) : chatError ? (
                  <Alert severity="warning">{chatError.message}</Alert>
                ) : (
                  <Stack spacing={2}>
                    <ChatForm
                      isSubmitting={isChatValidating}
                      onSubmit={async (text) => {
                        if (!session?.access_token || !player?.userId) {
                          throw new Error(locale.sessionInfoMissing)
                        }

                        const updated = await postChatMessage(
                          {
                            ownerId: player.userId,
                            text,
                          },
                          session.access_token,
                        )
                        await mutateChatRoom(updated, { revalidate: false })
                      }}
                    />
                    <ChatMessages messages={chatRoom?.messages ?? []} />
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

export default Home
