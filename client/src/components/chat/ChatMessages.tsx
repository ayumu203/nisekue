import { Alert, Stack, Typography } from '@mui/material'
import type { GetChatRoomResponse } from '../../schema/player'
import ChatMessageItem from './ChatMessageItem'

type Props = {
  messages: GetChatRoomResponse['messages']
  currentUserId: string
}

function ChatMessages({ messages, currentUserId }: Props) {
  if (messages.length === 0) {
    return <Alert severity="info">まだメッセージはありません。</Alert>
  }

  return (
    <Stack spacing={1.5}>
      <Typography variant="caption" color="text.secondary">
        {messages.length}件
      </Typography>
      {messages.map((message) => (
        <ChatMessageItem key={message.chatId} message={message} isMine={message.senderId === currentUserId} />
      ))}
    </Stack>
  )
}

export default ChatMessages
