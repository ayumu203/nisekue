import { Box, Stack, Typography } from '@mui/material'
import type { GetChatRoomResponse } from '@/schema/chat'

type ChatMessage = GetChatRoomResponse['messages'][number]

type Props = {
  message: ChatMessage
  isOwnMessage: boolean
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

function ChatMessageItem({ message, isOwnMessage }: Props) {
  return (
    <Stack alignItems={isOwnMessage ? 'flex-end' : 'flex-start'}>
      <Box
        sx={{
          maxWidth: '80%',
          px: 1.5,
          py: 1,
          borderRadius: 2,
          bgcolor: isOwnMessage ? '#78c27d' : 'grey.200',
          color: isOwnMessage ? '#ffffff' : 'text.primary',
        }}
      >
        <Typography
          variant="caption"
          color={isOwnMessage ? 'rgba(255, 255, 255, 0.82)' : 'text.secondary'}
          sx={{ display: 'block', mb: 0.5 }}
        >
          {message.senderName}
        </Typography>
        <Typography variant="body2" sx={{ whiteSpace: 'pre-wrap', wordBreak: 'break-word' }}>
          {message.text}
        </Typography>
      </Box>
      <Typography
        variant="caption"
        color="text.secondary"
        sx={{ mt: 0.5, textAlign: isOwnMessage ? 'right' : 'left' }}
      >
        {formatTime(message.createdAt)}
      </Typography>
    </Stack>
  )
}

export default ChatMessageItem
