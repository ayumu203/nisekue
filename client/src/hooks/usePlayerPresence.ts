import { useEffect } from 'react'
import { HubConnectionState } from '@microsoft/signalr'
import { useAuth } from '@/contexts/useAuth'
import { createPlayerPresenceHubConnection } from '@/lib/playerPresenceHub'

// タブが隠れてからこの時間が経ったら切断し、プレイ中の人数から外す
const HIDDEN_DISCONNECT_DELAY_MS = 5 * 60 * 1000
// 初回接続に失敗したとき、または自動再接続が終わってしまったときに接続し直すまでの時間
const RESTART_DELAY_MS = 30 * 1000

/**
 * ログイン中、プレイ中の人数を数えるハブにつなぎっぱなしにする。
 * サーバーは接続数をもとに人数を数えるので、DB への書き込みは発生しない。
 */
export function usePlayerPresence() {
  const { session } = useAuth()
  const userId = session?.user.id ?? null

  useEffect(() => {
    if (!userId) {
      return
    }

    const connection = createPlayerPresenceHubConnection()
    let isDisposed = false
    let shouldConnect = false
    let restartTimerId: number | null = null
    let hiddenTimerId: number | null = null

    const clearRestartTimer = () => {
      if (restartTimerId !== null) {
        window.clearTimeout(restartTimerId)
        restartTimerId = null
      }
    }

    const clearHiddenTimer = () => {
      if (hiddenTimerId !== null) {
        window.clearTimeout(hiddenTimerId)
        hiddenTimerId = null
      }
    }

    const scheduleRestart = () => {
      if (isDisposed || !shouldConnect || restartTimerId !== null) {
        return
      }
      restartTimerId = window.setTimeout(() => {
        restartTimerId = null
        void connect()
      }, RESTART_DELAY_MS)
    }

    const connect = async () => {
      if (isDisposed || !shouldConnect || connection.state !== HubConnectionState.Disconnected) {
        return
      }

      try {
        await connection.start()
      } catch {
        scheduleRestart()
        return
      }

      if (isDisposed || !shouldConnect) {
        await connection.stop().catch(() => undefined)
      }
    }

    const disconnect = () => {
      shouldConnect = false
      clearRestartTimer()
      void connection.stop().catch(() => undefined)
    }

    // 自分で stop したときは shouldConnect が false なので再接続しない
    connection.onclose(() => scheduleRestart())

    const handleVisibilityChange = () => {
      if (document.visibilityState === 'visible') {
        clearHiddenTimer()
        shouldConnect = true
        void connect()
        return
      }

      if (hiddenTimerId === null) {
        hiddenTimerId = window.setTimeout(() => {
          hiddenTimerId = null
          disconnect()
        }, HIDDEN_DISCONNECT_DELAY_MS)
      }
    }

    handleVisibilityChange()
    document.addEventListener('visibilitychange', handleVisibilityChange)

    return () => {
      isDisposed = true
      document.removeEventListener('visibilitychange', handleVisibilityChange)
      clearHiddenTimer()
      clearRestartTimer()
      void connection.stop().catch(() => undefined)
    }
  }, [userId])
}
