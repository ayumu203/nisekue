import { Alert, Box, Button, CircularProgress, Container, Paper, Snackbar, Stack, Typography } from '@mui/material'
import { useEffect, useEffectEvent, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import { getChatRoom, markChatMessagesAlerted, postChatMessage } from '@/api/chat'
import { getThreadAlerts, markThreadRepliesAlerted } from '@/api/thread'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/home/Home.json'
import ChatMessages from '@/components/chat/ChatMessages'
import ChatForm from '@/components/chat/ChatForm'
import BeginnerGuide from '@/components/common/BeginnerGuide'
import Status from '@/components/home/Status'
import {
  ItemsIcon,
  JobChangeIcon,
  MoveSettingIcon,
  QuestIcon,
  RebirthIcon,
  RankingIcon,
  SpecialThanksIcon,
  ThreadsIcon,
  TrainingIcon,
  TreasureMapIcon,
  VisitPlayersIcon,
} from '@/components/home/HomeIcons'
import {
  innerSurfaceSx,
  menuButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
  twoColumnContentGridSx,
} from '@/constants/styles'
import { beginnerGuides } from '@/lib/beginnerGuides'

function Home() {
  const { session, isLoading, isAnonymous } = useAuth()
  const [toastQueue, setToastQueue] = useState<string[]>([])
  const handledChatIdsRef = useRef<Set<number>>(new Set())
  const handledReplyIdsRef = useRef<Set<string>>(new Set())
  const enqueueToast = useEffectEvent((message: string) => {
    setToastQueue((current) => [...current, message])
  })
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
  const threadAlertsSWRKey =
    session?.access_token && player?.userId ? (['thread-alerts', player.userId] as const) : null
  const { data: threadAlerts } = useSWR(threadAlertsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getThreadAlerts(session.access_token)
  })

  useEffect(() => {
    if (!session?.access_token || !player?.userId || !chatRoom) {
      return
    }

    const unalertedMessages = chatRoom.messages.filter(
      (message) =>
        !message.isAlerted &&
        !handledChatIdsRef.current.has(message.chatId) &&
        (message.senderId == null || message.senderId !== player.userId),
    )

    if (unalertedMessages.length === 0) {
      return
    }

    unalertedMessages.forEach((message) => handledChatIdsRef.current.add(message.chatId))
    enqueueToast(locale.newMessageToast)
    void markChatMessagesAlerted(
      {
        ownerId: player.userId,
        chatIds: unalertedMessages.map((message) => message.chatId),
      },
      session.access_token,
    ).catch(() => {
      unalertedMessages.forEach((message) => handledChatIdsRef.current.delete(message.chatId))
    })
  }, [chatRoom, player?.userId, session?.access_token])

  useEffect(() => {
    if (!session?.access_token || !threadAlerts) {
      return
    }

    const pendingReplyIds = threadAlerts.items
      .flatMap((item) => item.replyIds)
      .filter((replyId) => !handledReplyIdsRef.current.has(replyId))

    if (pendingReplyIds.length === 0) {
      return
    }

    pendingReplyIds.forEach((replyId) => handledReplyIdsRef.current.add(replyId))
    enqueueToast(locale.newThreadReplyToast)
    void markThreadRepliesAlerted({ replyIds: pendingReplyIds }, session.access_token).catch(() => {
      pendingReplyIds.forEach((replyId) => handledReplyIdsRef.current.delete(replyId))
    })
  }, [session?.access_token, threadAlerts])

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
                <Status
                  player={player}
                  actionAlign="start"
                  topAction={
                    <BeginnerGuide
                      userId={session?.user.id}
                      guide={beginnerGuides.home}
                      triggerSx={{
                        minHeight: 44,
                        px: 2,
                        fontWeight: 700,
                      }}
                    />
                  }
                />
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
                to="/treasure-map"
                variant="contained"
                startIcon={<TreasureMapIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.treasureMap}
              </Button>
              <Button
                component={Link}
                to="/items"
                variant="contained"
                startIcon={<ItemsIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.items}
              </Button>
              <Button
                component={Link}
                to="/move-setting"
                variant="contained"
                startIcon={<MoveSettingIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.moveSetting}
              </Button>
              <Button
                component={Link}
                to="/job-change"
                variant="contained"
                startIcon={<JobChangeIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.jobChange}
              </Button>
              <Button
                component={Link}
                to="/rebirth"
                variant="contained"
                startIcon={<RebirthIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.rebirth}
              </Button>
              <Button
                component={Link}
                to="/players"
                variant="outlined"
                startIcon={<VisitPlayersIcon />}
                sx={menuButtonSx}
              >
                {locale.visitPlayers}
              </Button>
              <Button component={Link} to="/threads" variant="outlined" startIcon={<ThreadsIcon />} sx={menuButtonSx}>
                {locale.threads}
              </Button>
              <Button component={Link} to="/ranking" variant="outlined" startIcon={<RankingIcon />} sx={menuButtonSx}>
                {locale.ranking}
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
                      disabled={isAnonymous}
                      disabledReason={isAnonymous ? locale.anonymousPostingRestricted : null}
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
      <Snackbar
        open={toastQueue.length > 0}
        autoHideDuration={1500}
        onClose={(_, reason) => {
          if (reason === 'clickaway') {
            return
          }

          setToastQueue((current) => current.slice(1))
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity="info"
          variant="filled"
          onClose={() => setToastQueue((current) => current.slice(1))}
          sx={{ width: '100%', alignItems: 'center' }}
        >
          {toastQueue[0] ?? ''}
        </Alert>
      </Snackbar>
    </Container>
  )
}

export default Home
