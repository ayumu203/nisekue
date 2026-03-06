import { Box, Stack, Typography } from '@mui/material'
import type { GetChatRoomResponse } from '../../schema/player'

type ChatMessage = GetChatRoomResponse['messages'][number]

type Props = {
  message: ChatMessage
  isMine: boolean
}

function formatTime(value: string): string {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('ja-JP', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

function ChatMessageItem({ message, isMine }: Props) {
  return (
    <Stack alignItems={isMine ? 'flex-end' : 'flex-start'}>
      <Box
        sx={{
          maxWidth: '80%',
          px: 1.5,
          py: 1,
          borderRadius: 2,
          bgcolor: isMine ? 'primary.light' : 'grey.200',
          color: isMine ? 'primary.contrastText' : 'text.primary',
        }}
      >
        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {message.text}
        </Typography>
      </Box>
      <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
        #{message.chatId} {formatTime(message.createdAt)}
      </Typography>
    </Stack>
  )
}

export default ChatMessageItem
