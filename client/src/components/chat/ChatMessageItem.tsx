import { Avatar, Box, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import type { GetChatRoomResponse } from '@/schema/chat'
import { resolveCharacterAssetPath } from '@/lib/assets'

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
  const avatarSrc = resolveCharacterAssetPath(message.imagePath)
  const visitPlayerPath = !isOwnMessage && message.senderId ? `/players/${message.senderId}/visit` : null

  return (
    <Stack direction="row" spacing={1} alignItems="flex-end" justifyContent={isOwnMessage ? 'flex-end' : 'flex-start'}>
      {isOwnMessage ? null : (
        <Avatar
          {...(visitPlayerPath ? { component: RouterLink, to: visitPlayerPath } : {})}
          src={avatarSrc ?? undefined}
          alt={message.senderName}
          imgProps={{ referrerPolicy: 'no-referrer' }}
          sx={{
            width: 45,
            height: 45,
            bgcolor: 'grey.300',
            fontSize: 14,
            flexShrink: 0,
            textDecoration: 'none',
            transition: visitPlayerPath ? 'transform 140ms ease, box-shadow 140ms ease' : 'none',
            '& .MuiAvatar-img': {
              objectFit: 'cover',
              objectPosition: 'center top',
            },
            '&:hover': visitPlayerPath
              ? {
                  transform: 'translateY(-1px)',
                  boxShadow: '0 8px 18px rgba(36, 20, 11, 0.18)',
                }
              : undefined,
          }}
        >
          {message.senderName.slice(0, 1)}
        </Avatar>
      )}
      <Stack alignItems={isOwnMessage ? 'flex-end' : 'flex-start'}>
        <Box
          sx={{
            maxWidth: 'min(calc(80% + 100px), 100%)',
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
      {isOwnMessage ? (
        <Avatar
          src={avatarSrc ?? undefined}
          alt={message.senderName}
          imgProps={{ referrerPolicy: 'no-referrer' }}
          sx={{
            width: 45,
            height: 45,
            bgcolor: '#9ccc9f',
            fontSize: 14,
            flexShrink: 0,
            '& .MuiAvatar-img': {
              objectFit: 'cover',
              objectPosition: 'center top',
            },
          }}
        >
          {message.senderName.slice(0, 1)}
        </Avatar>
      ) : null}
    </Stack>
  )
}

export default ChatMessageItem
