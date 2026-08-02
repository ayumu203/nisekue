const announcementStorageKeyPrefix = 'nisekue:announcement'
const anonymousUserKey = 'anonymous'

export function getAnnouncementStorageKey(userId: string | null | undefined): string {
  return `${announcementStorageKeyPrefix}:${userId ? userId : anonymousUserKey}`
}

export function getLastSeenAnnouncementDate(userId: string | null | undefined): string | null {
  if (typeof window === 'undefined') {
    return null
  }

  try {
    return window.localStorage.getItem(getAnnouncementStorageKey(userId))
  } catch {
    return null
  }
}

export function markAnnouncementsSeen(userId: string | null | undefined, date: string | null): void {
  if (typeof window === 'undefined' || date === null) {
    return
  }

  try {
    window.localStorage.setItem(getAnnouncementStorageKey(userId), date)
  } catch {
    // localStorage が利用できない環境でも画面は継続表示する
  }
}

export function hasUnreadAnnouncement(userId: string | null | undefined, latestDate: string | null): boolean {
  if (latestDate === null) {
    return false
  }

  const lastSeenDate = getLastSeenAnnouncementDate(userId)

  if (lastSeenDate === null) {
    return true
  }

  // date は YYYY-MM-DD 固定のため辞書順比較で新旧を判定できる
  return latestDate > lastSeenDate
}
