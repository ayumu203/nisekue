import { useEffect, useEffectEvent, useState } from 'react'
import type { HubConnection } from '@microsoft/signalr'
import { endpoints } from '@/api/endpoints'
import {
  questRunHubErrorEventSchema,
  questRunHubSnapshotEventSchema,
  questRunHubUpdatedEventSchema,
  type QuestRunHubErrorEvent,
  type QuestRunHubSnapshotEvent,
  type QuestRunHubUpdatedEvent,
} from '@/schema/quest'
import { createQuestRunHubConnection, subscribeQuestRun, unsubscribeQuestRun } from '@/lib/questRunHub'

type UseQuestRunHubOptions = {
  runId: string | null
  accessToken: string | null
  onSnapshot?: (event: QuestRunHubSnapshotEvent) => void
  onUpdated?: (event: QuestRunHubUpdatedEvent) => void
  onError?: (event: QuestRunHubErrorEvent) => void
}

type UseQuestRunHubResult = {
  connection: HubConnection | null
  isConnected: boolean
  connectionError: Error | null
}

export function useQuestRunHub({
  runId,
  accessToken,
  onSnapshot,
  onUpdated,
  onError,
}: UseQuestRunHubOptions): UseQuestRunHubResult {
  const [connection, setConnection] = useState<HubConnection | null>(null)
  const [isConnected, setIsConnected] = useState(false)
  const [connectionError, setConnectionError] = useState<Error | null>(null)

  const handleSnapshot = useEffectEvent((payload: unknown) => {
    const parsed = questRunHubSnapshotEventSchema.parse(payload)
    onSnapshot?.(parsed)
  })

  const handleUpdated = useEffectEvent((payload: unknown) => {
    const parsed = questRunHubUpdatedEventSchema.parse(payload)
    onUpdated?.(parsed)
  })

  const handleHubError = useEffectEvent((payload: unknown) => {
    const parsed = questRunHubErrorEventSchema.parse(payload)
    onError?.(parsed)
  })

  useEffect(() => {
    if (!runId || !accessToken) {
      setConnection(null)
      setIsConnected(false)
      setConnectionError(null)
      return
    }

    let isDisposed = false
    const nextConnection = createQuestRunHubConnection(accessToken)
    setConnection(nextConnection)
    setConnectionError(null)

    nextConnection.on(endpoints.quest.hub.snapshotEvent, handleSnapshot)
    nextConnection.on(endpoints.quest.hub.updatedEvent, handleUpdated)
    nextConnection.on(endpoints.quest.hub.errorEvent, handleHubError)

    const start = async () => {
      try {
        await nextConnection.start()
        if (isDisposed) {
          await nextConnection.stop()
          return
        }

        await subscribeQuestRun(nextConnection, runId)
        if (isDisposed) {
          await unsubscribeQuestRun(nextConnection, runId)
          await nextConnection.stop()
          return
        }

        setIsConnected(true)
      } catch (error) {
        if (isDisposed) {
          return
        }

        setConnectionError(error instanceof Error ? error : new Error('QuestRunHub への接続に失敗しました'))
        setIsConnected(false)
      }
    }

    void start()

    return () => {
      isDisposed = true
      setIsConnected(false)
      setConnection(null)

      const teardown = async () => {
        nextConnection.off(endpoints.quest.hub.snapshotEvent, handleSnapshot)
        nextConnection.off(endpoints.quest.hub.updatedEvent, handleUpdated)
        nextConnection.off(endpoints.quest.hub.errorEvent, handleHubError)

        try {
          await unsubscribeQuestRun(nextConnection, runId)
        } catch {
          // Ignore unsubscribe errors during teardown.
        }

        try {
          await nextConnection.stop()
        } catch {
          // Ignore stop errors during teardown.
        }
      }

      void teardown()
    }
  }, [accessToken, handleHubError, handleSnapshot, handleUpdated, runId])

  return {
    connection,
    isConnected,
    connectionError,
  }
}
