import { z } from 'zod'

export const treasureMapGradeSchema = z.enum(['D', 'C', 'B', 'A', 'S'])

export const treasureMapRewardTendencySchema = z.object({
  itemRate: z.number().min(0),
  equipmentRate: z.number().min(0),
  experienceRate: z.number().min(0),
  goldRate: z.number().min(0),
})

export const treasureMapRewardItemCandidateSchema = z.object({
  itemId: z.number().int().min(1),
  name: z.string().min(1),
  quantityMin: z.number().int().min(1),
  quantityMax: z.number().int().min(1),
  weight: z.number().int().min(1),
})

export const treasureMapRewardEquipmentCandidateSchema = z.object({
  equipmentId: z.number().int().min(1),
  name: z.string().min(1),
  weight: z.number().int().min(1),
})

export const treasureMapRewardAmountCandidateSchema = z.object({
  amount: z.number().int().min(0),
  weight: z.number().int().min(1),
})

export const treasureMapRewardCandidatesSchema = z.object({
  items: z.array(treasureMapRewardItemCandidateSchema),
  equipments: z.array(treasureMapRewardEquipmentCandidateSchema),
  experiences: z.array(treasureMapRewardAmountCandidateSchema),
  golds: z.array(treasureMapRewardAmountCandidateSchema),
})

export const treasureMapSummarySchema = z.object({
  mapId: z.number().int().min(1),
  code: z.string().min(1),
  name: z.string().min(1),
  description: z.string(),
  narrativeText: z.string(),
  grade: treasureMapGradeSchema,
  durationSeconds: z.number().int().min(1),
  isMarketable: z.boolean(),
  isHiddenFromInventory: z.boolean(),
  ownedQuantity: z.number().int().min(0),
  rewardTendency: treasureMapRewardTendencySchema,
  rewardCandidates: treasureMapRewardCandidatesSchema,
})

export const treasureMapRewardSchema = z.object({
  itemIds: z.array(z.number().int().min(1)),
  equipmentIds: z.array(z.number().int().min(1)),
  experiencePoints: z.number().int().min(0),
  gold: z.number().int().min(0),
})

export const treasureMapExpeditionSchema = z.object({
  expeditionId: z.string().uuid(),
  mapId: z.number().int().min(1),
  startedAt: z.string().datetime({ offset: true }),
  endsAt: z.string().datetime({ offset: true }),
  status: z.enum(['InProgress', 'Completed', 'Claimed', 'Failed']),
  rewardClaimed: z.boolean(),
  completedAt: z.string().datetime({ offset: true }).nullable(),
  reward: treasureMapRewardSchema.nullable(),
})

export const startTreasureMapExpeditionRequestSchema = z.object({
  mapId: z.number().int().min(1),
})

export const claimTreasureMapRewardResponseSchema = z.object({
  message: z.string().min(1),
  expedition: treasureMapExpeditionSchema,
})

export const getTreasureMapsResponseSchema = z.array(treasureMapSummarySchema)
export const getCurrentTreasureMapExpeditionResponseSchema = treasureMapExpeditionSchema.nullable()

export type TreasureMapSummary = z.infer<typeof treasureMapSummarySchema>
export type TreasureMapRewardTendency = z.infer<typeof treasureMapRewardTendencySchema>
export type TreasureMapRewardCandidates = z.infer<typeof treasureMapRewardCandidatesSchema>
export type TreasureMapReward = z.infer<typeof treasureMapRewardSchema>
export type TreasureMapExpedition = z.infer<typeof treasureMapExpeditionSchema>
export type StartTreasureMapExpeditionRequest = z.infer<typeof startTreasureMapExpeditionRequestSchema>
export type ClaimTreasureMapRewardResponse = z.infer<typeof claimTreasureMapRewardResponseSchema>
export type GetTreasureMapsResponse = z.infer<typeof getTreasureMapsResponseSchema>
export type GetCurrentTreasureMapExpeditionResponse = z.infer<typeof getCurrentTreasureMapExpeditionResponseSchema>
