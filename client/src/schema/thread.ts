import { z } from 'zod'
import { pagedResponseSchema } from '@/schema/pagination'

const threadIdSchema = z.string().uuid('threadIdの形式が不正です')
const replyIdSchema = z.string().uuid('replyIdの形式が不正です')

export const threadSummarySchema = z.object({
  id: threadIdSchema,
  title: z.string().trim().min(1).max(50),
  previewBody: z.string().trim().min(1).max(100),
  createdAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  lastRepliedAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }).nullable(),
  replyCount: z.number().int().min(0),
  hasImage: z.boolean().optional().default(false),
  authorPlayerId: z.string().uuid('authorPlayerIdの形式が不正です'),
  authorName: z.string().trim().min(1),
  authorImagePath: z.string().min(1).nullable().optional(),
})

export const threadReplySchema = z.object({
  id: replyIdSchema,
  body: z.string().trim().min(1).max(250),
  createdAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  authorPlayerId: z.string().uuid('authorPlayerIdの形式が不正です'),
  authorName: z.string().trim().min(1),
  authorImagePath: z.string().min(1).nullable().optional(),
})

export const threadDetailSchema = z.object({
  id: threadIdSchema,
  title: z.string().trim().min(1).max(50),
  body: z.string().trim().min(1).max(500),
  createdAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  updatedAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  lastRepliedAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }).nullable(),
  authorPlayerId: z.string().uuid('authorPlayerIdの形式が不正です'),
  authorName: z.string().trim().min(1),
  authorImagePath: z.string().min(1).nullable().optional(),
  imageUrl: z.string().min(1).nullable().optional(),
  imageIsSensitive: z.boolean().optional().default(false),
  replies: z.array(threadReplySchema),
})

export const getThreadsRequestSchema = z.object({
  page: z.number().int().positive().optional(),
})

export const getThreadsResponseSchema = pagedResponseSchema(threadSummarySchema)

export const createThreadRequestSchema = z.object({
  title: z.string().trim().min(1, 'タイトルを入力してください').max(50, 'タイトルは50文字以内で入力してください'),
  body: z.string().trim().min(1, '本文を入力してください').max(500, '本文は500文字以内で入力してください'),
})

export const THREAD_IMAGE_ACCEPTED_TYPES = ['image/png', 'image/jpeg'] as const

/** スレッドに添付する画像。送信時は multipart/form-data になる */
export type ThreadImageAttachment = {
  file: File
  isSensitive: boolean
}

export const createThreadResponseSchema = threadDetailSchema

export const getThreadImagePermissionResponseSchema = z.object({
  canPostImage: z.boolean(),
  isImageUploadAvailable: z.boolean(),
  maxImageBytes: z.number().int().positive(),
})

export const createThreadReplyRequestSchema = z.object({
  body: z.string().trim().min(1, '返信を入力してください').max(250, '返信は250文字以内で入力してください'),
})

export const createThreadReplyResponseSchema = threadDetailSchema

export const deleteThreadResponseSchema = z.object({
  message: z.string(),
})

export const threadAlertSummarySchema = z.object({
  threadId: threadIdSchema,
  threadTitle: z.string().trim().min(1).max(50),
  replyIds: z.array(replyIdSchema),
  latestReplyAuthorName: z.string().trim().min(1),
  latestReplyCreatedAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  unalertedReplyCount: z.number().int().min(1),
})

export const getThreadAlertsResponseSchema = z.object({
  items: z.array(threadAlertSummarySchema),
})

export const markThreadRepliesAlertedRequestSchema = z.object({
  replyIds: z.array(replyIdSchema).min(1),
})

export const markThreadRepliesAlertedResponseSchema = z.object({
  updatedCount: z.number().int().min(0),
})

export type ThreadSummary = z.infer<typeof threadSummarySchema>
export type ThreadReply = z.infer<typeof threadReplySchema>
export type ThreadDetail = z.infer<typeof threadDetailSchema>
export type GetThreadsRequest = z.infer<typeof getThreadsRequestSchema>
export type GetThreadsResponse = z.infer<typeof getThreadsResponseSchema>
export type CreateThreadRequest = z.infer<typeof createThreadRequestSchema>
export type CreateThreadResponse = z.infer<typeof createThreadResponseSchema>
export type CreateThreadReplyRequest = z.infer<typeof createThreadReplyRequestSchema>
export type CreateThreadReplyResponse = z.infer<typeof createThreadReplyResponseSchema>
export type DeleteThreadResponse = z.infer<typeof deleteThreadResponseSchema>
export type GetThreadImagePermissionResponse = z.infer<typeof getThreadImagePermissionResponseSchema>
export type ThreadAlertSummary = z.infer<typeof threadAlertSummarySchema>
export type GetThreadAlertsResponse = z.infer<typeof getThreadAlertsResponseSchema>
export type MarkThreadRepliesAlertedRequest = z.infer<typeof markThreadRepliesAlertedRequestSchema>
export type MarkThreadRepliesAlertedResponse = z.infer<typeof markThreadRepliesAlertedResponseSchema>
