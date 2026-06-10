import { z } from 'zod'

export const petStatusSchema = z.object({
  maxHp: z.number().int().nonnegative(),
  maxMp: z.number().int().nonnegative(),
  strength: z.number().int().nonnegative(),
  defense: z.number().int().nonnegative(),
  intelligence: z.number().int().nonnegative(),
  luck: z.number().int().nonnegative(),
  speed: z.number().int().nonnegative(),
})

export const playerPetViewSchema = z.object({
  petId: z.string().uuid(),
  enemyDefinitionId: z.number().int().positive(),
  name: z.string().min(1),
  imagePath: z.string().min(1).nullable().optional(),
  level: z.number().int().positive(),
  isStandby: z.boolean(),
  capturedAt: z.string().datetime({ offset: true }),
  bonusStatus: petStatusSchema,
  totalStatus: petStatusSchema,
})

export const getPetsResponseSchema = z.object({
  maxPetCount: z.number().int().positive(),
  trainingCostGold: z.number().int().positive(),
  pets: z.array(playerPetViewSchema),
})

export const trainPetResponseSchema = playerPetViewSchema

export type PetStatus = z.infer<typeof petStatusSchema>
export type PlayerPetView = z.infer<typeof playerPetViewSchema>
export type GetPetsResponse = z.infer<typeof getPetsResponseSchema>
