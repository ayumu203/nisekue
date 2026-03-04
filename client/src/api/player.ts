import { endpoints } from './endpoints'
import type { CreatePlayerRequest, CreatePlayerResponse, GetPlayerResponse } from '../schema/player'

export async function getPlayer(accessToken: string): Promise<GetPlayerResponse> {
  const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/+$/, '')

  const response = await fetch(`${apiBaseUrl}${endpoints.player.get.path}`, {
    method: endpoints.player.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    const message =
      typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
        ? json.message
        : 'ユーザー情報の取得に失敗しました'
    throw new Error(message)
  }

  return endpoints.player.get.responseSchema.parse(json)
}

export async function createPlayer(input: CreatePlayerRequest, accessToken: string): Promise<CreatePlayerResponse> {
  const payload = endpoints.player.create.requestSchema.parse(input)
  const apiBaseUrl = (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/+$/, '')

  const response = await fetch(`${apiBaseUrl}${endpoints.player.create.path}`, {
    method: endpoints.player.create.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    const message =
      typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
        ? json.message
        : 'ユーザー名の登録に失敗しました'
    throw new Error(message)
  }

  return endpoints.player.create.responseSchema.parse(json)
}
