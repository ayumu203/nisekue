import { HubConnection, HubConnectionBuilder, HttpTransportType, LogLevel } from '@microsoft/signalr'
import { endpoints } from '@/api/endpoints'
import { resolveApiBaseUrl } from '@/api/util'
import { supabase } from '@/lib/supabase'

// 自動再接続の待ち時間。最後の値を上限として、つながるまで繰り返す。
const RECONNECT_DELAYS_MS = [0, 2_000, 10_000, 30_000]

function resolvePlayerPresenceHubUrl(): string {
  const apiBaseUrl = resolveApiBaseUrl()
  return new URL(`${apiBaseUrl}${endpoints.player.presenceHub.path}`, window.location.origin).toString()
}

export function createPlayerPresenceHubConnection(): HubConnection {
  return new HubConnectionBuilder()
    .withUrl(resolvePlayerPresenceHubUrl(), {
      // 長時間つなぎっぱなしにするため、(再)接続のたびに最新のトークンを取り直す
      accessTokenFactory: async () => {
        const { data } = await supabase.auth.getSession()
        return data.session?.access_token ?? ''
      },
      transport: HttpTransportType.WebSockets | HttpTransportType.LongPolling,
      withCredentials: false,
    })
    .withAutomaticReconnect({
      nextRetryDelayInMilliseconds: ({ previousRetryCount }) =>
        RECONNECT_DELAYS_MS[Math.min(previousRetryCount, RECONNECT_DELAYS_MS.length - 1)],
    })
    .configureLogging(import.meta.env.DEV ? LogLevel.Information : LogLevel.Warning)
    .build()
}
