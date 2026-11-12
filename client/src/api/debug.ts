import type { z } from 'zod'
import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type { GetDebugStatusResponse } from '@/schema/debug'

/** デバッグ用 API の共通処理。body を渡すと JSON で送る。 */
export async function callDebugApi<TSchema extends z.ZodType>(
  endpoint: { path: string; method: string; responseSchema: TSchema },
  accessToken: string,
  fallbackMessage: string,
  body?: unknown,
): Promise<z.infer<TSchema>> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoint.path}`, {
    method: endpoint.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, fallbackMessage))
  }

  return endpoint.responseSchema.parse(json)
}

export async function getDebugStatus(accessToken: string): Promise<GetDebugStatusResponse> {
  return callDebugApi(endpoints.debug.getStatus, accessToken, 'デバッグ機能の状態の取得に失敗しました')
}
