import { z } from 'zod'

export const treasureMapGradeSchema = z.enum(['D', 'C', 'B', 'A', 'S'])

export const treasureMapSummarySchema = z.object({
  mapId: z.number().int().min(1),
  code: z.string().min(1),
  name: z.string().min(1),
  description: z.string(),
  grade: treasureMapGradeSchema,
  durationSeconds: z.number().int().min(1),
  isMarketable: z.boolean(),
  isHiddenFromInventory: z.boolean(),
  ownedQuantity: z.number().int().min(0),
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
export type TreasureMapReward = z.infer<typeof treasureMapRewardSchema>
export type TreasureMapExpedition = z.infer<typeof treasureMapExpeditionSchema>
export type StartTreasureMapExpeditionRequest = z.infer<typeof startTreasureMapExpeditionRequestSchema>
export type ClaimTreasureMapRewardResponse = z.infer<typeof claimTreasureMapRewardResponseSchema>
export type GetTreasureMapsResponse = z.infer<typeof getTreasureMapsResponseSchema>
export type GetCurrentTreasureMapExpeditionResponse = z.infer<typeof getCurrentTreasureMapExpeditionResponseSchema>
