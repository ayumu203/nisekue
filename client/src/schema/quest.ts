import { z } from 'zod'
import { playerIdSchema, playerUserNameSchema } from '@/schema/player'

export const questRoomIdSchema = z.string().uuid('ルームIDの形式が不正です')
export const questRunIdSchema = z.string().uuid('クエスト実行IDの形式が不正です')
export const questParticipantIdSchema = z.string().uuid('参加者IDの形式が不正です')
export const questEnemyInstanceIdSchema = z.string().uuid('敵インスタンスIDの形式が不正です')

export const battleRowSchema = z.enum(['Front', 'Middle', 'Back'])
export const battleColumnSchema = z.enum(['Left', 'Right'])
export const battlePositionSchema = z.object({
  row: battleRowSchema,
  column: battleColumnSchema,
})

export const questRoomModeSchema = z.enum(['Solo', 'Multi'])
export const questRoomStatusSchema = z.enum(['Recruiting', 'Closed'])
export const questRunStatusSchema = z.enum(['InProgress', 'Succeeded', 'Failed', 'Aborted'])
export const questParticipantTypeSchema = z.enum(['Player', 'Npc'])
export const questActionModeSchema = z.enum(['Manual', 'AutoAttackOnly'])
export const questActionKindSchema = z.enum(['NormalAttack', 'UseMove', 'Guard', 'Wait', 'LeaveQuest', 'Escape'])
export const questManualControlRequestStatusSchema = z.enum(['None', 'Pending'])
export const questTargetResultTypeSchema = z.enum([
  'Hit',
  'Miss',
  'Guarded',
  'Healed',
  'BuffApplied',
  'AilmentApplied',
  'Defeated',
])

export const questStageSummarySchema = z.object({
  stageId: z.number().int().positive(),
  stageCode: z.string().min(1),
  name: z.string().min(1),
  recommendedLevel: z.number().int().nonnegative(),
  minPartyMemberCount: z.number().int().positive(),
  maxPartyMemberCount: z.number().int().positive(),
  isActive: z.boolean(),
  floors: z.array(
    z.object({
      floorNo: z.number().int().positive(),
      floorType: z.enum(['Normal', 'Boss']),
      enemyCount: z.number().int().nonnegative(),
    }),
  ),
})

export const getQuestStagesResponseSchema = z.array(questStageSummarySchema)

export const questRoomParticipantSchema = z.object({
  participantId: questParticipantIdSchema,
  type: questParticipantTypeSchema,
  playerId: playerIdSchema.nullable().optional(),
  npcTemplateId: z.number().int().positive().nullable().optional(),
  displayName: z.string().min(1),
  status: z.enum(['Joined', 'Disconnected', 'Left']),
  isOwner: z.boolean(),
  position: battlePositionSchema,
  joinedAt: z.string().datetime({ offset: true }),
  lastSeenAt: z.string().datetime({ offset: true }).nullable(),
  leftAt: z.string().datetime({ offset: true }).nullable(),
})

export const questRoomDetailResponseSchema = z.object({
  roomId: questRoomIdSchema,
  ownerPlayerId: playerIdSchema,
  stageId: z.number().int().positive(),
  mode: questRoomModeSchema,
  status: questRoomStatusSchema,
  version: z.number().int().nonnegative(),
  closeReason: z.enum(['Started', 'Cancelled', 'Expired']).nullable(),
  createdAt: z.string().datetime({ offset: true }),
  closedAt: z.string().datetime({ offset: true }).nullable(),
  canStart: z.boolean(),
  formation: z.object({
    occupiedPositions: z.array(battlePositionSchema),
  }),
  participants: z.array(questRoomParticipantSchema),
})

export const createQuestRoomRequestSchema = z.object({
  stageId: z.number().int().positive(),
  mode: questRoomModeSchema,
})

export const updateQuestRoomPositionRequestSchema = z.object({
  participantId: questParticipantIdSchema,
  row: battleRowSchema,
  column: battleColumnSchema,
})

export const listQuestRoomsRequestSchema = z.object({
  stageId: z.number().int().positive().optional(),
  mode: questRoomModeSchema.optional(),
  status: questRoomStatusSchema.optional(),
  ownerPlayerId: playerIdSchema.optional(),
  page: z.number().int().positive().optional(),
  pageSize: z.number().int().positive().optional(),
})

