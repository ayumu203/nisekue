import { useState } from 'react'
import { Alert, Button, Stack, SvgIcon, TextField, type SvgIconProps } from '@mui/material'
import { greenOutlinedInputSx, softGreenButtonSx } from '@/constants/styles'
import locale from '../../../locale/chat/Chat.json'

type Props = {
  isSubmitting: boolean
  disabled?: boolean
  disabledReason?: string | null
  onSubmit: (text: string) => Promise<void>
}

function SendIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M3 20.5v-7l15-1.5-15-1.5v-7l18 8z" />
    </SvgIcon>
  )
}

function ChatForm({ isSubmitting, disabled = false, disabledReason = null, onSubmit }: Props) {
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
      const message = error instanceof Error ? error.message : locale.sendFailed
      setErrorMessage(message)
    }
  }

  return (
    <Stack spacing={1}>
      {disabledReason ? <Alert severity="info">{disabledReason}</Alert> : null}
      {errorMessage ? <Alert severity="warning">{errorMessage}</Alert> : null}
      <Stack component="form" direction="row" spacing={1} alignItems="stretch" onSubmit={handleSubmit}>
        <TextField
          value={text}
          onChange={(event) => {
            setText(event.target.value)
            if (errorMessage) {
              setErrorMessage(null)
            }
          }}
          size="small"
          placeholder={locale.placeholder}
          fullWidth
          disabled={disabled || isSubmitting}
          inputProps={{ maxLength: 200 }}
          sx={{
            ...greenOutlinedInputSx,
            '& .MuiOutlinedInput-root': {
              ...greenOutlinedInputSx['& .MuiOutlinedInput-root'],
              minHeight: 40,
            },
          }}
        />
        <Button
          type="submit"
          variant="contained"
          disabled={disabled || isSubmitting || text.trim().length === 0}
          aria-label={locale.send}
          sx={{
            ...softGreenButtonSx,
            minWidth: 40,
            width: 40,
            minHeight: 40,
            height: 40,
            p: 0,
            borderRadius: '50%',
            flexShrink: 0,
          }}
        >
          <SendIcon fontSize="small" />
        </Button>
      </Stack>
    </Stack>
  )
}

export default ChatForm
