import { useEffect, useEffectEvent, useMemo, useRef, useState } from 'react'
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
  const isEnabled = Boolean(runId && accessToken)
  const [isConnected, setIsConnected] = useState(false)
  const [connectionError, setConnectionError] = useState<Error | null>(null)
  const connectionStateVersionRef = useRef(0)

  const connection = useMemo(
    () => (isEnabled && accessToken ? createQuestRunHubConnection(accessToken) : null),
    [accessToken, isEnabled],
  )

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
    if (!connection || !runId) {
      return
    }

    const currentVersion = connectionStateVersionRef.current + 1
    connectionStateVersionRef.current = currentVersion
    let isDisposed = false

    connection.on(endpoints.quest.hub.snapshotEvent, handleSnapshot)
    connection.on(endpoints.quest.hub.updatedEvent, handleUpdated)
    connection.on(endpoints.quest.hub.errorEvent, handleHubError)

    const start = async () => {
      try {
        await connection.start()
        if (isDisposed || connectionStateVersionRef.current != currentVersion) {
          await connection.stop()
          return
        }

        await subscribeQuestRun(connection, runId)
        if (isDisposed || connectionStateVersionRef.current != currentVersion) {
          await unsubscribeQuestRun(connection, runId)
          await connection.stop()
          return
        }

        setConnectionError(null)
        setIsConnected(true)
      } catch (error) {
        if (isDisposed || connectionStateVersionRef.current != currentVersion) {
          return
        }

        setConnectionError(error instanceof Error ? error : new Error('QuestRunHub への接続に失敗しました'))
        setIsConnected(false)
      }
    }

    void start()

    return () => {
      isDisposed = true

      const teardown = async () => {
        connection.off(endpoints.quest.hub.snapshotEvent, handleSnapshot)
        connection.off(endpoints.quest.hub.updatedEvent, handleUpdated)
        connection.off(endpoints.quest.hub.errorEvent, handleHubError)

        try {
          await unsubscribeQuestRun(connection, runId)
        } catch {
          // Ignore unsubscribe errors during teardown.
        }

        try {
          await connection.stop()
        } catch {
          // Ignore stop errors during teardown.
        }

        if (connectionStateVersionRef.current == currentVersion) {
          setIsConnected(false)
        }
      }

      void teardown()
    }
  }, [connection, runId])

  return {
    connection: isEnabled ? connection : null,
    isConnected: isEnabled ? isConnected : false,
    connectionError: isEnabled ? connectionError : null,
  }
}
