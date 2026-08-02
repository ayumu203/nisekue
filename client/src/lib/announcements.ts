import locale from '../../locale/announcement/Announcement.json'

export type AnnouncementItem = {
  id: string
  date: string
  title: string
  body: string
}

export const announcementTabLabel = locale.tabLabel
export const announcementEmptyMessage = locale.emptyMessage

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

/**
 * お知らせ一覧。並べ替えは行わないため、Announcement.json の items は
 * 日付の新しい順（先頭が最新）で記述すること。
 */
export const announcements = locale.items as AnnouncementItem[]

export const latestAnnouncementDate: string | null = announcements[0]?.date ?? null
