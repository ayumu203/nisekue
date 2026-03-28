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
  description: z.string().min(1, 'ジョブ説明が空です'),
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
  effectImagePath: z.string().min(1).nullable().optional(),
  elementType: moveElementTypeSchema.nullable(),
  targetType: moveTargetTypeSchema.nullable(),
  attackRange: moveAttackRangeSchema.nullable(),
  mpCost: z.number().int().min(0).nullable(),
  category: moveCategorySchema.nullable(),
})

export const learnedMoveSchema = z.object({
  moveId: z.number().int().min(1),
  moveName: z.string().min(1),
})

const playerStatusValuesSchema = z.object({
  maxHp: z.number().int().min(1, '最大HPは1以上である必要があります'),
  maxMp: z.number().int().min(0, '最大MPは0以上である必要があります'),
  strength: z.number().int().min(0, 'Strengthは0以上である必要があります'),
  defense: z.number().int().min(0, 'Defenseは0以上である必要があります'),
  intelligence: z.number().int().min(0, 'Intelligenceは0以上である必要があります'),
  luck: z.number().int().min(0, 'Luckは0以上である必要があります'),
  speed: z.number().int().min(0, 'Speedは0以上である必要があります'),
})

const playerEquipmentBonusValuesSchema = z.object({
  maxHp: z.number().int().min(0),
  maxMp: z.number().int().min(0),
  strength: z.number().int().min(0),
  defense: z.number().int().min(0),
  intelligence: z.number().int().min(0),
  luck: z.number().int().min(0),
  speed: z.number().int().min(0),
})

const playerEquipmentTypeSchema = z.enum(['Weapon', 'Armor'])
const playerEquipmentStatusSchema = z.enum(['Inventory', 'Equipped', 'Broken'])

const playerEquipmentSchema = z.object({
  playerEquipmentId: z.string().uuid('装備IDの形式が不正です'),
  equipmentId: z.number().int().min(1),
  name: z.string().trim().min(1, '装備名が空です'),
  equipmentType: playerEquipmentTypeSchema,
  status: playerEquipmentStatusSchema,
  durability: z.number().int().min(0),
  maxDurability: z.number().int().min(1),
  mastery: z.number().int().min(0),
  canEquipCurrentJob: z.boolean(),
  bonusValues: playerEquipmentBonusValuesSchema,
})

export const getPlayerResponseSchema = z.object({
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  imagePath: z.string().min(1).nullable().optional(),
  job: playerJobSchema,
  jobProfiles: z.array(playerJobSchema),
  level: z.number().int().min(1, 'レベルは1以上である必要があります'),
  exp: z.number().int().min(0, '経験値は0以上である必要があります'),
  jobLevel: z.number().int().min(1, '職業レベルは1以上である必要があります'),
  jobExp: z.number().int().min(0, '職業経験値は0以上である必要があります'),
  status: z.object({
    baseValues: playerStatusValuesSchema,
    effectiveValues: playerStatusValuesSchema,
  }),
  moveSlots: z.array(playerMoveSlotSchema).length(10),
  equipments: z.array(playerEquipmentSchema),
}).transform((value) => ({
  ...value,
  baseStatus: value.status.baseValues,
  effectiveStatus: value.status.effectiveValues,
  status: value.status.effectiveValues,
}))

export const playerSummarySchema = z.object({
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  imagePath: z.string().min(1).nullable().optional(),
})

export const listPlayersResponseSchema = z.array(playerSummarySchema)

export const createPlayerRequestSchema = z.object({
  userName: playerUserNameSchema,
})

export const createPlayerResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  imagePath: z.string().min(1).nullable().optional(),
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

export const updatePlayerImageRequestSchema = z.object({
  imageNo: z.number().int().min(1, '画像番号は1以上で入力してください'),
})

export const updatePlayerImageResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  imagePath: z.string().min(1).nullable().optional(),
})

export const updatePlayerJobRequestSchema = z.object({
  job: z.number().int().min(1).max(6),
})

export const updatePlayerJobResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
  newlyLearnedMoves: z.array(learnedMoveSchema),
})

export type GetPlayerResponse = z.infer<typeof getPlayerResponseSchema>
export type PlayerSummary = z.infer<typeof playerSummarySchema>
export type ListPlayersResponse = z.infer<typeof listPlayersResponseSchema>
export type PlayerMoveSlot = z.infer<typeof playerMoveSlotSchema>
export type CreatePlayerRequest = z.infer<typeof createPlayerRequestSchema>
export type CreatePlayerResponse = z.infer<typeof createPlayerResponseSchema>
export type UpdatePlayerNameRequest = z.infer<typeof updatePlayerNameRequestSchema>
export type UpdatePlayerNameResponse = z.infer<typeof updatePlayerNameResponseSchema>
export type UpdatePlayerImageRequest = z.infer<typeof updatePlayerImageRequestSchema>
export type UpdatePlayerImageResponse = z.infer<typeof updatePlayerImageResponseSchema>
export type UpdatePlayerJobRequest = z.infer<typeof updatePlayerJobRequestSchema>
export type UpdatePlayerJobResponse = z.infer<typeof updatePlayerJobResponseSchema>
