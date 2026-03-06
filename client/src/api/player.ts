import { endpoints } from '@/api/endpoints'
import type {
  CreatePlayerRequest,
  CreatePlayerResponse,
  GetPlayerResponse,
} from '@/schema/player'

function resolveApiBaseUrl(): string {
  return (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/+$/, '')
}

function extractErrorMessage(json: unknown, fallback: string): string {
  return typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
    ? json.message
    : fallback
}

export async function getPlayer(accessToken: string): Promise<GetPlayerResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetch(`${apiBaseUrl}${endpoints.player.get.path}`, {
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
    throw new Error(extractErrorMessage(json, 'ユーザー名の登録に失敗しました'))
  }

  return endpoints.player.create.responseSchema.parse(json)
}
