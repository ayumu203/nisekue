import { z } from 'zod'

export const playerIdSchema = z.string().uuid('ユーザーIDの形式が不正です')

export const playerUserNameSchema = z
  .string()
  .trim()
  .min(1, 'ユーザー名を入力してください')
  .max(20, 'ユーザー名は20文字以内で入力してください')

export const playerJobCodeSchema = z.enum(['Apprentice', 'Warrior', 'Guardian', 'Mage', 'Priest', 'Ranger'])

export const playerJobSchema = z.object({
  code: playerJobCodeSchema,
  value: z.number().int().min(1).max(6),
  displayName: z.string().min(1, 'ジョブ名が空です'),
})

export const moveTargetTypeSchema = z.enum(['Enemy', 'Ally', 'Self'])
export const moveAttackRangeSchema = z.enum(['Single', 'Column', 'Row', 'Square', 'All'])
export const moveCategorySchema = z.enum(['Attack', 'Support', 'Hybrid'])
export const moveElementTypeSchema = z.enum([
  'Strike',
  'Slash',
  'Pierce',
  'Fire',
  'Water',
  'Earth',
  'Wind',
  'Holy',
  'None',
])

export const playerMoveSlotSchema = z.object({
  slot: z.number().int().min(1).max(10),
  moveId: z.number().int().min(1).nullable(),
  moveName: z.string().min(1).nullable(),
  description: z.string().min(1).nullable(),
  elementType: moveElementTypeSchema.nullable(),
  targetType: moveTargetTypeSchema.nullable(),
  attackRange: moveAttackRangeSchema.nullable(),
  mpCost: z.number().int().min(0).nullable(),
  category: moveCategorySchema.nullable(),
})

export const getPlayerResponseSchema = z.object({
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
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
  moveSlots: z.array(playerMoveSlotSchema).length(10),
})

export const createPlayerRequestSchema = z.object({
  userName: playerUserNameSchema,
})

export const createPlayerResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
})

export const updatePlayerNameRequestSchema = z.object({
  userName: playerUserNameSchema,
})

export const updatePlayerNameResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
})

export const updatePlayerJobRequestSchema = z.object({
  job: z.number().int().min(1).max(6),
})

export const updatePlayerJobResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
})

export type GetPlayerResponse = z.infer<typeof getPlayerResponseSchema>
export type PlayerMoveSlot = z.infer<typeof playerMoveSlotSchema>
export type CreatePlayerRequest = z.infer<typeof createPlayerRequestSchema>
export type CreatePlayerResponse = z.infer<typeof createPlayerResponseSchema>
export type UpdatePlayerNameRequest = z.infer<typeof updatePlayerNameRequestSchema>
export type UpdatePlayerNameResponse = z.infer<typeof updatePlayerNameResponseSchema>
export type UpdatePlayerJobRequest = z.infer<typeof updatePlayerJobRequestSchema>
export type UpdatePlayerJobResponse = z.infer<typeof updatePlayerJobResponseSchema>
