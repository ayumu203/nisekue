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
import { createPlayer, getPlayer } from '@/api/player'
import { getChatRoom, postChatMessage } from '@/api/chat'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/home/Home.json'
import ChatMessages from '@/components/chat/ChatMessages'
import ChatForm from '@/components/chat/ChatForm'
import Status from '@/components/home/Status'
import {
  innerSurfaceSx,
  menuButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
  twoColumnContentGridSx,
} from '@/constants/styles'

function TrainingIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M11 21h-1l1-7H7.5a.5.5 0 0 1-.39-.81L13 3h1l-1 7h3.5c.4 0 .64.45.39.76z" />
    </SvgIcon>
  )
}

function QuestIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M4 5h16v3H4zm2 5h12l-1 9H7zM10 2h4v2h-4z" />
    </SvgIcon>
  )
}

function MoveSettingIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M4 7h16v2H4zm0 4h10v2H4zm0 4h16v2H4z" />
    </SvgIcon>
  )
}

function JobChangeIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M7 4h7v2H7zM7 8h10v2H7zm0 4h7v2H7zm8.5 1 4.5 4.5-4.5 4.5-1.41-1.41L16.17 18H11v-2h5.17l-2.08-2.09z" />
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

function VisitPlayersIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M9 11a3 3 0 1 0-3-3 3 3 0 0 0 3 3m6 1a2.5 2.5 0 1 0-2.5-2.5A2.5 2.5 0 0 0 15 12m0 1.5c-1.84 0-5.5.92-5.5 2.75V18h11v-1.75C20.5 14.42 16.84 13.5 15 13.5M9 12c-2.33 0-7 1.17-7 3.5V18h5.5v-1.75c0-.83.31-1.58.89-2.21A11 11 0 0 1 9 12" />
    </SvgIcon>
  )
}

function Home() {
  const { session, isLoading } = useAuth()
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
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
              {isPlayerLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.playerLoading}</Typography>
                </Stack>
              ) : playerError ? (
                <Alert severity="warning">{playerError.message}</Alert>
              ) : (
                <Status player={player} actionAlign="start" />
              )}
              <Button
                component={Link}
                to="/quest"
                variant="contained"
                startIcon={<QuestIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.quest}
              </Button>
              <Button
                component={Link}
                to="/training"
                variant="contained"
                startIcon={<TrainingIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.training}
              </Button>
              <Button
                component={Link}
                to="/players"
                variant="contained"
                startIcon={<VisitPlayersIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.visitPlayers}
              </Button>
              <Button
                component={Link}
                to="/move-setting"
                variant="outlined"
                startIcon={<MoveSettingIcon />}
                sx={menuButtonSx}
              >
                {locale.moveSetting}
              </Button>
              <Button
                component={Link}
                to="/job-change"
                variant="outlined"
                startIcon={<JobChangeIcon />}
                sx={menuButtonSx}
              >
                {locale.jobChange}
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
            </Stack>

            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 }, mt: '48px' }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Stack direction="row" justifyContent="space-between" alignItems="center" sx={{ minHeight: 44 }}>
                  <Typography variant="h5">{locale.chatTitle}</Typography>
                </Stack>
                {isChatLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.chatLoading}</Typography>
                  </Stack>
                ) : chatError ? (
                  <Alert severity="warning">{chatError.message}</Alert>
                ) : !player ? (
                  <Alert severity="warning">{locale.chatFetchInfoMissing}</Alert>
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
                    <ChatMessages messages={chatRoom?.messages ?? []} currentPlayerId={player.userId} />
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
