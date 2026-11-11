import { z } from 'zod'

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
