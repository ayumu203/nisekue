import { z } from 'zod'
import { playerIdSchema } from '@/schema/player'

export const chatMessageSchema = z.object({
  chatId: z.number().int().min(1, 'chatIdは1以上である必要があります'),
  senderId: playerIdSchema.nullable(),
  senderName: z.string().trim().min(1, '投稿者名が不正です'),
  imagePath: z.string().min(1).nullable().optional(),
  text: z.string().trim().min(1, 'メッセージを入力してください').max(200, 'メッセージは200文字以内で入力してください'),
  createdAt: z.string().datetime({ offset: true, message: '日時の形式が不正です' }),
  isAlerted: z.boolean(),
})

export const getChatRoomRequestSchema = z.object({
  ownerId: playerIdSchema,
})

export const getChatRoomResponseSchema = z.object({
  ownerId: playerIdSchema,
  lastChatId: z.number().int().min(0, 'lastChatIdは0以上である必要があります'),
  messages: z.array(chatMessageSchema),
})

export const postChatMessageRequestSchema = z.object({
  ownerId: playerIdSchema,
  text: z.string().trim().min(1, 'メッセージを入力してください').max(200, 'メッセージは200文字以内で入力してください'),
})

export const postChatMessageResponseSchema = getChatRoomResponseSchema

export const markChatMessagesAlertedRequestSchema = z.object({
  ownerId: playerIdSchema,
  chatIds: z.array(z.number().int().min(1)).min(1),
})

export const markChatMessagesAlertedResponseSchema = z.object({
  updatedCount: z.number().int().min(0),
})

export type GetChatRoomRequest = z.infer<typeof getChatRoomRequestSchema>
export type GetChatRoomResponse = z.infer<typeof getChatRoomResponseSchema>
export type PostChatMessageRequest = z.infer<typeof postChatMessageRequestSchema>
export type PostChatMessageResponse = z.infer<typeof postChatMessageResponseSchema>
export type MarkChatMessagesAlertedRequest = z.infer<typeof markChatMessagesAlertedRequestSchema>
export type MarkChatMessagesAlertedResponse = z.infer<typeof markChatMessagesAlertedResponseSchema>
