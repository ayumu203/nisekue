import { Alert, FormControlLabel, Stack, Switch, Typography } from '@mui/material'
import { useState } from 'react'
import type { GetChatRoomResponse } from '@/schema/chat'
import ChatMessageItem from '@/components/chat/ChatMessageItem'
import { getShowSystemMessages, saveShowSystemMessages } from '@/lib/chatPreferencesStorage'
import locale from '../../../locale/chat/Chat.json'

type Props = {
  messages: GetChatRoomResponse['messages']
  currentPlayerId: string
}

function ChatMessages({ messages, currentPlayerId }: Props) {
  const [showSystemMessages, setShowSystemMessages] = useState(() => getShowSystemMessages(currentPlayerId))

  if (messages.length === 0) {
    return <Alert severity="info">まだメッセージはありません。</Alert>
  }

  const filteredMessages = showSystemMessages ? messages : messages.filter((message) => message.senderType !== 'System')
  const reversedMessages = [...filteredMessages].reverse()

  return (
    <Stack spacing={1.5}>
      <FormControlLabel
        control={
          <Switch
            size="small"
            checked={showSystemMessages}
            onChange={(event) => {
              setShowSystemMessages(event.target.checked)
              saveShowSystemMessages(currentPlayerId, event.target.checked)
            }}
          />
        }
        label={locale.showSystemMessages}
      />
      <Typography variant="caption" color="text.secondary">
        {filteredMessages.length}件
      </Typography>
      {reversedMessages.length === 0 ? (
        <Alert severity="info">{locale.noVisibleMessages}</Alert>
      ) : (
        reversedMessages.map((message) => (
          <ChatMessageItem key={message.chatId} message={message} isOwnMessage={message.senderId === currentPlayerId} />
        ))
      )}
    </Stack>
  )
}

export default ChatMessages
