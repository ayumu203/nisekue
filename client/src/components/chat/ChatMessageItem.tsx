import CloseIcon from '@mui/icons-material/Close'
import { Avatar, Box, IconButton, Stack, Typography } from '@mui/material'
import { Link as RouterLink } from 'react-router-dom'
import type { GetChatRoomResponse } from '@/schema/chat'
import { resolveCharacterAssetPath } from '@/lib/assets'
import locale from '../../../locale/chat/Chat.json'
import { font, palette, withAlpha } from '@/theme/tokens'

type ChatMessage = GetChatRoomResponse['messages'][number]

type Props = {
  message: ChatMessage
  isOwnMessage: boolean
  onDelete?: () => void
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

function ChatMessageItem({ message, isOwnMessage, onDelete }: Props) {
  const avatarSrc = resolveCharacterAssetPath(message.imagePath)
  const visitPlayerPath = !isOwnMessage && message.senderId ? `/players/${message.senderId}/visit` : null
  const canDelete = isOwnMessage && message.senderType === 'Player' && onDelete !== undefined

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
            fontSize: font.sm,
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
                  boxShadow: `0 8px 18px ${withAlpha(palette.brown900, 0.25)}`,
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
            bgcolor: isOwnMessage ? palette.green400 : 'grey.200',
            color: isOwnMessage ? palette.white : 'text.primary',
          }}
        >
          <Typography
            variant="caption"
            color={isOwnMessage ? withAlpha(palette.white, 0.75) : 'text.secondary'}
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
      {canDelete ? (
        <IconButton
          size="small"
          aria-label={locale.deleteMessage}
          title={locale.deleteMessage}
          onClick={onDelete}
          sx={{ alignSelf: 'flex-start', p: 0.25, color: 'text.secondary' }}
        >
          <CloseIcon sx={{ fontSize: font.md }} />
        </IconButton>
      ) : null}
      {isOwnMessage ? (
        <Avatar
          src={avatarSrc ?? undefined}
          alt={message.senderName}
          imgProps={{ referrerPolicy: 'no-referrer' }}
          sx={{
            width: 45,
            height: 45,
            bgcolor: palette.green400,
            fontSize: font.sm,
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
