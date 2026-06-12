import { z } from 'zod'
import { pagedResponseSchema } from '@/schema/pagination'

export const playerIdSchema = z.string().uuid('ユーザーIDの形式が不正です')

export const playerUserNameSchema = z
  .string()
  .trim()
  .min(1, 'ユーザー名を入力してください')
  .max(20, 'ユーザー名は20文字以内で入力してください')

export const playerJobCodeSchema = z.enum([
  'Apprentice',
  'Warrior',
  'Guardian',
  'Mage',
  'Priest',
  'Ranger',
  'OniWarrior',
  'SwordMaster',
  'Trickster',
  'Crusader',
  'FireMage',
  'WaterMage',
  'WindMage',
  'HighPriest',
  'Necromancer',
  'Sniper',
  'TrapMaster',
  'GrandWarrior',
  'GrandGuard',
  'GrandCaster',
  'GrandPriest',
  'GrandRanger',
  'Shogun',
  'Archmage',
  'GreatThief',
  'Bushin',
  'Seikaiou',
  'Matouou',
  'Shugoshin',
])

export const playerJobSchema = z.object({
  code: playerJobCodeSchema,
  value: z.number().int().min(1).max(30),
  displayName: z.string().min(1, 'ジョブ名が空です'),
  description: z.string().min(1, 'ジョブ説明が空です'),
})

export type PlayerJob = z.infer<typeof playerJobSchema>
export type PlayerJobCode = z.infer<typeof playerJobCodeSchema>

export const moveTargetTypeSchema = z.enum(['Enemy', 'Ally', 'Self'])
export const moveTargetLifeStateSchema = z.enum(['Alive', 'Dead', 'Any'])
const moveAttackRangeCanonicalSchema = z.enum(['Single', 'Column', 'Row', 'Square', 'All'])
const moveAttackRangeLegacyInputSchema = z.enum([
  'Single',
  'Column',
  'Row',
  'Square',
  'All',
  'AcrossRows',
  'AcrossColumns',
])
export const moveAttackRangeSchema = z.preprocess((value) => {
  const parsedValue = moveAttackRangeLegacyInputSchema.safeParse(value)

  if (!parsedValue.success) {
    return value
  }

  if (parsedValue.data === 'AcrossRows') {
    return 'Column'
  }

  if (parsedValue.data === 'AcrossColumns') {
    return 'Row'
  }

  return parsedValue.data
}, moveAttackRangeCanonicalSchema)
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

export const moveEffectTypeSchema = z.enum([
  'Damage',
  'Heal',
  'RestoreMp',
  'Ailment',
  'Buff',
  'Knockout',
  'HalveSelfHp',
])
export const moveBuffStatSchema = z.enum([
  'MaxHp',
  'MaxMp',
  'Strength',
  'Defense',
  'Intelligence',
  'Luck',
  'Speed',
  'Accuracy',
  'Evasion',
  'CriticalChance',
  'DamageReduction',
  'StrengthIntelligence',
])
export const moveAilmentTypeSchema = z.enum([
  'Paralysis',
  'Poison',
  'Sleep',
  'Burn',
  'Taunt',
  'PoisonTrap',
  'DamageTrap',
  'InstantDeath',
  'Regeneration',
  'CoverAll',
])

export const moveEffectSummarySchema = z.object({
  effectType: moveEffectTypeSchema,
  powerRate: z.number().nonnegative().nullable().optional(),
  buffStat: moveBuffStatSchema.nullable().optional(),
  buffTurns: z.number().int().min(1).nullable().optional(),
  ailmentType: moveAilmentTypeSchema.nullable().optional(),
  ailmentTurns: z.number().int().min(1).nullable().optional(),
})

export const playerMoveSlotSchema = z.object({
  slot: z.number().int().min(1).max(10),
  moveId: z.number().int().min(1).nullable(),
  moveName: z.string().min(1).nullable(),
  description: z.string().min(1).nullable(),
  effectImagePath: z.string().min(1).nullable().optional(),
  elementType: moveElementTypeSchema.nullable(),
  targetType: moveTargetTypeSchema.nullable(),
  targetLifeState: moveTargetLifeStateSchema.nullable().optional(),
  attackRange: moveAttackRangeSchema.nullable(),
  mpCost: z.number().int().min(0).nullable(),
  category: moveCategorySchema.nullable(),
  effectSummaries: z.array(moveEffectSummarySchema).nullable().optional(),
})

