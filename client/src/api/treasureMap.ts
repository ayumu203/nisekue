import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  ClaimTreasureMapRewardResponse,
  GetCurrentTreasureMapExpeditionResponse,
  GetTreasureMapsResponse,
  StartTreasureMapExpeditionRequest,
  TreasureMapExpedition,
} from '@/schema/treasureMap'

export async function getTreasureMaps(accessToken: string): Promise<GetTreasureMapsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.treasureMap.getMaps.path}`, {
    method: endpoints.treasureMap.getMaps.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '宝の地図一覧の取得に失敗しました'))
  }

  return endpoints.treasureMap.getMaps.responseSchema.parse(json)
}

export async function getCurrentTreasureMapExpedition(
  accessToken: string,
): Promise<GetCurrentTreasureMapExpeditionResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.treasureMap.getCurrentExpedition.path}`, {
    method: endpoints.treasureMap.getCurrentExpedition.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '進行中の宝の地図遠征の取得に失敗しました'))
  }

  return endpoints.treasureMap.getCurrentExpedition.responseSchema.parse(json)
}

export async function startTreasureMapExpedition(
  input: StartTreasureMapExpeditionRequest,
  accessToken: string,
): Promise<TreasureMapExpedition> {
  const payload = endpoints.treasureMap.startExpedition.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.treasureMap.startExpedition.path}`, {
    method: endpoints.treasureMap.startExpedition.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '宝の地図遠征の開始に失敗しました'))
  }

  return endpoints.treasureMap.startExpedition.responseSchema.parse(json)
}

export async function claimTreasureMapReward(
  expeditionId: string,
  accessToken: string,
): Promise<ClaimTreasureMapRewardResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.treasureMap.claimReward.path(expeditionId)}`, {
    method: endpoints.treasureMap.claimReward.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '宝の地図報酬の受け取りに失敗しました'))
  }

  return endpoints.treasureMap.claimReward.responseSchema.parse(json)
}
