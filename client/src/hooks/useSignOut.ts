import { useState } from 'react'
import signOutLocale from '../../locale/auth/SignOut.json'
import { useNotify } from '@/contexts/useNotify'
import { supabase } from '@/lib/supabase'

// ログアウト処理。ステータス欄とホームのヘッダーの両方から使う。失敗はトーストで出す
export function useSignOut() {
  const [isSigningOut, setIsSigningOut] = useState(false)
  const notify = useNotify()

  const signOut = async () => {
    setIsSigningOut(true)

    const { error } = await supabase.auth.signOut()
    if (error) {
      notify.error(error.message || signOutLocale.toastFailed)
    }

    setIsSigningOut(false)
  }

  return { signOut, isSigningOut }
}
