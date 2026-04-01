import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type { GetRankingsResponse } from '@/schema/ranking'

export async function getRankings(accessToken: string): Promise<GetRankingsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.ranking.get.path}`, {
    method: endpoints.ranking.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ランキングの取得に失敗しました'))
  }

  return endpoints.ranking.get.responseSchema.parse(json)
}
