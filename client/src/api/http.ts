const NETWORK_ERROR_PATTERN = /networkerror|failed to fetch|fetch failed|load failed|network request failed/i

const DEFAULT_NETWORK_ERROR_MESSAGE = 'ネットワークエラーが発生しました。通信環境を確認して再試行してください。'

export async function fetchSafely(
  input: RequestInfo | URL,
  init?: RequestInit,
  networkErrorMessage: string = DEFAULT_NETWORK_ERROR_MESSAGE,
): Promise<Response> {
  try {
    return await fetch(input, init)
  } catch (error) {
    if (error instanceof Error) {
      const combinedMessage = `${error.name} ${error.message}`
      if (NETWORK_ERROR_PATTERN.test(combinedMessage)) {
        throw new Error(networkErrorMessage)
      }

      throw error
    }

    throw new Error(networkErrorMessage)
  }
}
