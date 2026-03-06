import { useState } from 'react'
import { Button, Stack, TextField } from '@mui/material'

type Props = {
  isSubmitting: boolean
  onSubmit: (text: string) => Promise<void>
}

function ChatForm({ isSubmitting, onSubmit }: Props) {
  const [text, setText] = useState('')

  const handleSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault()
    const normalized = text.trim()
    if (!normalized) {
      return
    }

    await onSubmit(normalized)
    setText('')
  }

  return (
    <Stack component="form" direction="row" spacing={1} onSubmit={handleSubmit}>
      <TextField
        value={text}
        onChange={(event) => setText(event.target.value)}
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
  )
}

export default ChatForm
