import { useState } from 'react'
import { Alert, Button, Stack } from '@mui/material'
import { supabase } from '@/lib/supabase'

type SignOutProps = {
  onMessage?: (message: string) => void
}

function SignOut({ onMessage }: SignOutProps) {
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleSignOut = async () => {
    setIsSubmitting(true)
    setError(null)

    const { error: signOutError } = await supabase.auth.signOut()

    if (signOutError) {
      setError(signOutError.message)
      onMessage?.('ログアウトに失敗しました')
      setIsSubmitting(false)
      return
    }

    onMessage?.('ログアウトしました')
    setIsSubmitting(false)
  }

  return (
    <Stack spacing={2} alignItems="flex-start">
      <Button type="button" variant="outlined" onClick={handleSignOut} disabled={isSubmitting}>
        {isSubmitting ? 'Signing out...' : 'Sign out'}
      </Button>
      {error ? <Alert severity="error">{error}</Alert> : null}
    </Stack>
  )
}

export default SignOut
