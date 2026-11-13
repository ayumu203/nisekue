import useSWR from 'swr'
import { getAnnouncements } from '@/api/announcement'

const announcementsSWRKey = 'announcements'

/** お知らせ一覧（日付の新しい順）と、未読判定に使う最新の日付を返す。 */
export function useAnnouncements() {
  const { data, error, isLoading } = useSWR(announcementsSWRKey, getAnnouncements, {
    revalidateOnFocus: false,
  })

  const announcements = data?.items ?? []

  return {
    announcements,
    latestAnnouncementDate: announcements[0]?.date ?? null,
    error: error as Error | undefined,
    isLoading,
  }
}
