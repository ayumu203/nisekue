const beginnerGuideStorageKeyPrefix = 'nisekue:beginner-guide'
const beginnerGuideSeenValue = 'seen'

export function getBeginnerGuideStorageKey(userId: string, pageKey: string): string {
  return `${beginnerGuideStorageKeyPrefix}:${userId}:${pageKey}`
}

export function hasSeenBeginnerGuide(userId: string, pageKey: string): boolean {
  if (typeof window === 'undefined') {
    return false
  }

  try {
    return window.localStorage.getItem(getBeginnerGuideStorageKey(userId, pageKey)) === beginnerGuideSeenValue
  } catch {
    return false
  }
}

export function markBeginnerGuideSeen(userId: string, pageKey: string): void {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.setItem(getBeginnerGuideStorageKey(userId, pageKey), beginnerGuideSeenValue)
  } catch {
    // localStorage が利用できない環境でも画面は継続表示する
  }
}
