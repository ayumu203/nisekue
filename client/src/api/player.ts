import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  CreatePlayerRequest,
  CreatePlayerResponse,
  GetPlayerResponse,
  UpdatePlayerNameRequest,
  UpdatePlayerNameResponse,
  UpdatePlayerJobRequest,
  UpdatePlayerJobResponse,
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

export async function updatePlayer(input: UpdatePlayerNameRequest, accessToken: string): Promise<UpdatePlayerNameResponse> {
  const parsedPayload = endpoints.player.updateName.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.updateName.path}`, {
    method: endpoints.player.updateName.method,
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

  return endpoints.player.updateName.responseSchema.parse(json)
}

export async function updatePlayerJob(
  input: UpdatePlayerJobRequest,
  accessToken: string,
): Promise<UpdatePlayerJobResponse> {
  const parsedPayload = endpoints.player.updateJob.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.updateJob.path}`, {
    method: endpoints.player.updateJob.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '職業の更新に失敗しました'))
  }

  return endpoints.player.updateJob.responseSchema.parse(json)
}
