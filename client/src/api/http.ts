const NETWORK_ERROR_PATTERN = /networkerror|failed to fetch|fetch failed|load failed|network request failed/i
const DEFAULT_CONNECTION_ERROR_MESSAGE = 'サーバーに接続できませんでした。時間をおいて再試行してください。'

const SERVER_ERROR_MESSAGE = 'サーバーで問題が発生しています。時間をおいて再試行してください。'

export async function fetchSafely(
  input: RequestInfo | URL,
  init?: RequestInit,
  connectionErrorMessage: string = DEFAULT_CONNECTION_ERROR_MESSAGE,
): Promise<Response> {
  let response: Response

  try {
    response = await fetch(input, init)
  } catch (error) {
    if (error instanceof Error) {
      const combinedMessage = `${error.name} ${error.message}`
      if (NETWORK_ERROR_PATTERN.test(combinedMessage)) {
        throw new Error(connectionErrorMessage)
      }

      throw error
    }

    throw new Error(connectionErrorMessage)
  }

  if (response.status >= 500 && !(await hasErrorMessageBody(response))) {
    throw new Error(SERVER_ERROR_MESSAGE)
  }

  return response
}

async function hasErrorMessageBody(response: Response): Promise<boolean> {
  try {
    const json: unknown = await response.clone().json()
    return typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
  } catch {
    return false
  }
}
