import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  CreateQuestRoomRequest,
  GetQuestStagesResponse,
  ListQuestRoomsRequest,
  ListQuestRoomsResponse,
  ManualControlRequest,
  ManualControlResponse,
  PostQuestChatMessageRequest,
  PostQuestChatMessageResponse,
  QuestRoomDetailResponse,
  QuestRunDetailResponse,
  SubmitQuestCommandRequest,
  SubmitQuestCommandResponse,
} from '@/schema/quest'

function createAuthorizedHeaders(accessToken: string, contentType: 'application/json' | null = 'application/json') {
  const headers: Record<string, string> = {
    Authorization: `Bearer ${accessToken}`,
  }

  if (contentType) {
    headers['Content-Type'] = contentType
  }

  return headers
}

export async function getQuestStages(accessToken: string): Promise<GetQuestStagesResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.getStages.path}`, {
    method: endpoints.quest.getStages.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'クエストステージの取得に失敗しました'))
  }

  return endpoints.quest.getStages.responseSchema.parse(json)
}

export async function createQuestRoom(
  input: CreateQuestRoomRequest,
  accessToken: string,
): Promise<QuestRoomDetailResponse> {
  const payload = endpoints.quest.createRoom.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.createRoom.path}`, {
    method: endpoints.quest.createRoom.method,
    headers: createAuthorizedHeaders(accessToken),
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ルームの作成に失敗しました'))
  }

  return endpoints.quest.createRoom.responseSchema.parse(json)
}

export async function listQuestRooms(
  input: ListQuestRoomsRequest,
  accessToken: string,
): Promise<ListQuestRoomsResponse> {
  const payload = endpoints.quest.listRooms.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const url = new URL(`${apiBaseUrl}${endpoints.quest.listRooms.path}`, window.location.origin)

  if (payload.stageId != null) {
    url.searchParams.set('stageId', String(payload.stageId))
  }
  if (payload.mode != null) {
    url.searchParams.set('mode', payload.mode)
  }
  if (payload.status != null) {
    url.searchParams.set('status', payload.status)
  }
  if (payload.ownerPlayerId != null) {
    url.searchParams.set('ownerPlayerId', payload.ownerPlayerId)
  }
  if (payload.page != null) {
    url.searchParams.set('page', String(payload.page))
  }
  if (payload.pageSize != null) {
    url.searchParams.set('pageSize', String(payload.pageSize))
  }

  const response = await fetchSafely(url.toString(), {
    method: endpoints.quest.listRooms.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ルーム一覧の取得に失敗しました'))
  }

  return endpoints.quest.listRooms.responseSchema.parse(json)
}

export async function getQuestRoom(roomId: string, accessToken: string): Promise<QuestRoomDetailResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.getRoom.path(roomId)}`, {
    method: endpoints.quest.getRoom.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ルーム詳細の取得に失敗しました'))
  }

  return endpoints.quest.getRoom.responseSchema.parse(json)
}

export async function getQuestRunByRoom(roomId: string, accessToken: string): Promise<QuestRunDetailResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.getRoomRun.path(roomId)}`, {
    method: endpoints.quest.getRoomRun.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'クエスト進行情報の取得に失敗しました'))
  }

  return endpoints.quest.getRoomRun.responseSchema.parse(json)
}

export async function joinQuestRoom(roomId: string, accessToken: string): Promise<QuestRoomDetailResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.joinRoom.path(roomId)}`, {
    method: endpoints.quest.joinRoom.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ルーム参加に失敗しました'))
  }

  return endpoints.quest.joinRoom.responseSchema.parse(json)
}

export async function updateQuestRoomPosition(
  roomId: string,
  input: { participantId: string; row: 'Front' | 'Middle' | 'Back'; column: 'Left' | 'Right' },
  accessToken: string,
): Promise<QuestRoomDetailResponse> {
  const payload = endpoints.quest.updateRoomPosition.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.updateRoomPosition.path(roomId)}`, {
    method: endpoints.quest.updateRoomPosition.method,
    headers: createAuthorizedHeaders(accessToken),
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '配置更新に失敗しました'))
  }

  return endpoints.quest.updateRoomPosition.responseSchema.parse(json)
}

export async function startQuestRoom(roomId: string, accessToken: string): Promise<QuestRunDetailResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.startRoom.path(roomId)}`, {
    method: endpoints.quest.startRoom.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'クエスト開始に失敗しました'))
  }

  return endpoints.quest.startRoom.responseSchema.parse(json)
}

export async function getQuestRun(runId: string, accessToken: string): Promise<QuestRunDetailResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.getRun.path(runId)}`, {
    method: endpoints.quest.getRun.method,
    headers: createAuthorizedHeaders(accessToken, null),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'クエスト進行情報の取得に失敗しました'))
  }

  return endpoints.quest.getRun.responseSchema.parse(json)
}

export async function submitQuestCommand(
  runId: string,
  input: SubmitQuestCommandRequest,
  accessToken: string,
): Promise<SubmitQuestCommandResponse> {
  const payload = endpoints.quest.submitCommand.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.submitCommand.path(runId)}`, {
    method: endpoints.quest.submitCommand.method,
    headers: createAuthorizedHeaders(accessToken),
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'コマンド送信に失敗しました'))
  }

  return endpoints.quest.submitCommand.responseSchema.parse(json)
}

export async function requestQuestManualControl(
  runId: string,
  input: ManualControlRequest,
  accessToken: string,
): Promise<ManualControlResponse> {
  const payload = endpoints.quest.requestManualControl.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.requestManualControl.path(runId)}`, {
    method: endpoints.quest.requestManualControl.method,
    headers: createAuthorizedHeaders(accessToken),
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '手動復帰申請に失敗しました'))
  }

  return endpoints.quest.requestManualControl.responseSchema.parse(json)
}

export async function approveQuestManualControl(
  runId: string,
  input: ManualControlRequest,
  accessToken: string,
): Promise<ManualControlResponse> {
  const payload = endpoints.quest.approveManualControl.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.approveManualControl.path(runId)}`, {
    method: endpoints.quest.approveManualControl.method,
    headers: createAuthorizedHeaders(accessToken),
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '手動復帰承認に失敗しました'))
  }

  return endpoints.quest.approveManualControl.responseSchema.parse(json)
}

export async function postQuestChatMessage(
  runId: string,
  input: PostQuestChatMessageRequest,
  accessToken: string,
): Promise<PostQuestChatMessageResponse> {
  const payload = endpoints.quest.postChatMessage.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.quest.postChatMessage.path(runId)}`, {
    method: endpoints.quest.postChatMessage.method,
    headers: createAuthorizedHeaders(accessToken),
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'クエストチャット送信に失敗しました'))
  }

  return endpoints.quest.postChatMessage.responseSchema.parse(json)
}
