import { useEffect, useEffectEvent } from 'react'
import { sendPlayerHeartbeat } from '@/api/player'
import { useAuth } from '@/contexts/useAuth'

// サーバーはこの送信をもとに直近5分以内に活動したプレイヤーを「プレイ中」として数える。
// 5分の判定枠に対して十分短い間隔で送る。
const HEARTBEAT_INTERVAL_MS = 2 * 60 * 1000

/**
 * ログイン中かつタブが表示されている間、定期的にハートビートを送る。
 * 画面を開いているだけのプレイヤーもアクティブ人数に数えるため。
 */
export function useActivityHeartbeat() {
  const { session } = useAuth()
  const isSignedIn = session !== null

  const sendHeartbeat = useEffectEvent(() => {
    const accessToken = session?.access_token
    if (!accessToken) {
      return
    }

    // 人数集計のためだけの送信なので、失敗してもプレイには影響させない
    sendPlayerHeartbeat(accessToken).catch(() => undefined)
  })

  useEffect(() => {
    if (!isSignedIn) {
      return
    }

    let timerId: number | null = null

    const start = () => {
      if (timerId !== null) {
        return
      }
      sendHeartbeat()
      timerId = window.setInterval(() => sendHeartbeat(), HEARTBEAT_INTERVAL_MS)
    }

    const stop = () => {
      if (timerId === null) {
        return
      }
      window.clearInterval(timerId)
      timerId = null
    }

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        start()
      } else {
        stop()
      }
    }

    handleVisibilityChange()
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      stop()
    }
  }, [isSignedIn])
}
