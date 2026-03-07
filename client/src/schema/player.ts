import { z } from 'zod'

export const playerIdSchema = z.string().uuid('ユーザーIDの形式が不正です')

export const playerUserNameSchema = z
  .string()
  .trim()
  .min(1, 'ユーザー名を入力してください')
  .max(20, 'ユーザー名は20文字以内で入力してください')

export const getPlayerResponseSchema = z.object({
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  level: z.number().int().min(1, 'レベルは1以上である必要があります'),
  exp: z.number().int().min(0, '経験値は0以上である必要があります'),
  status: z.object({
    maxHp: z.number().int().min(1, '最大HPは1以上である必要があります'),
    maxMp: z.number().int().min(0, '最大MPは0以上である必要があります'),
    strength: z.number().int().min(0, 'Strengthは0以上である必要があります'),
    defense: z.number().int().min(0, 'Defenseは0以上である必要があります'),
    intelligence: z.number().int().min(0, 'Intelligenceは0以上である必要があります'),
    luck: z.number().int().min(0, 'Luckは0以上である必要があります'),
    speed: z.number().int().min(0, 'Speedは0以上である必要があります'),
  }),
})

export const createPlayerRequestSchema = z.object({
  userName: playerUserNameSchema,
})

export const createPlayerResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
})

export const updatePlayerRequestSchema = z.object({
  userName: playerUserNameSchema,
})

export const updatePlayerResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema,
})

export type GetPlayerResponse = z.infer<typeof getPlayerResponseSchema>
export type CreatePlayerRequest = z.infer<typeof createPlayerRequestSchema>
export type CreatePlayerResponse = z.infer<typeof createPlayerResponseSchema>
export type UpdatePlayerRequest = z.infer<typeof updatePlayerRequestSchema>
export type UpdatePlayerResponse = z.infer<typeof updatePlayerResponseSchema>
