export function resolveApiBaseUrl(): string {
  return (import.meta.env.VITE_API_BASE_URL ?? '/api').replace(/\/+$/, '')
}

export function extractErrorMessage(json: unknown, fallback: string): string {
  return typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
    ? json.message
    : fallback
}
