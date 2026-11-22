import { useState } from 'react'
import signOutLocale from '../../locale/auth/SignOut.json'
import { supabase } from '@/lib/supabase'

// ログアウト処理。ステータス欄とホームのヘッダーの両方から使う
export function useSignOut() {
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [signOutError, setSignOutError] = useState<string | null>(null)

  const signOut = async () => {
    setIsSigningOut(true)
    setSignOutError(null)

    const { error } = await supabase.auth.signOut()
    if (error) {
      setSignOutError(error.message || signOutLocale.toastFailed)
    }

    setIsSigningOut(false)
  }

  return { signOut, isSigningOut, signOutError }
}
