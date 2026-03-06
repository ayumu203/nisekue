import { useState } from 'react'
import { Alert, Button, Stack, TextField } from '@mui/material'

type Props = {
  isSubmitting: boolean
  onSubmit: (text: string) => Promise<void>
}

function ChatForm({ isSubmitting, onSubmit }: Props) {
  const [text, setText] = useState('')
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const normalized = text.trim()
    if (!normalized) {
      return
    }

    try {
      await onSubmit(normalized)
      setText('')
      setErrorMessage(null)
    } catch (error) {
      const message = error instanceof Error ? error.message : 'メッセージ送信に失敗しました'
      setErrorMessage(message)
    }
  }

  return (
    <Stack spacing={1}>
      {errorMessage ? <Alert severity="warning">{errorMessage}</Alert> : null}
      <Stack component="form" direction="row" spacing={1} onSubmit={handleSubmit}>
        <TextField
          value={text}
          onChange={(event) => {
            setText(event.target.value)
            if (errorMessage) {
              setErrorMessage(null)
            }
          }}
          size="small"
          placeholder="メッセージを入力"
          fullWidth
          disabled={isSubmitting}
          inputProps={{ maxLength: 200 }}
        />
        <Button type="submit" variant="contained" disabled={isSubmitting || text.trim().length === 0}>
          送信
        </Button>
      </Stack>
    </Stack>
  )
}

export default ChatForm
