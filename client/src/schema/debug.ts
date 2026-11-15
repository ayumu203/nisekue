import { z } from 'zod'
import { playerJobCodeSchema } from '@/schema/player'

export const getDebugStatusResponseSchema = z.object({
  canUseDebug: z.boolean(),
})

export type GetDebugStatusResponse = z.infer<typeof getDebugStatusResponseSchema>

/** 204 No Content を返すデバッグ API 用（本文が無いと callDebugApi は null を渡す） */
export const debugNoContentSchema = z.null()

const nonNegativeInt = z.number().int().min(0, '0以上の整数を入力してください')

export const debugUpdateStatusRequestSchema = z.object({
  maxHp: z.number().int().min(1, 'HPは1以上の整数を入力してください'),
  maxMp: nonNegativeInt,
  strength: nonNegativeInt,
  defense: nonNegativeInt,
  intelligence: nonNegativeInt,
  luck: nonNegativeInt,
  speed: nonNegativeInt,
})

export type DebugUpdateStatusRequest = z.infer<typeof debugUpdateStatusRequestSchema>

export const debugLearnMoveRequestSchema = z.object({
  moveId: z.number().int().min(1),
  slotIndex: z.number().int().min(0).max(9).nullable(),
})

export type DebugLearnMoveRequest = z.infer<typeof debugLearnMoveRequestSchema>

export const debugMoveListResponseSchema = z.array(
  z.object({
    moveId: z.number().int().min(1),
    name: z.string(),
  }),
)

export type DebugMoveListResponse = z.infer<typeof debugMoveListResponseSchema>

export const debugItemListResponseSchema = z.array(
  z.object({
    itemId: z.number().int().min(1),
    name: z.string(),
    maxStack: z.number().int().min(1),
  }),
)

export type DebugItemListResponse = z.infer<typeof debugItemListResponseSchema>

export const debugEquipmentListResponseSchema = z.array(
  z.object({
    equipmentId: z.number().int().min(1),
    name: z.string(),
    equipmentType: z.string(),
  }),
)

export type DebugEquipmentListResponse = z.infer<typeof debugEquipmentListResponseSchema>

export const debugGrantItemRequestSchema = z.object({
  itemId: z.number().int().min(1),
  quantity: z.number().int().min(1, '個数は1以上の整数を入力してください'),
})

export type DebugGrantItemRequest = z.infer<typeof debugGrantItemRequestSchema>

export const debugGrantItemResponseSchema = z.object({ quantity: z.number().int().min(0) })

export const debugGrantEquipmentRequestSchema = z.object({
  equipmentId: z.number().int().min(1),
  count: z.number().int().min(1, '個数は1〜20の整数を入力してください').max(20, '個数は1〜20の整数を入力してください'),
  plusValue: z
    .number()
    .int()
    .min(0, '強化値は0〜100の整数を入力してください')
    .max(100, '強化値は0〜100の整数を入力してください'),
})

export type DebugGrantEquipmentRequest = z.infer<typeof debugGrantEquipmentRequestSchema>

export const debugGrantEquipmentResponseSchema = z.object({ count: z.number().int().min(0) })

export const debugLevelUpRequestSchema = z.object({
  level: z
    .number()
    .int()
    .min(1, 'レベルは1〜100の整数を入力してください')
    .max(100, 'レベルは1〜100の整数を入力してください'),
})

export type DebugLevelUpRequest = z.infer<typeof debugLevelUpRequestSchema>

export const debugLevelUpResponseSchema = z.object({
  level: z.number().int().min(1),
  newlyLearnedMoveIds: z.array(z.number().int()),
  hasMasteredCurrentJob: z.boolean(),
})

export type DebugLevelUpResponse = z.infer<typeof debugLevelUpResponseSchema>

export const debugMasterJobsRequestSchema = z.object({
  jobs: z.array(playerJobCodeSchema).min(1, 'ジョブを1つ以上選んでください'),
  unlockRoadmap: z.boolean(),
})

export type DebugMasterJobsRequest = z.infer<typeof debugMasterJobsRequestSchema>

export const debugMasterJobsResponseSchema = z.object({
  newlyMasteredJobs: z.array(z.string()),
  newlyLearnedMoveIds: z.array(z.number().int()),
})

export type DebugMasterJobsResponse = z.infer<typeof debugMasterJobsResponseSchema>

export const debugTemplateListResponseSchema = z.array(
  z.object({
    templateId: z.string().min(1),
    name: z.string(),
  }),
)

export type DebugTemplateListResponse = z.infer<typeof debugTemplateListResponseSchema>

export const debugApplyTemplateRequestSchema = z.object({
  templateId: z.string().min(1, 'テンプレートを選んでください'),
})

export type DebugApplyTemplateRequest = z.infer<typeof debugApplyTemplateRequestSchema>

export const debugApplyTemplateResponseSchema = z.object({
  level: z.number().int().min(1),
  job: z.string(),
  masteredJobs: z.array(z.string()),
})

export const debugChangeJobRequestSchema = z.object({
  job: playerJobCodeSchema,
})

export type DebugChangeJobRequest = z.infer<typeof debugChangeJobRequestSchema>

export const debugChangeJobResponseSchema = z.object({
  job: z.string(),
  jobLevel: z.number().int().min(1),
})

/** サーバー側の金額は C# の int なので、それを超える値は送らない */
const MAX_INT32 = 2147483647

export const debugGrantGoldRequestSchema = z.object({
  amount: z
    .number()
    .int('ゴールドは1〜2147483647の整数を入力してください')
    .min(1, 'ゴールドは1〜2147483647の整数を入力してください')
    .max(MAX_INT32, 'ゴールドは1〜2147483647の整数を入力してください'),
})

export type DebugGrantGoldRequest = z.infer<typeof debugGrantGoldRequestSchema>

export const debugGrantGoldResponseSchema = z.object({ gold: z.number().int().min(0) })

export const debugRebirthResponseSchema = z.object({
  rebirthCount: z.number().int().min(1),
  level: z.number().int().min(1),
})

export type DebugRebirthResponse = z.infer<typeof debugRebirthResponseSchema>

export const debugCompleteTreasureMapResponseSchema = z.object({
  endsAt: z.string().datetime({ offset: true }),
})
