import { Alert, Stack, Typography } from '@mui/material'
import type { GetChatRoomResponse } from '@/schema/chat'
import ChatMessageItem from '@/components/chat/ChatMessageItem'

type Props = {
  messages: GetChatRoomResponse['messages']
  currentPlayerId: string
}

function ChatMessages({ messages, currentPlayerId }: Props) {
  if (messages.length === 0) {
    return <Alert severity="info">まだメッセージはありません。</Alert>
  }

  const reversedMessages = [...messages].reverse()

  return (
    <Stack spacing={1.5}>
      <Typography variant="caption" color="text.secondary">
        {messages.length}件
      </Typography>
      {reversedMessages.map((message) => (
        <ChatMessageItem key={message.chatId} message={message} isOwnMessage={message.senderId === currentPlayerId} />
      ))}
    </Stack>
  )
}

export default ChatMessages
