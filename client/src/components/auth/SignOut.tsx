import { useState } from 'react'
import { Button, Stack, type SxProps, type Theme } from '@mui/material'
import { supabase } from '@/lib/supabase'
import locale from '../../../locale/auth/SignOut.json'
import { useNotify } from '@/contexts/useNotify'

type SignOutProps = {
  onMessage?: (message: string) => void
  buttonSx?: SxProps<Theme>
}

function SignOut({ onMessage, buttonSx }: SignOutProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const notify = useNotify()

  const handleSignOut = async () => {
    setIsSubmitting(true)

    const { error: signOutError } = await supabase.auth.signOut()

    if (signOutError) {
      notify.error(signOutError.message)
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
    </Stack>
  )
}

export default SignOut
