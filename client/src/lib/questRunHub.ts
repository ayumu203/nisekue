import {
  HubConnection,
  HubConnectionBuilder,
  HubConnectionState,
  HttpTransportType,
  LogLevel,
} from '@microsoft/signalr'
import { endpoints } from '@/api/endpoints'
import { resolveApiBaseUrl } from '@/api/util'

function resolveQuestRunHubUrl(): string {
  const apiBaseUrl = resolveApiBaseUrl()
  return new URL(`${apiBaseUrl}${endpoints.quest.hub.path}`, window.location.origin).toString()
}

export function createQuestRunHubConnection(accessToken: string): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(resolveQuestRunHubUrl(), {
      accessTokenFactory: () => accessToken,
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      withCredentials: false,
    })
    .withAutomaticReconnect()
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build()
}

export async function subscribeQuestRun(connection: HubConnection, runId: string) {
  await connection.invoke(endpoints.quest.hub.subscribeMethod, runId)
}

export async function unsubscribeQuestRun(connection: HubConnection, runId: string) {
  if (connection.state !== HubConnectionState.Connected) {
    return
  }

  await connection.invoke(endpoints.quest.hub.unsubscribeMethod, runId)
}
