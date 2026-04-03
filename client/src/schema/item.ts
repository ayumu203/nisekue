import { z } from 'zod'
import { playerJobCodeSchema } from '@/schema/player'

const statusBonusSchema = z.object({
  maxHp: z.number().int(),
  maxMp: z.number().int(),
  strength: z.number().int(),
  defense: z.number().int(),
  intelligence: z.number().int(),
  luck: z.number().int(),
  speed: z.number().int(),
})

const statusBonusPercentSchema = z.object({
  maxHp: z.number().int().min(0),
  maxMp: z.number().int().min(0),
  strength: z.number().int().min(0),
  defense: z.number().int().min(0),
  intelligence: z.number().int().min(0),
  luck: z.number().int().min(0),
  speed: z.number().int().min(0),
})

export const itemEquipmentTypeSchema = z.enum(['Weapon', 'Armor'])
export const itemEquipmentStatusSchema = z.enum(['Inventory', 'Equipped', 'Broken'])
export const itemEffectTypeSchema = z.enum(['StatBoost', 'ChangeJob'])
export const marketListingCategorySchema = z.enum(['Weapon', 'Armor', 'Item', 'Map'])
export const marketEquipmentDetailSchema = z.object({
  equipmentType: itemEquipmentTypeSchema,
  durability: z.number().int().min(0),
  maxDurability: z.number().int().min(1),
  mastery: z.number().int().min(0),
  masteryCap: z.number().int().min(0),
  statusBonus: statusBonusSchema,
})

export const itemEquipmentViewSchema = z.object({
  kind: z.literal('equipment'),
  playerEquipmentId: z.string().uuid(),
  equipmentId: z.number().int().min(1),
  name: z.string().min(1),
  flavorText: z.string(),
  equipmentType: itemEquipmentTypeSchema,
  status: itemEquipmentStatusSchema,
  durability: z.number().int().min(0),
  maxDurability: z.number().int().min(1),
  mastery: z.number().int().min(0),
  masteryCap: z.number().int().min(0),
  synthesisGoldCost: z.number().int().min(0),
  statusBonus: statusBonusSchema,
})

export const itemStackViewSchema = z.object({
  kind: z.literal('item'),
  itemStackId: z.string().uuid(),
  itemId: z.number().int().min(1),
  name: z.string().min(1),
  flavorText: z.string(),
  quantity: z.number().int().min(1),
  effectType: itemEffectTypeSchema,
  requiredLevel: z.number().int().min(1).nullable(),
  changeJobTo: playerJobCodeSchema.nullable(),
  statusBonus: statusBonusSchema.nullable(),
  statusBonusPercent: statusBonusPercentSchema.nullable(),
})

export const inventoryItemViewSchema = z.discriminatedUnion('kind', [itemEquipmentViewSchema, itemStackViewSchema])

export const getItemsResponseSchema = z.object({
  capacity: z.number().int().min(1),
  usedSlots: z.number().int().min(0),
  gold: z.number().int().min(0),
  equippedItems: z.array(itemEquipmentViewSchema),
  inventoryItems: z.array(inventoryItemViewSchema),
})

export const useItemRequestSchema = z.object({
  quantity: z.number().int().min(1),
})

export const itemActionResponseSchema = z.object({
  message: z.string().min(1),
})

export const synthesizeEquipmentResponseSchema = z.object({
  message: z.string().min(1),
  targetPlayerEquipmentId: z.string().uuid(),
  durability: z.number().int().min(0),
  maxDurability: z.number().int().min(1),
  gold: z.number().int().min(0),
})

export const createMarketListingRequestSchema = z
  .object({
    playerEquipmentId: z.string().uuid().nullable().optional(),
    itemStackId: z.string().uuid().nullable().optional(),
    quantity: z.number().int().min(1),
    unitPrice: z.number().int().min(1),
  })
  .refine((value) => Boolean(value.playerEquipmentId) !== Boolean(value.itemStackId), {
    message: '装備かアイテムのどちらか一方を指定してください',
  })

export const createMarketListingResponseSchema = z.object({
  message: z.string().min(1),
  listingId: z.string().uuid(),
})

export const marketListingViewSchema = z.object({
  listingId: z.string().uuid(),
  itemName: z.string().min(1),
  flavorText: z.string(),
  sellerId: z.string().uuid().optional(),
  sellerName: z.string().min(1).optional(),
  sellerImagePath: z.string().nullable().optional(),
  quantity: z.number().int().min(0),
  unitPrice: z.number().int().min(1),
  expiresAt: z.string().datetime({ offset: true }),
  listingCategory: marketListingCategorySchema,
  equipmentDetail: marketEquipmentDetailSchema.nullable().optional(),
})

export const getMarketListingsResponseSchema = z.array(marketListingViewSchema)

export const purchaseMarketListingRequestSchema = z.object({
  quantity: z.number().int().min(1),
})

export const purchaseMarketListingResponseSchema = z.object({
  message: z.string().min(1),
  gold: z.number().int().min(0),
})

export type ItemEquipmentView = z.infer<typeof itemEquipmentViewSchema>
export type ItemStackView = z.infer<typeof itemStackViewSchema>
export type InventoryItemView = z.infer<typeof inventoryItemViewSchema>
export type GetItemsResponse = z.infer<typeof getItemsResponseSchema>
export type UseItemRequest = z.infer<typeof useItemRequestSchema>
export type ItemActionResponse = z.infer<typeof itemActionResponseSchema>
export type SynthesizeEquipmentResponse = z.infer<typeof synthesizeEquipmentResponseSchema>
export type CreateMarketListingRequest = z.infer<typeof createMarketListingRequestSchema>
export type CreateMarketListingResponse = z.infer<typeof createMarketListingResponseSchema>
export type MarketListingView = z.infer<typeof marketListingViewSchema>
export type MarketEquipmentDetail = z.infer<typeof marketEquipmentDetailSchema>
export type GetMarketListingsResponse = z.infer<typeof getMarketListingsResponseSchema>
export type PurchaseMarketListingRequest = z.infer<typeof purchaseMarketListingRequestSchema>
export type PurchaseMarketListingResponse = z.infer<typeof purchaseMarketListingResponseSchema>
