import { z } from 'zod'

export const announcementSchema = z.object({
  id: z.string().min(1),
  date: z.string().regex(/^\d{4}-\d{2}-\d{2}$/, '日付の形式が不正です'),
  title: z.string().min(1),
  body: z.string().min(1),
})

export const getAnnouncementsResponseSchema = z.object({
  items: z.array(announcementSchema),
})

export type Announcement = z.infer<typeof announcementSchema>
export type GetAnnouncementsResponse = z.infer<typeof getAnnouncementsResponseSchema>
