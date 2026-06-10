import { z } from 'zod'
import { moveAttackRangeSchema, moveTargetLifeStateSchema, moveTargetTypeSchema } from '@/schema/player'
import { battleColumnSchema, battleRowSchema } from '@/schema/quest'

export type { BattleColumn, BattleRow } from '@/schema/quest'

export const petBattleRoomStatusSchema = z.enum(['WaitingForStart', 'Closed'])
export const petBattleRunStatusSchema = z.enum(['InProgress', 'OwnerWon', 'OpponentWon', 'Aborted'])
export const petBattleActionKindSchema = z.enum(['NormalAttack', 'UseMove', 'Guard', 'Wait'])

export const petBattleStatsSchema = z.object({
  rating: z.number().int().nonnegative(),
  wins: z.number().int().nonnegative(),
  losses: z.number().int().nonnegative(),
  totalBattles: z.number().int().nonnegative(),
  updatedAt: z.string().datetime({ offset: true }),
})

export const getPetBattleStatsResponseSchema = petBattleStatsSchema.nullable()

export const petBattleRoomSlotSchema = z.object({
  petId: z.string().uuid(),
  row: battleRowSchema,
  column: battleColumnSchema,
})

export const petBattleRoomSchema = z.object({
  roomId: z.string().uuid(),
  status: petBattleRoomStatusSchema,
  ownerPlayerId: z.string().uuid(),
  ownerPlayerName: z.string().min(1).nullable().optional(),
  opponentPlayerId: z.string().uuid(),
  opponentPlayerName: z.string().min(1).nullable().optional(),
  slots: z.array(petBattleRoomSlotSchema),
  createdAt: z.string().datetime({ offset: true }),
})

export const petBattleMemberMoveSchema = z.object({
  moveId: z.number().int().min(1),
  name: z.string().min(1),
  mpCost: z.number().int().min(0),
  targetType: moveTargetTypeSchema,
  targetLifeState: moveTargetLifeStateSchema,
  attackRange: moveAttackRangeSchema,
})

export const petBattleActiveEffectSchema = z.object({
  effectType: z.string().min(1),
  remainingTurns: z.number().int(),
})

export const petBattleMemberSchema = z.object({
  participantId: z.string().uuid(),
  enemyDefinitionId: z.number().int().positive(),
  displayName: z.string().min(1),
  imagePath: z.string().min(1).nullable().optional(),
  maxHp: z.number().int().nonnegative(),
  maxMp: z.number().int().nonnegative(),
  currentHp: z.number().int().nonnegative(),
  currentMp: z.number().int().nonnegative(),
  isDead: z.boolean(),
  moves: z.array(petBattleMemberMoveSchema),
  startRow: battleRowSchema,
  startColumn: battleColumnSchema,
  activeEffects: z.array(petBattleActiveEffectSchema),
})

export const petBattleTargetSummarySchema = z.object({
  targetParticipantId: z.string().uuid(),
  targetDisplayName: z.string().min(1),
  resultType: z.string().min(1),
  hpChange: z.number().int(),
  mpChange: z.number().int(),
  appliedEffects: z.array(z.string()),
  isDeadAfterAction: z.boolean(),
})

export const petBattleResolvedActionSchema = z.object({
  actorParticipantId: z.string().uuid(),
  actorDisplayName: z.string().min(1),
  actionKind: z.string().min(1),
  moveId: z.number().int().nullable().optional(),
  moveName: z.string().min(1).nullable().optional(),
  succeeded: z.boolean(),
  targetSummaries: z.array(petBattleTargetSummarySchema),
})

export const petBattleLastTurnResultsSchema = z.object({
  turnNo: z.number().int().positive(),
  resolvedAt: z.string().datetime({ offset: true }),
  actions: z.array(petBattleResolvedActionSchema),
})

export const petBattleRunSchema = z.object({
  runId: z.string().uuid(),
  roomId: z.string().uuid(),
  status: petBattleRunStatusSchema,
  ownerPlayerId: z.string().uuid(),
  ownerPlayerName: z.string().min(1).nullable().optional(),
  opponentPlayerId: z.string().uuid(),
  opponentPlayerName: z.string().min(1).nullable().optional(),
  winnerPlayerId: z.string().uuid().nullable().optional(),
  currentTurnNo: z.number().int().positive(),
  actionDeadlineAt: z.string().datetime({ offset: true }),
  startedAt: z.string().datetime({ offset: true }),
  endedAt: z.string().datetime({ offset: true }).nullable().optional(),
  waitingParticipantIds: z.array(z.string().uuid()),
  submittedParticipantIds: z.array(z.string().uuid()),
  ownerMembers: z.array(petBattleMemberSchema),
  opponentMembers: z.array(petBattleMemberSchema),
  lastTurnResults: petBattleLastTurnResultsSchema.nullable().optional(),
})

export const getPetBattleStatusResponseSchema = z.object({
  room: petBattleRoomSchema.nullable().optional(),
  run: petBattleRunSchema.nullable().optional(),
})

export const assignPetBattleSlotRequestSchema = z.object({
  petId: z.string().uuid(),
  row: battleRowSchema,
  column: battleColumnSchema,
})

export const submitPetBattleCommandRequestSchema = z.object({
  participantId: z.string().uuid(),
  turnNo: z.number().int().positive(),
  actionKind: petBattleActionKindSchema,
  moveId: z.number().int().min(1).nullable(),
  targetRow: battleRowSchema.nullable(),
  targetColumn: battleColumnSchema.nullable(),
})

export const submitPetBattleCommandResponseSchema = z.object({
  accepted: z.boolean(),
  resolvedInThisRequest: z.boolean(),
  run: petBattleRunSchema,
})

export type PetBattleStats = z.infer<typeof petBattleStatsSchema>
export type PetBattleRoom = z.infer<typeof petBattleRoomSchema>
export type PetBattleRun = z.infer<typeof petBattleRunSchema>
export type PetBattleMember = z.infer<typeof petBattleMemberSchema>
export type PetBattleMemberMove = z.infer<typeof petBattleMemberMoveSchema>
export type PetBattleActionKind = z.infer<typeof petBattleActionKindSchema>
export type PetBattleLastTurnResults = z.infer<typeof petBattleLastTurnResultsSchema>
export type GetPetBattleStatusResponse = z.infer<typeof getPetBattleStatusResponseSchema>
export type AssignPetBattleSlotRequest = z.infer<typeof assignPetBattleSlotRequestSchema>
export type SubmitPetBattleCommandRequest = z.infer<typeof submitPetBattleCommandRequestSchema>
export type SubmitPetBattleCommandResponse = z.infer<typeof submitPetBattleCommandResponseSchema>
