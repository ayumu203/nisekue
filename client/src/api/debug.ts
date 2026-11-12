import type { z } from 'zod'
import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  DebugEquipmentListResponse,
  DebugGrantEquipmentRequest,
  DebugGrantItemRequest,
  DebugItemListResponse,
  DebugLearnMoveRequest,
  DebugLevelUpRequest,
  DebugLevelUpResponse,
  DebugMasterJobsRequest,
  DebugMasterJobsResponse,
  DebugMoveListResponse,
  DebugUpdateStatusRequest,
  GetDebugStatusResponse,
} from '@/schema/debug'

/** デバッグ用 API の共通処理。body を渡すと JSON で送る。 */
export async function callDebugApi<TSchema extends z.ZodType>(
  endpoint: { path: string; method: string; responseSchema: TSchema },
  accessToken: string,
  fallbackMessage: string,
  body?: unknown,
): Promise<z.infer<TSchema>> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoint.path}`, {
    method: endpoint.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
      ...(body === undefined ? {} : { 'Content-Type': 'application/json' }),
    },
    ...(body === undefined ? {} : { body: JSON.stringify(body) }),
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, fallbackMessage))
  }

  return endpoint.responseSchema.parse(json)
}

export async function getDebugStatus(accessToken: string): Promise<GetDebugStatusResponse> {
  return callDebugApi(endpoints.debug.getStatus, accessToken, 'デバッグ機能の状態の取得に失敗しました')
}

export async function getDebugMoves(accessToken: string): Promise<DebugMoveListResponse> {
  return callDebugApi(endpoints.debug.getMoves, accessToken, 'スキル一覧の取得に失敗しました')
}

export async function updateDebugStatus(request: DebugUpdateStatusRequest, accessToken: string): Promise<void> {
  const body = endpoints.debug.updateStatus.requestSchema.parse(request)
  await callDebugApi(endpoints.debug.updateStatus, accessToken, 'ステータスの変更に失敗しました', body)
}

export async function learnDebugMove(request: DebugLearnMoveRequest, accessToken: string): Promise<void> {
  const body = endpoints.debug.learnMove.requestSchema.parse(request)
  await callDebugApi(endpoints.debug.learnMove, accessToken, 'スキルの習得に失敗しました', body)
}

export async function clearDebugMoveSlot(slotIndex: number, accessToken: string): Promise<void> {
  const endpoint = endpoints.debug.clearMoveSlot
  await callDebugApi({ ...endpoint, path: endpoint.path(slotIndex) }, accessToken, 'スキルを外せませんでした')
}

export async function getDebugItems(accessToken: string): Promise<DebugItemListResponse> {
  return callDebugApi(endpoints.debug.getItems, accessToken, 'アイテム一覧の取得に失敗しました')
}

export async function getDebugEquipments(accessToken: string): Promise<DebugEquipmentListResponse> {
  return callDebugApi(endpoints.debug.getEquipments, accessToken, '装備一覧の取得に失敗しました')
}

/** 入手後の所持数を返す */
export async function grantDebugItem(request: DebugGrantItemRequest, accessToken: string): Promise<number> {
  const body = endpoints.debug.grantItem.requestSchema.parse(request)
  const result = await callDebugApi(endpoints.debug.grantItem, accessToken, 'アイテムの入手に失敗しました', body)
  return result.quantity
}

export async function grantDebugEquipment(request: DebugGrantEquipmentRequest, accessToken: string): Promise<number> {
  const body = endpoints.debug.grantEquipment.requestSchema.parse(request)
  const result = await callDebugApi(endpoints.debug.grantEquipment, accessToken, '装備の入手に失敗しました', body)
  return result.count
}

export async function levelUpDebugPlayer(
  request: DebugLevelUpRequest,
  accessToken: string,
): Promise<DebugLevelUpResponse> {
  const body = endpoints.debug.levelUp.requestSchema.parse(request)
  return callDebugApi(endpoints.debug.levelUp, accessToken, 'レベルの変更に失敗しました', body)
}

export async function masterDebugJobs(
  request: DebugMasterJobsRequest,
  accessToken: string,
): Promise<DebugMasterJobsResponse> {
  const body = endpoints.debug.masterJobs.requestSchema.parse(request)
  return callDebugApi(endpoints.debug.masterJobs, accessToken, 'ジョブのマスターに失敗しました', body)
}
