const NETWORK_ERROR_PATTERN = /networkerror|failed to fetch|fetch failed|load failed|network request failed/i

// fetch の失敗はサーバー停止・CORS 拒否・回線断のいずれでも同じ TypeError になり、原因を区別できない。
// そのため、利用者の通信環境だけを原因として断定する文言は使わない。
const DEFAULT_CONNECTION_ERROR_MESSAGE =
  'サーバーに接続できませんでした。時間をおいて再試行してください。（通信環境が原因の場合もあります）'

// サーバーまで届いたうえで 5xx が返った場合は、サーバー側の問題であることを明示する。
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

// バックエンドが返す { message } は呼び出し側でそのまま表示させたいので、
// 本文にメッセージを持たない 5xx（ホスティング側が返すエラーページなど）だけを共通文言に置き換える。
async function hasErrorMessageBody(response: Response): Promise<boolean> {
  try {
    const json: unknown = await response.clone().json()
    return typeof json === 'object' && json !== null && 'message' in json && typeof json.message === 'string'
  } catch {
    return false
  }
}
