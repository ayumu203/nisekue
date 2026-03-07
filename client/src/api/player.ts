import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  CreatePlayerRequest,
  CreatePlayerResponse,
  GetPlayerResponse,
  UpdatePlayerRequest,
  UpdatePlayerResponse,
} from '@/schema/player'

export async function getPlayer(accessToken: string): Promise<GetPlayerResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.get.path}`, {
    method: endpoints.player.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ユーザー情報の取得に失敗しました'))
  }

  return endpoints.player.get.responseSchema.parse(json)
}

export async function createPlayer(input: CreatePlayerRequest, accessToken: string): Promise<CreatePlayerResponse> {
  const payload = endpoints.player.create.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.create.path}`, {
    method: endpoints.player.create.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ユーザー名の登録に失敗しました'))
  }

  return endpoints.player.create.responseSchema.parse(json)
}

export async function updatePlayer(input: UpdatePlayerRequest, accessToken: string): Promise<UpdatePlayerResponse> {
  const parsedPayload = endpoints.player.update.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.update.path}`, {
    method: endpoints.player.update.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ユーザー情報の更新に失敗しました'))
  }

  return endpoints.player.update.responseSchema.parse(json)
}
