import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import { applyPaginationSearchParams } from '@/lib/pagination'
import type { PaginationOptions } from '@/lib/pagination'
import type {
  CreatePlayerRequest,
  CreatePlayerResponse,
  GetPlayerResponse,
  ListPlayersResponse,
  UpdatePlayerNameRequest,
  UpdatePlayerNameResponse,
  UpdatePlayerImageRequest,
  UpdatePlayerImageResponse,
  UpdatePlayerJobRequest,
  UpdatePlayerJobResponse,
  UpdatePlayerEquipmentRequest,
  UpdatePlayerEquipmentResponse,
  UpdatePlayerMoveSetRequest,
  UpdatePlayerMoveSetResponse,
  SendPlayerGiftRequest,
  SendPlayerGiftResponse,
  RebirthPlayerResponse,
  UnlockJobRoadmapRequest,
  UnlockJobRoadmapResponse,
  JobRoadmapListResponse,
  JobRoadmapResponse,
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

export async function getPlayerById(playerId: string, accessToken: string): Promise<GetPlayerResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.getById.path(playerId)}`, {
    method: endpoints.player.getById.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'プレイヤー情報の取得に失敗しました'))
  }

  return endpoints.player.getById.responseSchema.parse(json)
}

export async function listPlayers(accessToken: string, options?: PaginationOptions): Promise<ListPlayersResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const url = new URL(`${apiBaseUrl}${endpoints.player.list.path}`)
  applyPaginationSearchParams(url, options)

  const response = await fetchSafely(url.toString(), {
    method: endpoints.player.list.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'プレイヤー一覧の取得に失敗しました'))
  }

  return endpoints.player.list.responseSchema.parse(json)
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

export async function updatePlayer(
  input: UpdatePlayerNameRequest,
  accessToken: string,
): Promise<UpdatePlayerNameResponse> {
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

export async function updatePlayerImage(
  input: UpdatePlayerImageRequest,
  accessToken: string,
): Promise<UpdatePlayerImageResponse> {
  const parsedPayload = endpoints.player.updateImage.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.updateImage.path}`, {
    method: endpoints.player.updateImage.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'プレイヤー画像の更新に失敗しました'))
  }

  return endpoints.player.updateImage.responseSchema.parse(json)
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

export async function updatePlayerEquipment(
  input: UpdatePlayerEquipmentRequest,
  accessToken: string,
): Promise<UpdatePlayerEquipmentResponse> {
  const parsedPayload = endpoints.player.updateEquipment.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.updateEquipment.path}`, {
    method: endpoints.player.updateEquipment.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '装備の更新に失敗しました'))
  }

  return endpoints.player.updateEquipment.responseSchema.parse(json)
}

export async function updatePlayerMoveSet(
  input: UpdatePlayerMoveSetRequest,
  accessToken: string,
): Promise<UpdatePlayerMoveSetResponse> {
  const parsedPayload = endpoints.player.updateMoveSet.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }

  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.updateMoveSet.path}`, {
    method: endpoints.player.updateMoveSet.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(parsedPayload.data),
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'スキル順の更新に失敗しました'))
  }

  return endpoints.player.updateMoveSet.responseSchema.parse(json)
}

export async function sendPlayerGift(
  targetPlayerId: string,
  input: SendPlayerGiftRequest,
  accessToken: string,
): Promise<SendPlayerGiftResponse> {
  const parsedPayload = endpoints.player.sendGift.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }

  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.sendGift.path(targetPlayerId)}`, {
    method: endpoints.player.sendGift.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(parsedPayload.data),
  })

  const json: unknown = await response.json().catch(() => null)
  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'プレゼント送信に失敗しました'))
  }

  return endpoints.player.sendGift.responseSchema.parse(json)
}

export async function rebirthPlayer(accessToken: string): Promise<RebirthPlayerResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.rebirth.path}`, {
    method: endpoints.player.rebirth.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '転生に失敗しました'))
  }

  return endpoints.player.rebirth.responseSchema.parse(json)
}

export async function getJobRoadmapList(accessToken: string): Promise<JobRoadmapListResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.jobRoadmapList.path}`, {
    method: endpoints.player.jobRoadmapList.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ロードマップ一覧の取得に失敗しました'))
  }

  return endpoints.player.jobRoadmapList.responseSchema.parse(json)
}

export async function getJobRoadmap(jobId: number, accessToken: string): Promise<JobRoadmapResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.jobRoadmap.path(jobId)}`, {
    method: endpoints.player.jobRoadmap.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ロードマップの取得に失敗しました'))
  }

  return endpoints.player.jobRoadmap.responseSchema.parse(json)
}

export async function unlockJobRoadmap(
  input: UnlockJobRoadmapRequest,
  accessToken: string,
): Promise<UnlockJobRoadmapResponse> {
  const parsedPayload = endpoints.player.unlockJobRoadmap.requestSchema.safeParse(input)
  if (!parsedPayload.success) {
    throw new Error(parsedPayload.error.issues[0]?.message ?? '入力内容が不正です')
  }
  const payload = parsedPayload.data
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.player.unlockJobRoadmap.path}`, {
    method: endpoints.player.unlockJobRoadmap.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ロードマップの解放に失敗しました'))
  }

  return endpoints.player.unlockJobRoadmap.responseSchema.parse(json)
}
