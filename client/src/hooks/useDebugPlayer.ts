import useSWR from 'swr'
import { getPlayer } from '@/api/player'
import { useAuth } from '@/contexts/useAuth'

/**
 * デバッグ画面用に自分のプレイヤー情報を取得する。
 * ホームと同じ SWR キーを使うので、refresh するとホームの表示も更新される。
 */
export function useDebugPlayer() {
  const { session } = useAuth()
  const accessToken = session?.access_token ?? null
  const key = session?.user.id ? (['player', session.user.id] as const) : null

  const { data, error, isLoading, mutate } = useSWR(key, async () => {
    if (!accessToken) {
      throw new Error('セッションが無効です')
    }

    return getPlayer(accessToken)
  })

  return {
    player: data,
    error: error instanceof Error ? error : null,
    isLoading,
    accessToken,
    refresh: async () => {
      await mutate()
    },
  }
}