export const questRoomSummaryResponseSchema = z.object({
  roomId: questRoomIdSchema,
  stageId: z.number().int().positive(),
  stageName: z.string().min(1).nullable().optional(),
  mode: questRoomModeSchema,
  status: questRoomStatusSchema,
  ownerPlayerId: playerIdSchema,
  ownerDisplayName: playerUserNameSchema.nullable().optional(),
  participantCount: z.number().int().nonnegative(),
  minPartyMemberCount: z.number().int().positive().nullable().optional(),
  maxPartyMemberCount: z.number().int().positive().nullable().optional(),
  createdAt: z.string().datetime({ offset: true }),
})

export const listQuestRoomsResponseSchema = z.array(questRoomSummaryResponseSchema)

export const questActiveEffectSchema = z.object({
  effectType: z.string().min(1),
  displayName: z.string().min(1),
  remainingTurns: z.number().int().nullable(),
  stacks: z.number().int().nullable(),
})

export const questPartyMemberViewSchema = z.object({
  participantId: questParticipantIdSchema,
  type: questParticipantTypeSchema,
  displayName: z.string().min(1),
  imagePath: z.string().min(1).nullable().optional(),
  position: battlePositionSchema,
  currentHp: z.number().int().nonnegative(),
  currentMp: z.number().int().nonnegative(),
  maxHp: z.number().int().positive().nullable().optional(),
  maxMp: z.number().int().nonnegative().nullable().optional(),
  isDead: z.boolean(),
  canActFromTurn: z.number().int().positive(),
  actionMode: questActionModeSchema,
  manualControlRequestStatus: questManualControlRequestStatusSchema,
  activeEffects: z.array(questActiveEffectSchema),
})

export const questEnemyViewSchema = z.object({
  enemyInstanceId: questEnemyInstanceIdSchema,
  enemyDefinitionId: z.number().int().positive(),
  name: z.string().min(1),
  imagePath: z.string().min(1).nullable().optional(),
  position: battlePositionSchema,
  currentHp: z.number().int().nonnegative(),
  currentMp: z.number().int().nonnegative(),
  maxHp: z.number().int().positive().nullable().optional(),
  maxMp: z.number().int().nonnegative().nullable().optional(),
  isDead: z.boolean(),
  activeEffects: z.array(questActiveEffectSchema),
})

export const questPendingCommandViewSchema = z.object({
  participantId: questParticipantIdSchema,
  turnNo: z.number().int().positive(),
  actionKind: questActionKindSchema,
  moveId: z.number().int().positive().nullable(),
  selectedTargetPosition: battlePositionSchema.nullable(),
  isAutoSubmitted: z.boolean(),
  submittedAt: z.string().datetime({ offset: true }),
})

export const questChatMessageViewSchema = z.object({
  senderParticipantId: questParticipantIdSchema,
  displayName: z.string().min(1),
  imagePath: z.string().min(1).nullable().optional(),
  message: z.string().min(1),
  sentAt: z.string().datetime({ offset: true }),
})

export const questRewardViewSchema = z.object({
  exp: z.number().int().nonnegative(),
})

export const questActionTargetResultViewSchema = z.object({
  targetParticipantId: questParticipantIdSchema.nullable(),
  targetEnemyInstanceId: questEnemyInstanceIdSchema.nullable(),
  targetDisplayName: z.string().min(1),
  resultType: questTargetResultTypeSchema,
  hpChange: z.number().int(),
  mpChange: z.number().int(),
  appliedEffects: z.array(z.string().min(1)),
  removedEffects: z.array(z.string().min(1)),
  isDeadAfterAction: z.boolean(),
})

export const questResolvedActionViewSchema = z.object({
  actorParticipantId: questParticipantIdSchema.nullable(),
  actorEnemyInstanceId: questEnemyInstanceIdSchema.nullable(),
  actorDisplayName: z.string().min(1),
  actionKind: questActionKindSchema,
  moveId: z.number().int().positive().nullable(),
  moveName: z.string().min(1).nullable(),
  succeeded: z.boolean(),
  targetSummaries: z.array(questActionTargetResultViewSchema),
  logs: z.array(z.string()),
})

export const questFloorTransitionViewSchema = z.object({
  previousFloorNo: z.number().int().positive(),
  currentFloorNo: z.number().int().positive(),
  floorCleared: z.boolean(),
  bossFloorReached: z.boolean(),
})

export const questRunTransitionViewSchema = z.object({
  previousStatus: questRunStatusSchema,
  currentStatus: questRunStatusSchema,
  questEnded: z.boolean(),
})

