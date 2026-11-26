import { Alert, Box, Button, Dialog, DialogContent, FormControlLabel, Stack, Switch, Typography } from '@mui/material'
import { useState } from 'react'
import type { GetChatRoomResponse } from '@/schema/chat'
import ChatMessageItem from '@/components/chat/ChatMessageItem'
import { getShowSystemMessages, saveShowSystemMessages } from '@/lib/chatPreferencesStorage'
import locale from '../../../locale/chat/Chat.json'
import { font } from '@/theme/tokens'

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
      <Dialog
        open={pendingDeleteChatId !== null}
        onClose={closeDeleteDialog}
        maxWidth="xs"
        fullWidth
        PaperProps={{
          sx: {
            bgcolor: '#000',
            border: '3px solid #fff',
            borderRadius: 0,
            boxShadow: '4px 4px 0 #888, inset 0 0 0 1px #444',
            overflow: 'hidden',
          },
        }}
      >
        <DialogContent sx={{ p: 0 }}>
          <Stack spacing={0}>
            <Box sx={{ px: 3, py: 2.5, textAlign: 'center' }}>
              <Typography color="#fff" fontWeight={700} sx={{ fontSize: font.md, lineHeight: 1.7 }}>
                {locale.deleteConfirmTitle}
              </Typography>
              <Typography sx={{ mt: 1, fontSize: font.xs, color: '#aaa', lineHeight: 1.6 }}>
                {locale.deleteConfirmBody}
              </Typography>
              {deleteError ? (
                <Alert severity="error" sx={{ mt: 2, textAlign: 'left' }}>
                  {deleteError}
                </Alert>
              ) : null}
            </Box>
            <Box sx={{ borderTop: '1px solid #333', px: 3, py: 2 }}>
              <Stack direction="row" spacing={2} justifyContent="center">
                <Button
                  onClick={closeDeleteDialog}
                  disabled={isDeleting}
                  sx={{
                    '&&': {
                      minWidth: 80,
                      border: '2px solid #666',
                      borderRadius: 0,
                      color: '#aaa',
                      bgcolor: '#000',
                      fontWeight: 900,
                      fontSize: font.sm,
                      py: 0.75,
                      letterSpacing: '0.05em',
                      boxShadow: 'none',
                    },
                    '&&:hover': {
                      border: '2px solid #aaa',
                      color: '#fff',
                      bgcolor: '#1a1a1a',
                      boxShadow: 'none',
                    },
                  }}
                >
                  {locale.deleteCancel}
                </Button>
                <Button
                  onClick={() => void confirmDelete()}
                  disabled={isDeleting}
                  sx={{
                    '&&': {
                      minWidth: 80,
                      border: '2px solid #d32f2f',
                      borderRadius: 0,
                      color: '#fff',
                      bgcolor: '#d32f2f',
                      fontWeight: 900,
                      fontSize: font.sm,
                      py: 0.75,
                      letterSpacing: '0.05em',
                      boxShadow: 'none',
                    },
                    '&&:hover': {
                      bgcolor: '#b71c1c',
                      border: '2px solid #b71c1c',
                      boxShadow: 'none',
                    },
                    '&&.Mui-disabled': {
                      opacity: 0.5,
                    },
                  }}
                >
                  {locale.deleteConfirm}
                </Button>
              </Stack>
            </Box>
          </Stack>
        </DialogContent>
      </Dialog>
    </Stack>
  )
}

export default ChatMessages
