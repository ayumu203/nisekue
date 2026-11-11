import useSWR from 'swr'
import { getDebugStatus } from '@/api/debug'
import { useAuth } from '@/contexts/useAuth'

/** ログイン中のプレイヤーがデバッグ機能を使えるか。取得に失敗した場合は使えない扱いにする。 */
export function useDebugStatus(): { canUseDebug: boolean; isLoading: boolean } {
  const { session } = useAuth()
  const accessToken = session?.access_token ?? null
  const key = session?.user.id ? (['debug-status', session.user.id] as const) : null

  const { data, isLoading } = useSWR(
    key,
    async () => {
      if (!accessToken) {
        throw new Error('セッションが無効です')
      }

      return getDebugStatus(accessToken)
    },
    { revalidateOnFocus: false, shouldRetryOnError: false },
  )

  return { canUseDebug: data?.canUseDebug ?? false, isLoading }
}
