import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  AssignPetBattleSlotRequest,
  GetPetBattleStatusResponse,
  PetBattleRoom,
  PetBattleRun,
  PetBattleStats,
  SubmitPetBattleCommandRequest,
  SubmitPetBattleCommandResponse,
} from '@/schema/petBattle'

export async function matchPetBattle(accessToken: string): Promise<PetBattleRoom> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.match.path}`, {
    method: endpoints.petBattles.match.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'マッチングに失敗しました'))
  }

  return endpoints.petBattles.match.responseSchema.parse(json)
}

export async function getPetBattleStatus(accessToken: string): Promise<GetPetBattleStatusResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.status.path}`, {
    method: endpoints.petBattles.status.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '対戦状況の取得に失敗しました'))
  }

  return endpoints.petBattles.status.responseSchema.parse(json)
}

export async function getPetBattleStats(accessToken: string): Promise<PetBattleStats | null> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.stats.path}`, {
    method: endpoints.petBattles.stats.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '対戦成績の取得に失敗しました'))
  }

  return endpoints.petBattles.stats.responseSchema.parse(json)
}

export async function getPetBattleRoom(roomId: string, accessToken: string): Promise<PetBattleRoom> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.getRoom.path(roomId)}`, {
    method: endpoints.petBattles.getRoom.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '対戦ルームの取得に失敗しました'))
  }

  return endpoints.petBattles.getRoom.responseSchema.parse(json)
}

export async function assignPetBattleSlot(
  roomId: string,
  request: AssignPetBattleSlotRequest,
  accessToken: string,
): Promise<PetBattleRoom> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.assignSlot.path(roomId)}`, {
    method: endpoints.petBattles.assignSlot.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(endpoints.petBattles.assignSlot.requestSchema.parse(request)),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ペットの配置に失敗しました'))
  }

  return endpoints.petBattles.assignSlot.responseSchema.parse(json)
}

export async function removePetBattleSlot(roomId: string, petId: string, accessToken: string): Promise<PetBattleRoom> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.removeSlot.path(roomId, petId)}`, {
    method: endpoints.petBattles.removeSlot.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ペットの配置解除に失敗しました'))
  }

  return endpoints.petBattles.removeSlot.responseSchema.parse(json)
}

export async function startPetBattle(roomId: string, accessToken: string): Promise<PetBattleRun> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.start.path(roomId)}`, {
    method: endpoints.petBattles.start.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '対戦の開始に失敗しました'))
  }

  return endpoints.petBattles.start.responseSchema.parse(json)
}

export async function getPetBattleRun(runId: string, accessToken: string): Promise<PetBattleRun> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.getRun.path(runId)}`, {
    method: endpoints.petBattles.getRun.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '対戦情報の取得に失敗しました'))
  }

  return endpoints.petBattles.getRun.responseSchema.parse(json)
}

export async function submitPetBattleCommand(
  runId: string,
  request: SubmitPetBattleCommandRequest,
  accessToken: string,
): Promise<SubmitPetBattleCommandResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.submitCommand.path(runId)}`, {
    method: endpoints.petBattles.submitCommand.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(endpoints.petBattles.submitCommand.requestSchema.parse(request)),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'コマンドの送信に失敗しました'))
  }

  return endpoints.petBattles.submitCommand.responseSchema.parse(json)
}

export async function abortPetBattle(runId: string, accessToken: string): Promise<PetBattleRun> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.petBattles.abort.path(runId)}`, {
    method: endpoints.petBattles.abort.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '対戦の中断に失敗しました'))
  }

  return endpoints.petBattles.abort.responseSchema.parse(json)
}
