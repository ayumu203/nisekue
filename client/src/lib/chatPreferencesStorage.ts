const keyPrefix = 'nisekue:chat'

function showSystemMessagesKey(userId: string): string {
  return `${keyPrefix}:show-system-messages:${userId}`
}

export function getShowSystemMessages(userId: string): boolean {
  if (typeof window === 'undefined') {
    return true
  }

  try {
    const stored = window.localStorage.getItem(showSystemMessagesKey(userId))
    return stored !== 'false'
  } catch {
    return true
  }
}

export function saveShowSystemMessages(userId: string, value: boolean): void {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.setItem(showSystemMessagesKey(userId), String(value))
  } catch {
    // localStorage が利用できない環境でも動作を継続する
  }
}
