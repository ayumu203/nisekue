import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  CreateThreadReplyRequest,
  CreateThreadReplyResponse,
  CreateThreadRequest,
  CreateThreadResponse,
  DeleteThreadResponse,
  GetThreadAlertsResponse,
  GetThreadImagePermissionResponse,
  GetThreadsRequest,
  GetThreadsResponse,
  MarkThreadRepliesAlertedRequest,
  MarkThreadRepliesAlertedResponse,
  ThreadDetail,
  ThreadImageAttachment,
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

export async function createThread(
  input: CreateThreadRequest,
  accessToken: string,
  image?: ThreadImageAttachment | null,
): Promise<CreateThreadResponse> {
  const parsedPayload = endpoints.thread.create.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  // 画像があるときは multipart/form-data で送る（Content-Type はブラウザに付けてもらう）
  let body: BodyInit
  const headers: Record<string, string> = { Authorization: `Bearer ${accessToken}` }
  if (image) {
    const formData = new FormData()
    formData.append('title', payload.title)
    formData.append('body', payload.body)
    formData.append('image', image.file)
    formData.append('imageIsSensitive', String(image.isSensitive))
    body = formData
  } else {
    headers['Content-Type'] = 'application/json'
    body = JSON.stringify(payload)
  }

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.create.path}`, {
    method: endpoints.thread.create.method,
    headers,
    body,
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    const fallback = response.status === 413 ? '画像のサイズが大きすぎます' : 'スレッド作成に失敗しました'
    throw new Error(extractErrorMessage(json, fallback))
  }

  return endpoints.thread.create.responseSchema.parse(json)
}

export async function getThreadImagePermission(accessToken: string): Promise<GetThreadImagePermissionResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.imagePermission.path}`, {
    method: endpoints.thread.imagePermission.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '画像投稿の権限の取得に失敗しました'))
  }

  return endpoints.thread.imagePermission.responseSchema.parse(json)
}

export async function createThreadReply(
  threadId: string,
  input: CreateThreadReplyRequest,
  accessToken: string,
): Promise<CreateThreadReplyResponse> {
  const parsedPayload = endpoints.thread.reply.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
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

export async function deleteThread(threadId: string, accessToken: string): Promise<DeleteThreadResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.delete.path(threadId)}`, {
    method: endpoints.thread.delete.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スレッド削除に失敗しました'))
  }

  return endpoints.thread.delete.responseSchema.parse(json)
}

export async function getThreadAlerts(accessToken: string): Promise<GetThreadAlertsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.alerts.path}`, {
    method: endpoints.thread.alerts.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スレッド通知の取得に失敗しました'))
  }

  return endpoints.thread.alerts.responseSchema.parse(json)
}

export async function markThreadRepliesAlerted(
  input: MarkThreadRepliesAlertedRequest,
  accessToken: string,
): Promise<MarkThreadRepliesAlertedResponse> {
  const payload = endpoints.thread.markAlerts.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.thread.markAlerts.path}`, {
    method: endpoints.thread.markAlerts.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スレッド通知の更新に失敗しました'))
  }

  return endpoints.thread.markAlerts.responseSchema.parse(json)
}
