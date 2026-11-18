import {
  Alert,
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControlLabel,
  Stack,
  Switch,
  Typography,
} from '@mui/material'
import { useState } from 'react'
import type { GetChatRoomResponse } from '@/schema/chat'
import ChatMessageItem from '@/components/chat/ChatMessageItem'
import { getShowSystemMessages, saveShowSystemMessages } from '@/lib/chatPreferencesStorage'
import locale from '../../../locale/chat/Chat.json'

type Props = {
  messages: GetChatRoomResponse['messages']
  currentPlayerId: string
  // 渡されたときだけ自分の発言に削除ボタンを出す
  onDeleteMessage?: (chatId: number) => Promise<void>
}

function ChatMessages({ messages, currentPlayerId, onDeleteMessage }: Props) {
  const [showSystemMessages, setShowSystemMessages] = useState(() => getShowSystemMessages(currentPlayerId))
  const [pendingDeleteChatId, setPendingDeleteChatId] = useState<number | null>(null)
  const [isDeleting, setIsDeleting] = useState(false)
  const [deleteError, setDeleteError] = useState<string | null>(null)

  const closeDeleteDialog = () => {
    if (isDeleting) {
      return
    }

    setPendingDeleteChatId(null)
    setDeleteError(null)
  }

  const confirmDelete = async () => {
    if (pendingDeleteChatId === null || !onDeleteMessage) {
      return
    }

    setIsDeleting(true)
    setDeleteError(null)
    try {
      await onDeleteMessage(pendingDeleteChatId)
      setPendingDeleteChatId(null)
    } catch (error) {
      setDeleteError(error instanceof Error ? error.message : locale.deleteFailed)
    } finally {
      setIsDeleting(false)
    }
  }

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
          <ChatMessageItem
            key={message.chatId}
            message={message}
            isOwnMessage={message.senderId === currentPlayerId}
            onDelete={onDeleteMessage ? () => setPendingDeleteChatId(message.chatId) : undefined}
          />
        ))
      )}
      <Dialog open={pendingDeleteChatId !== null} onClose={closeDeleteDialog} maxWidth="xs" fullWidth>
        <DialogTitle>{locale.deleteConfirmTitle}</DialogTitle>
        <DialogContent>
          <DialogContentText>{locale.deleteConfirmBody}</DialogContentText>
          {deleteError ? (
            <Alert severity="error" sx={{ mt: 2 }}>
              {deleteError}
            </Alert>
          ) : null}
        </DialogContent>
        <DialogActions>
          <Button onClick={closeDeleteDialog} disabled={isDeleting}>
            {locale.deleteCancel}
          </Button>
          <Button variant="contained" color="error" onClick={() => void confirmDelete()} disabled={isDeleting}>
            {locale.deleteConfirm}
          </Button>
        </DialogActions>
      </Dialog>
    </Stack>
  )
}

export default ChatMessages
