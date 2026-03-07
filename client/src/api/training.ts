import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { trainingCooldownErrorSchema } from '@/schema/training'
import type { ExecuteTrainingRequest, ExecuteTrainingResponse, GetTrainingEnemiesResponse } from '@/schema/training'

export class TrainingCooldownError extends Error {
  public readonly retryAfterSeconds: number

  public readonly cooldownUntil: string

  constructor(message: string, retryAfterSeconds: number, cooldownUntil: string) {
    super(message)
    this.name = 'TrainingCooldownError'
    this.retryAfterSeconds = retryAfterSeconds
    this.cooldownUntil = cooldownUntil
  }
}

function resolveApiBaseUrl(): string {
  return (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/+$/, '')
}

function extractErrorMessage(json: unknown, fallback: string): string {
  return typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
    ? json.message
    : fallback
}

export async function getTrainingEnemies(accessToken: string): Promise<GetTrainingEnemiesResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.training.getEnemies.path}`, {
    method: endpoints.training.getEnemies.method,
    headers: {
      Authorization: `Bearer ${accessToken}`,
    },
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '訓練相手の取得に失敗しました'))
  }

  return endpoints.training.getEnemies.responseSchema.parse(json)
}

export async function executeTraining(
  input: ExecuteTrainingRequest,
  accessToken: string,
): Promise<ExecuteTrainingResponse> {
  const payload = endpoints.training.execute.requestSchema.parse(input)
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.training.execute.path}`, {
    method: endpoints.training.execute.method,
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken}`,
    },
    body: JSON.stringify(payload),
  })

  const json: unknown = await response.json().catch(() => null)

  if (response.status === 429) {
    const cooldown = trainingCooldownErrorSchema.safeParse(json)
    if (cooldown.success) {
      throw new TrainingCooldownError(cooldown.data.message, cooldown.data.retryAfterSeconds, cooldown.data.cooldownUntil)
    }

    throw new Error('訓練のクールダウン中です')
  }

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, '訓練の実行に失敗しました'))
  }

  return endpoints.training.execute.responseSchema.parse(json)
}