export type MoveAttackRange = z.infer<typeof moveAttackRangeSchema>
export type MoveAttackRangeInput = z.input<typeof moveAttackRangeSchema>

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

const playerStatusRankSchema = z.enum(['SSS', 'SS', 'S', 'A', 'B', 'C', 'D', 'E', 'F', 'G'])

const playerStatusRanksSchema = z.object({
  maxHp: playerStatusRankSchema,
  maxMp: playerStatusRankSchema,
  strength: playerStatusRankSchema,
  defense: playerStatusRankSchema,
  intelligence: playerStatusRankSchema,
  luck: playerStatusRankSchema,
  speed: playerStatusRankSchema,
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
  plusValue: z.number().int().min(0),
  mastery: z.number().int().min(0),
  canEquipCurrentJob: z.boolean(),
  bonusValues: playerEquipmentBonusValuesSchema,
})

export const getPlayerResponseSchema = z
  .object({
    userId: playerIdSchema,
    userName: playerUserNameSchema.optional(),
    imagePath: z.string().min(1).nullable().optional(),
    job: playerJobSchema,
    jobProfiles: z.array(playerJobSchema),
    masteredJobs: z.array(playerJobSchema).default([]),
    level: z.number().int().min(1, 'レベルは1以上である必要があります'),
    exp: z.number().int().min(0, '経験値は0以上である必要があります'),
    requiredExpForNextLevel: z.number().int().min(1, '次レベル必要経験値は1以上である必要があります').optional(),
    jobLevel: z.number().int().min(1, '職業レベルは1以上である必要があります'),
    jobExp: z.number().int().min(0, '職業経験値は0以上である必要があります'),
    requiredJobExpForNextLevel: z.number().int().min(1, '次職業レベル必要経験値は1以上である必要があります').optional(),
    gold: z.number().int().min(0, 'Goldは0以上である必要があります'),
    status: z.object({
      baseValues: playerStatusValuesSchema,
      baseRanks: playerStatusRanksSchema,
      effectiveValues: playerStatusValuesSchema,
      effectiveRanks: playerStatusRanksSchema,
    }),
    combatIndex: z.object({
      baseValue: z.number().int().min(0),
      baseRank: playerStatusRankSchema,
      effectiveValue: z.number().int().min(0),
      effectiveRank: playerStatusRankSchema,
    }),
    moveSlots: z.array(playerMoveSlotSchema).length(10),
    equipments: z.array(playerEquipmentSchema),
  })
  .transform((value) => ({
    ...value,
    requiredExpForNextLevel: value.requiredExpForNextLevel ?? Math.max(1, value.level * 10),
    requiredJobExpForNextLevel: value.requiredJobExpForNextLevel ?? Math.max(1, value.jobLevel * 10),
    baseStatus: value.status.baseValues,
    baseStatusRanks: value.status.baseRanks,
    effectiveStatus: value.status.effectiveValues,
    effectiveStatusRanks: value.status.effectiveRanks,
    status: value.status.effectiveValues,
    statusRanks: value.status.effectiveRanks,
    combatIndexValue: value.combatIndex.effectiveValue,
    combatIndexRank: value.combatIndex.effectiveRank,
  }))

export const playerSummarySchema = z.object({
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  imagePath: z.string().min(1).nullable().optional(),
  level: z.number().int().min(1, 'レベルは1以上である必要があります'),
  job: playerJobSchema,
  combatIndexRank: playerStatusRankSchema,
})

export const listPlayersResponseSchema = pagedResponseSchema(playerSummarySchema)

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
  job: z.number().int().min(1).max(29),
})

export const updatePlayerJobResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
  newlyLearnedMoves: z.array(learnedMoveSchema),
})

export const updatePlayerEquipmentRequestSchema = z.object({
  equipmentType: playerEquipmentTypeSchema,
  playerEquipmentId: z.string().uuid().nullable(),
})

export const updatePlayerEquipmentResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  player: getPlayerResponseSchema,
})

export const updatePlayerMoveSetRequestSchema = z.object({
  moveIds: z.array(z.number().int().min(1).nullable()).length(10),
})

export const updatePlayerMoveSetResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
})

export const sendPlayerGiftRequestSchema = z
  .object({
    playerEquipmentId: z.string().uuid().nullable().optional(),
    itemStackId: z.string().uuid().nullable().optional(),
    quantity: z.number().int().min(1).nullable().optional(),
  })
  .refine((value) => Boolean(value.playerEquipmentId) !== Boolean(value.itemStackId), {
    message: '装備かアイテムのどちらか一方を指定してください',
  })

