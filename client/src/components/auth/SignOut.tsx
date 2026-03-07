import { useState } from 'react'
import { Alert, Button, Stack, type SxProps, type Theme } from '@mui/material'
import { supabase } from '@/lib/supabase'
import locale from '../../../locale/auth/SignOut.json'

type SignOutProps = {
  onMessage?: (message: string) => void
  buttonSx?: SxProps<Theme>
}

function SignOut({ onMessage, buttonSx }: SignOutProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSignOut = async () => {
    setIsSubmitting(true)
    setError(null)

    const { error: signOutError } = await supabase.auth.signOut()

    if (signOutError) {
      setError(signOutError.message)
      onMessage?.(locale.toastFailed)
      setIsSubmitting(false)
      return
    }

    onMessage?.(locale.toastSuccess)
    setIsSubmitting(false)
  }

  return (
    <Stack spacing={2} alignItems="flex-start">
      <Button type="button" variant="outlined" onClick={handleSignOut} disabled={isSubmitting} sx={buttonSx}>
        {isSubmitting ? locale.submitting : locale.submit}
      </Button>
      {error ? <Alert severity="error">{error}</Alert> : null}
    </Stack>
  )
}

export default SignOut
