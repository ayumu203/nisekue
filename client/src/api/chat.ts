import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  GetChatRoomRequest,
  GetChatRoomResponse,
  MarkChatMessagesAlertedRequest,
  MarkChatMessagesAlertedResponse,
  PostChatMessageRequest,
  PostChatMessageResponse,
  GetGlobalChatRoomRequest,
  GetGlobalChatRoomResponse,
  PostGlobalChatMessageRequest,
  PostGlobalChatMessageResponse,
} from '@/schema/chat'

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

export async function getGlobalChatRoom(
  input: GetGlobalChatRoomRequest,
  accessToken: string,
): Promise<GetGlobalChatRoomResponse> {
  const payload = endpoints.globalChatRoom.get.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const url = new URL(`${apiBaseUrl}${endpoints.globalChatRoom.get.path}`, window.location.origin)
  if (payload.page !== undefined) {
    url.searchParams.set('page', String(payload.page))
  }

  const response = await fetchSafely(url.toString(), {
    method: endpoints.globalChatRoom.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '全体チャットルームの取得に失敗しました'))
  }

  return endpoints.globalChatRoom.get.responseSchema.parse(json)
}

export async function postGlobalChatMessage(
  input: PostGlobalChatMessageRequest,
  accessToken: string,
): Promise<PostGlobalChatMessageResponse> {
  const payload = endpoints.globalChatRoom.postMessage.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.globalChatRoom.postMessage.path}`, {
    method: endpoints.globalChatRoom.postMessage.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '全体チャットへのメッセージ送信に失敗しました'))
  }

  return endpoints.globalChatRoom.postMessage.responseSchema.parse(json)
}

export async function markChatMessagesAlerted(
  input: MarkChatMessagesAlertedRequest,
  accessToken: string,
): Promise<MarkChatMessagesAlertedResponse> {
  const payload = endpoints.chatRoom.markAlerts.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.chatRoom.markAlerts.path}`, {
    method: endpoints.chatRoom.markAlerts.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'チャット通知の更新に失敗しました'))
  }

  return endpoints.chatRoom.markAlerts.responseSchema.parse(json)
}
