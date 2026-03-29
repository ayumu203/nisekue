import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  CreateThreadReplyRequest,
  CreateThreadReplyResponse,
  CreateThreadRequest,
  CreateThreadResponse,
  GetThreadsRequest,
  GetThreadsResponse,
  ThreadDetail,
} from '@/schema/thread'

export async function getThreads(input: GetThreadsRequest, accessToken: string): Promise<GetThreadsResponse> {
  const payload = endpoints.thread.list.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const url = new URL(`${apiBaseUrl}${endpoints.thread.list.path}`, window.location.origin)

  if (payload.page != null) {
    url.searchParams.set('page', String(payload.page))
  }

  const response = await fetchSafely(url.toString(), {
    method: endpoints.thread.list.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スレッド一覧の取得に失敗しました'))
  }

  return endpoints.thread.list.responseSchema.parse(json)
}

export async function getThreadDetail(threadId: string, accessToken: string): Promise<ThreadDetail> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.get.path(threadId)}`, {
    method: endpoints.thread.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スレッド詳細の取得に失敗しました'))
  }

  return endpoints.thread.get.responseSchema.parse(json)
}

export async function createThread(input: CreateThreadRequest, accessToken: string): Promise<CreateThreadResponse> {
  const payload = endpoints.thread.create.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.create.path}`, {
    method: endpoints.thread.create.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スレッド作成に失敗しました'))
  }

  return endpoints.thread.create.responseSchema.parse(json)
}

export async function createThreadReply(
  threadId: string,
  input: CreateThreadReplyRequest,
  accessToken: string,
): Promise<CreateThreadReplyResponse> {
  const payload = endpoints.thread.reply.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.reply.path(threadId)}`, {
    method: endpoints.thread.reply.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '返信送信に失敗しました'))
  }

  return endpoints.thread.reply.responseSchema.parse(json)
}
