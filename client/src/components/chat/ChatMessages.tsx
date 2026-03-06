import { Alert, Stack, Typography } from '@mui/material'
import type { GetChatRoomResponse } from '@/schema/chat'
import ChatMessageItem from '@/components/chat/ChatMessageItem'

type Props = {
  messages: GetChatRoomResponse['messages']
}

function ChatMessages({ messages }: Props) {
  if (messages.length === 0) {
    return <Alert severity="info">まだメッセージはありません。</Alert>
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="caption" color="text.secondary">
        {messages.length}件
      </Typography>
      {messages.map((message) => (
        <ChatMessageItem key={message.chatId} message={message} />
      ))}
    </Stack>
  )
}

export default ChatMessages