export const questLastTurnResultsViewSchema = z.object({
  turnNo: z.number().int().positive(),
  resolvedAt: z.string().datetime({ offset: true }),
  actions: z.array(questResolvedActionViewSchema),
  floorTransition: questFloorTransitionViewSchema.nullable(),
  runTransition: questRunTransitionViewSchema.nullable(),
})

export const questRunDetailResponseSchema = z.object({
  runId: questRunIdSchema,
  roomId: questRoomIdSchema,
  stageId: z.number().int().positive(),
  status: questRunStatusSchema,
  floor: z.object({
    currentFloorNo: z.number().int().positive(),
    isBossFloor: z.boolean(),
  }),
  turn: z.object({
    currentTurnNo: z.number().int().positive(),
    actionDeadlineAt: z.string().datetime({ offset: true }),
    waitingParticipantIds: z.array(questParticipantIdSchema),
  }),
  partyMembers: z.array(questPartyMemberViewSchema),
  enemies: z.array(questEnemyViewSchema),
  pendingCommands: z.array(questPendingCommandViewSchema),
  chatMessages: z.array(questChatMessageViewSchema),
  rewards: questRewardViewSchema,
  lastTurnResults: questLastTurnResultsViewSchema.nullable(),
})

export const submitQuestCommandRequestSchema = z
  .object({
    participantId: questParticipantIdSchema,
    turnNo: z.number().int().positive(),
    actionKind: questActionKindSchema,
    moveId: z.number().int().positive().nullable().optional(),
    targetRow: battleRowSchema.nullable().optional(),
    targetColumn: battleColumnSchema.nullable().optional(),
  })
  .superRefine((value, ctx) => {
    const hasTargetRow = value.targetRow != null
    const hasTargetColumn = value.targetColumn != null
    if (hasTargetRow !== hasTargetColumn) {
      ctx.addIssue({
        code: z.ZodIssueCode.custom,
        message: 'targetRow と targetColumn は両方指定するか、両方省略してください',
        path: hasTargetRow ? ['targetColumn'] : ['targetRow'],
      })
    }
  })

export const submitQuestCommandResponseSchema = z.object({
  accepted: z.boolean(),
  resolvedInThisRequest: z.boolean(),
})

export const manualControlRequestSchema = z.object({
  participantId: questParticipantIdSchema,
})

export const manualControlResponseSchema = z.object({
  accepted: z.boolean(),
})

export const postQuestChatMessageRequestSchema = z.object({
  participantId: questParticipantIdSchema,
  message: z.string().trim().min(1),
})

export const postQuestChatMessageResponseSchema = z.object({
  accepted: z.boolean(),
})

export const questRunHubSnapshotEventSchema = questRunDetailResponseSchema
export const questRunHubUpdatedEventSchema = questRunDetailResponseSchema
export const questRunHubErrorEventSchema = z.object({
  code: z.string().min(1),
  message: z.string().min(1),
})

export type GetQuestStagesResponse = z.infer<typeof getQuestStagesResponseSchema>
export type CreateQuestRoomRequest = z.infer<typeof createQuestRoomRequestSchema>
export type QuestRoomDetailResponse = z.infer<typeof questRoomDetailResponseSchema>
export type ListQuestRoomsRequest = z.infer<typeof listQuestRoomsRequestSchema>
export type QuestRoomSummaryResponse = z.infer<typeof questRoomSummaryResponseSchema>
export type ListQuestRoomsResponse = z.infer<typeof listQuestRoomsResponseSchema>
export type QuestRunDetailResponse = z.infer<typeof questRunDetailResponseSchema>
export type SubmitQuestCommandRequest = z.infer<typeof submitQuestCommandRequestSchema>
export type SubmitQuestCommandResponse = z.infer<typeof submitQuestCommandResponseSchema>
export type ManualControlRequest = z.infer<typeof manualControlRequestSchema>
export type ManualControlResponse = z.infer<typeof manualControlResponseSchema>
export type PostQuestChatMessageRequest = z.infer<typeof postQuestChatMessageRequestSchema>
export type PostQuestChatMessageResponse = z.infer<typeof postQuestChatMessageResponseSchema>
export type QuestRunHubSnapshotEvent = z.infer<typeof questRunHubSnapshotEventSchema>
export type QuestRunHubUpdatedEvent = z.infer<typeof questRunHubUpdatedEventSchema>
export type QuestRunHubErrorEvent = z.infer<typeof questRunHubErrorEventSchema>
