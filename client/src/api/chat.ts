import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import type { GetChatRoomRequest, GetChatRoomResponse, PostChatMessageRequest, PostChatMessageResponse } from '@/schema/chat'

function resolveApiBaseUrl(): string {
  return (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/+$/, '')
}

function extractErrorMessage(json: unknown, fallback: string): string {
  return typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
    ? json.message
    : fallback
}

export async function getChatRoom(input: GetChatRoomRequest, accessToken: string): Promise<GetChatRoomResponse> {
  const payload = endpoints.chatRoom.get.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const url = new URL(`${apiBaseUrl}${endpoints.chatRoom.get.path}`, window.location.origin)
  url.searchParams.set('ownerId', payload.ownerId)

  const response = await fetchSafely(url.toString(), {
    method: endpoints.chatRoom.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'チャットルームの取得に失敗しました'))
  }

  return endpoints.chatRoom.get.responseSchema.parse(json)
}

export async function postChatMessage(
  input: PostChatMessageRequest,
  accessToken: string,
): Promise<PostChatMessageResponse> {
  const payload = endpoints.chatRoom.postMessage.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.chatRoom.postMessage.path}`, {
    method: endpoints.chatRoom.postMessage.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'メッセージ送信に失敗しました'))
  }

  return endpoints.chatRoom.postMessage.responseSchema.parse(json)
}
