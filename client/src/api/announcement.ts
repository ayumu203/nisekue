import { endpoints } from '@/api/endpoints'
import { fetchSafely } from '@/api/http'
import { extractErrorMessage, resolveApiBaseUrl } from '@/api/util'
import type { GetAnnouncementsResponse } from '@/schema/announcement'

/** お知らせ一覧を取得する。ログイン不要のため認証ヘッダーは付けない。 */
export async function getAnnouncements(): Promise<GetAnnouncementsResponse> {
  const apiBaseUrl = resolveApiBaseUrl()

  const response = await fetchSafely(`${apiBaseUrl}${endpoints.announcement.list.path}`, {
    method: endpoints.announcement.list.method,
  })

  const json: unknown = await response.json().catch(() => null)

  if (!response.ok) {
    throw new Error(extractErrorMessage(json, 'お知らせの取得に失敗しました'))
  }

  return endpoints.announcement.list.responseSchema.parse(json)
}
