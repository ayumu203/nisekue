import { Alert, Box, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import useSWR from 'swr'
import SignOut from '@/components/auth/SignOut'
import { createPlayer, getPlayer } from '@/api/player'
import { getChatRoom, postChatMessage } from '@/api/chat'
import { useAuth } from '@/contexts/useAuth'
import locale from '../../locale/home/Home.json'
import ChatMessages from '@/components/chat/ChatMessages'
import ChatForm from '@/components/chat/ChatForm'

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

      await createPlayer({ userName: toDefaultUserName(user?.email) }, session.access_token)
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
      throw new Error('チャット情報の取得に必要な情報が不足しています')
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
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <Stack spacing={2}>
          <Typography variant="h4">{locale.title}</Typography>
          <Alert severity="success">
            {locale.signedInAs}: {user?.email}
          </Alert>
          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.playerLoading}</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : (
            <Alert severity="info">
              {locale.labels.userName}: {player?.userName ?? locale.notSet}
              <br />
              {locale.labels.level}: {player?.level ?? locale.unknownValue}
              <br />
              {locale.labels.exp}: {player?.exp ?? locale.unknownValue}
              <br />
              {locale.labels.maxHp}: {player?.status.maxHp ?? locale.unknownValue}
              <br />
              {locale.labels.maxMp}: {player?.status.maxMp ?? locale.unknownValue}
              <br />
              {locale.labels.strength}: {player?.status.strength ?? locale.unknownValue}
              <br />
              {locale.labels.defense}: {player?.status.defense ?? locale.unknownValue}
              <br />
              {locale.labels.intelligence}: {player?.status.intelligence ?? locale.unknownValue}
              <br />
              {locale.labels.luck}: {player?.status.luck ?? locale.unknownValue}
              <br />
              {locale.labels.speed}: {player?.status.speed ?? locale.unknownValue}
            </Alert>
          )}
          <Typography variant="h5">チャット</Typography>
          {isChatLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">チャットを読み込み中...</Typography>
            </Stack>
          ) : chatError ? (
            <Alert severity="warning">{chatError.message}</Alert>
          ) : (
            <Stack spacing={2}>
              <ChatMessages messages={chatRoom?.messages ?? []} />
              <ChatForm
                isSubmitting={isChatValidating}
                onSubmit={async (text) => {
                  if (!session?.access_token || !player?.userId || !session.user.id) {
                    throw new Error('セッション情報が不足しています')
                  }

                  const updated = await postChatMessage(
                    {
                      ownerId: player.userId,
                      senderId: session.user.id,
                      text,
                    },
                    session.access_token,
                  )
                  await mutateChatRoom(updated, { revalidate: false })
                }}
              />
            </Stack>
          )}
          <SignOut />
        </Stack>
      </Paper>
    </Container>
  )
}

export default Home
