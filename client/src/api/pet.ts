import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type { GetPetsResponse, PlayerPetView } from '@/schema/pet'

export async function getPets(accessToken: string): Promise<GetPetsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.pets.get.path}`, {
    method: endpoints.pets.get.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ペット一覧の取得に失敗しました'))
  }

  return endpoints.pets.get.responseSchema.parse(json)
}

export async function trainPet(petId: string, accessToken: string): Promise<PlayerPetView> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.pets.train.path(petId)}`, {
    method: endpoints.pets.train.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ペットの育成に失敗しました'))
  }

  return endpoints.pets.train.responseSchema.parse(json)
}

export async function standbyPet(petId: string, accessToken: string): Promise<GetPetsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.pets.standby.path(petId)}`, {
    method: endpoints.pets.standby.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ペットのスタンバイ設定に失敗しました'))
  }

  return endpoints.pets.standby.responseSchema.parse(json)
}

export async function clearStandbyPet(petId: string, accessToken: string): Promise<GetPetsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.pets.clearStandby.path(petId)}`, {
    method: endpoints.pets.clearStandby.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'ペットのスタンバイ解除に失敗しました'))
  }

  return endpoints.pets.clearStandby.responseSchema.parse(json)
}

export async function releasePet(petId: string, accessToken: string): Promise<void> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.pets.release.path(petId)}`, {
    method: endpoints.pets.release.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  if (!response.ok) {
    const json: unknown = await response.json().catch(() => null)
    throw new Error(extractErrorMessage(json, 'ペットを逃がせませんでした'))
  }
}
