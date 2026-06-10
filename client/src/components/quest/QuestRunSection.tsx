import QuestBattleStatusPanel from '@/components/quest/QuestBattleStatusPanel'
import QuestLastTurnResultsPanel from '@/components/quest/QuestLastTurnResultsPanel'
import QuestRunResultPanel from '@/components/quest/QuestRunResultPanel'
import type { MoveAttackRange } from '@/schema/player'
import locale from '../../../locale/quest/QuestRoom.json'
import type { BattleColumn, BattleRow, QuestActionKind, QuestRunDetailResponse } from '@/schema/quest'

type AvailableMove = {
  slot: number
  moveId: number | null
  moveName: string
  targetType: 'Enemy' | 'Ally' | 'Self' | null
  targetLifeState: 'Alive' | 'Dead' | 'Any' | null | undefined
  attackRange: MoveAttackRange | null
}

type QuestRunSectionProps = {
  currentRun: QuestRunDetailResponse | null | undefined
  battlefieldImagePath: string | null
  selfParticipantId: string | null
  availableMoves: AvailableMove[]
  selectedActionKind: QuestActionKind
  selectedMoveId: number | ''
  selectedTargetRow: BattleRow | ''
  selectedTargetColumn: BattleColumn | ''
  currentPendingCommand: {
    actionKind: QuestActionKind
    submittedAt: string
  } | null
  canCapture: boolean
  petSummon: { name: string; remaining: number } | null
  chatMessage: string
  canSubmitCurrentTurn: boolean
  isCommandSubmitting: boolean
  isChatSubmitting: boolean
  chatDisabledReason?: string | null
  onActionKindChange: (actionKind: QuestActionKind) => void
  onMoveChange: (moveId: number | '') => void
  onTargetRowChange: (row: BattleRow | '') => void
  onTargetColumnChange: (column: BattleColumn | '') => void
  onChatMessageChange: (value: string) => void
  onSubmitCommand: () => void | Promise<void>
  onSubmitChatMessage: () => void | Promise<void>
  onLeaveFinishedRun: () => void
}

export default function QuestRunSection({
  currentRun,
  battlefieldImagePath,
  selfParticipantId,
  availableMoves,
  selectedActionKind,
  selectedMoveId,
  selectedTargetRow,
  selectedTargetColumn,
  currentPendingCommand,
  canCapture,
  petSummon,
  chatMessage,
  canSubmitCurrentTurn,
  isCommandSubmitting,
  isChatSubmitting,
  chatDisabledReason = null,
  onActionKindChange,
  onMoveChange,
  onTargetRowChange,
  onTargetColumnChange,
  onChatMessageChange,
  onSubmitCommand,
  onSubmitChatMessage,
  onLeaveFinishedRun,
}: QuestRunSectionProps) {
  return (
    <>
      {currentRun?.status === 'Succeeded' || currentRun?.status === 'Failed' ? (
        <QuestRunResultPanel run={currentRun} onLeaveFinishedRun={onLeaveFinishedRun} locale={locale} />
      ) : null}

      {currentRun?.status === 'InProgress' ? (
        <QuestBattleStatusPanel
          run={currentRun}
          battlefieldImagePath={battlefieldImagePath}
          selfParticipantId={selfParticipantId}
          availableMoves={availableMoves}
          selectedActionKind={selectedActionKind}
          selectedMoveId={selectedMoveId}
          selectedTargetRow={selectedTargetRow}
          selectedTargetColumn={selectedTargetColumn}
          currentPendingCommand={currentPendingCommand}
          canCapture={canCapture}
          petSummon={petSummon}
          canSubmitCurrentTurn={canSubmitCurrentTurn}
          isCommandSubmitting={isCommandSubmitting}
          onActionKindChange={onActionKindChange}
          onMoveChange={onMoveChange}
          onTargetRowChange={onTargetRowChange}
          onTargetColumnChange={onTargetColumnChange}
          onSubmitCommand={onSubmitCommand}
          locale={locale}
        />
      ) : null}

      {currentRun?.status === 'InProgress' ? (
        <QuestLastTurnResultsPanel
          run={currentRun}
          chatMessage={chatMessage}
          isChatSubmitting={isChatSubmitting}
          canPostChat={selfParticipantId != null}
          chatDisabledReason={chatDisabledReason}
          onChatMessageChange={onChatMessageChange}
          onSubmitChatMessage={onSubmitChatMessage}
          locale={locale}
        />
      ) : null}
    </>
  )
}
