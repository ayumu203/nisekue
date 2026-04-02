import { z } from 'zod'

const threadIdSchema = z.string().uuid('threadIdの形式が不正です')
const replyIdSchema = z.string().uuid('replyIdの形式が不正です')

export const threadSummarySchema = z.object({
  id: threadIdSchema,
  title: z.string().trim().min(1).max(50),
  previewBody: z.string().trim().min(1).max(100),
  createdAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  lastRepliedAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }).nullable(),
  replyCount: z.number().int().min(0),
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
  replies: z.array(threadReplySchema),
})

export const getThreadsRequestSchema = z.object({
  page: z.number().int().positive().optional(),
})

export const getThreadsResponseSchema = z.object({
  items: z.array(threadSummarySchema),
  page: z.number().int().positive(),
  pageSize: z.number().int().positive(),
  totalCount: z.number().int().min(0),
  hasNextPage: z.boolean(),
})

export const createThreadRequestSchema = z.object({
  title: z.string().trim().min(1, 'タイトルを入力してください').max(50, 'タイトルは50文字以内で入力してください'),
  body: z.string().trim().min(1, '本文を入力してください').max(500, '本文は500文字以内で入力してください'),
})

export const createThreadResponseSchema = threadDetailSchema

export const createThreadReplyRequestSchema = z.object({
  body: z.string().trim().min(1, '返信を入力してください').max(250, '返信は250文字以内で入力してください'),
})

export const createThreadReplyResponseSchema = threadDetailSchema

export const deleteThreadResponseSchema = z.object({
  message: z.string(),
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
