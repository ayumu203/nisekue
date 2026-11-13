import locale from '../../locale/announcement/Announcement.json'

export const announcementTabLabel = locale.tabLabel
export const announcementEmptyMessage = locale.emptyMessage
export const announcementLoadingMessage = locale.loadingMessage
export const announcementErrorMessage = locale.errorMessage

/**
 * 'YYYY-MM-DD' を 'YY/M/D' 形式（例: 2026-08-02 -> 26/8/2）に整形する。
 * 想定外の形式はそのまま返す。
 */
export function formatAnnouncementDate(date: string): string {
  const match = date.match(/^(\d{4})-(\d{2})-(\d{2})$/)

  if (!match) {
    return date
  }

  const [, year, month, day] = match

  return `${year.slice(2)}/${Number(month)}/${Number(day)}`
}