export const sendPlayerGiftResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
})

export const rebirthPlayerResponseSchema = z.object({
  message: z.string().min(1, 'レスポンスメッセージが空です'),
  userId: playerIdSchema,
  userName: playerUserNameSchema.optional(),
  job: playerJobSchema,
  level: z.number().int().min(1),
  exp: z.number().int().min(0),
  jobLevel: z.number().int().min(1),
  jobExp: z.number().int().min(0),
  gold: z.number().int().min(0),
  status: z.object({
    baseValues: playerStatusValuesSchema,
  }),
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
export type UpdatePlayerEquipmentRequest = z.infer<typeof updatePlayerEquipmentRequestSchema>
export type UpdatePlayerEquipmentResponse = z.infer<typeof updatePlayerEquipmentResponseSchema>
export type UpdatePlayerMoveSetRequest = z.infer<typeof updatePlayerMoveSetRequestSchema>
export type UpdatePlayerMoveSetResponse = z.infer<typeof updatePlayerMoveSetResponseSchema>
export type SendPlayerGiftRequest = z.infer<typeof sendPlayerGiftRequestSchema>
export type SendPlayerGiftResponse = z.infer<typeof sendPlayerGiftResponseSchema>
export type RebirthPlayerResponse = z.infer<typeof rebirthPlayerResponseSchema>

export const questStageReferenceSchema = z.object({
  id: z.number().int(),
  name: z.string(),
})

export const jobRoadmapRequirementSchema = z.object({
  id: z.number().int(),
  code: playerJobCodeSchema,
  name: z.string(),
})

export const unlockJobRoadmapRequestSchema = z.object({
  jobId: z.number().int().min(1).max(29),
})

export const unlockJobRoadmapResponseSchema = z.object({
  jobId: z.number().int(),
  jobName: z.string(),
  paidGold: z.number().int(),
  remainingGold: z.number().int(),
})

export const jobRoadmapListEntrySchema = z.object({
  jobId: z.number().int(),
  jobCode: playerJobCodeSchema,
  jobName: z.string(),
  rank: z.number().int(),
  isUnlocked: z.boolean(),
  canUnlock: z.boolean(),
  goldCostToUnlock: z.number().int(),
})

export const jobRoadmapListResponseSchema = z.array(jobRoadmapListEntrySchema)

export type JobRoadmapNode = {
  type: 'job' | 'item'
  jobId?: number
  jobCode?: PlayerJobCode
  jobName?: string
  rank?: number
  itemId?: number
  itemName?: string
  isUnlocked: boolean
  goldCostToUnlock?: number | null
  stages?: { id: number; name: string }[] | null
  requiredMasterJobs?: { id: number; code: PlayerJobCode; name: string }[] | null
  requirements: JobRoadmapNode[]
}

const jobRoadmapNodeSchema: z.ZodType<JobRoadmapNode> = z.lazy(() =>
  z.discriminatedUnion('type', [
    z.object({
      type: z.literal('job'),
      jobId: z.number().int(),
      jobCode: playerJobCodeSchema,
      jobName: z.string(),
      rank: z.number().int(),
      isUnlocked: z.boolean(),
      goldCostToUnlock: z.number().int().nullable(),
      requirements: z.array(jobRoadmapNodeSchema),
    }),
    z.object({
      type: z.literal('item'),
      itemId: z.number().int(),
      itemName: z.string(),
      isUnlocked: z.boolean(),
      stages: z.array(questStageReferenceSchema).nullable(),
      requiredMasterJobs: z.array(jobRoadmapRequirementSchema).nullable(),
      requirements: z.array(jobRoadmapNodeSchema),
    }),
  ]),
)

export const jobRoadmapResponseSchema = jobRoadmapNodeSchema

export type UnlockJobRoadmapRequest = z.infer<typeof unlockJobRoadmapRequestSchema>
export type UnlockJobRoadmapResponse = z.infer<typeof unlockJobRoadmapResponseSchema>
export type JobRoadmapListEntry = z.infer<typeof jobRoadmapListEntrySchema>
export type JobRoadmapListResponse = z.infer<typeof jobRoadmapListResponseSchema>
export type JobRoadmapResponse = z.infer<typeof jobRoadmapResponseSchema>
