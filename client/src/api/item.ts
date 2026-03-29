import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type {
  CreateMarketListingRequest,
  CreateMarketListingResponse,
  GetItemsResponse,
  GetMarketListingsResponse,
  ItemActionResponse,
  PurchaseMarketListingRequest,
  PurchaseMarketListingResponse,
  SynthesizeEquipmentResponse,
  UseItemRequest,
} from '@/schema/item'

export async function getItems(accessToken: string): Promise<GetItemsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.items.get.path}`, {
    method: endpoints.items.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'アイテム一覧の取得に失敗しました'))
  }

  return endpoints.items.get.responseSchema.parse(json)
}

export async function useItem(
  itemStackId: string,
  input: UseItemRequest,
  accessToken: string,
): Promise<ItemActionResponse> {
  const payload = endpoints.items.use.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.items.use.path(itemStackId)}`, {
    method: endpoints.items.use.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'アイテム使用に失敗しました'))
  }

  return endpoints.items.use.responseSchema.parse(json)
}

export async function synthesizeEquipment(
  playerEquipmentId: string,
  accessToken: string,
): Promise<SynthesizeEquipmentResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.items.synthesize.path(playerEquipmentId)}`, {
    method: endpoints.items.synthesize.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '合成に失敗しました'))
  }

  return endpoints.items.synthesize.responseSchema.parse(json)
}

export async function deleteEquipment(playerEquipmentId: string, accessToken: string): Promise<ItemActionResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.items.deleteEquipment.path(playerEquipmentId)}`, {
    method: endpoints.items.deleteEquipment.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '装備削除に失敗しました'))
  }

  return endpoints.items.deleteEquipment.responseSchema.parse(json)
}

export async function deleteItemStack(
  itemStackId: string,
  input: UseItemRequest,
  accessToken: string,
): Promise<ItemActionResponse> {
  const payload = endpoints.items.deleteStack.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.items.deleteStack.path(itemStackId)}`, {
    method: endpoints.items.deleteStack.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'アイテム削除に失敗しました'))
  }

  return endpoints.items.deleteStack.responseSchema.parse(json)
}

export async function createMarketListing(
  input: CreateMarketListingRequest,
  accessToken: string,
): Promise<CreateMarketListingResponse> {
  const payload = endpoints.market.createListing.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.market.createListing.path}`, {
    method: endpoints.market.createListing.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '出品に失敗しました'))
  }

  return endpoints.market.createListing.responseSchema.parse(json)
}

export async function getMarketListings(accessToken: string): Promise<GetMarketListingsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.market.getListings.path}`, {
    method: endpoints.market.getListings.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'マーケット一覧の取得に失敗しました'))
  }

  return endpoints.market.getListings.responseSchema.parse(json)
}

export async function getMyMarketListings(accessToken: string): Promise<GetMarketListingsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.market.getMyListings.path}`, {
    method: endpoints.market.getMyListings.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '自分の出品一覧の取得に失敗しました'))
  }

  return endpoints.market.getMyListings.responseSchema.parse(json)
}

export async function purchaseMarketListing(
  listingId: string,
  input: PurchaseMarketListingRequest,
  accessToken: string,
): Promise<PurchaseMarketListingResponse> {
  const payload = endpoints.market.purchase.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()
  const response = await fetchSafely(`${apiBaseUrl}${endpoints.market.purchase.path(listingId)}`, {
    method: endpoints.market.purchase.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })
  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '購入に失敗しました'))
  }

  return endpoints.market.purchase.responseSchema.parse(json)
}
