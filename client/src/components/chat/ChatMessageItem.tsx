import { Box, Stack, Typography } from '@mui/material'
import type { GetChatRoomResponse } from '@/schema/chat'

type ChatMessage = GetChatRoomResponse['messages'][number]

type Props = {
  message: ChatMessage
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

function ChatMessageItem({ message }: Props) {
  return (
    <Stack alignItems="flex-start">
      <Box
        sx={{
          maxWidth: '80%',
          px: 1.5,
          py: 1,
          borderRadius: 2,
          bgcolor: 'grey.200',
          color: 'text.primary',
        }}
      >
        <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.5 }}>
          {message.senderName}
        </Typography>
        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {message.text}
        </Typography>
      </Box>
      <Typography variant="caption" color="text.secondary" sx={{ mt: 0.5 }}>
        {formatTime(message.createdAt)}
      </Typography>
    </Stack>
  )
}

export default ChatMessageItem
