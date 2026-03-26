import QuestBattleStatusPanel from '@/components/quest/QuestBattleStatusPanel'
import QuestLastTurnResultsPanel from '@/components/quest/QuestLastTurnResultsPanel'
import locale from '../../../locale/quest/QuestRoom.json'
import type { BattleColumn, BattleRow, QuestActionKind, QuestRunDetailResponse } from '@/schema/quest'

type AvailableMove = {
  slot: number
  moveId: number | null
  moveName: string
  targetType: 'Enemy' | 'Ally' | 'Self' | null
  attackRange: 'Single' | 'Column' | 'Row' | 'Square' | 'All' | null
}

type QuestRunSectionProps = {
  startedRun: QuestRunDetailResponse | null
  currentRun: QuestRunDetailResponse | null | undefined
  selfParticipantId: string | null
  availableMoves: AvailableMove[]
  selectedActionKind: QuestActionKind
  selectedMoveId: number | ''
  selectedTargetRow: BattleRow | ''
  selectedTargetColumn: BattleColumn | ''
  currentPendingCommand:
    | {
        actionKind: QuestActionKind
        submittedAt: string
      }
    | null
  canSubmitCurrentTurn: boolean
  isRunLoading: boolean
  isCommandSubmitting: boolean
  isRunFinished: boolean
  onActionKindChange: (actionKind: QuestActionKind) => void
  onMoveChange: (moveId: number | '') => void
  onTargetRowChange: (row: BattleRow | '') => void
  onTargetColumnChange: (column: BattleColumn | '') => void
  onSubmitCommand: () => void | Promise<void>
  onLeaveFinishedRun: () => void
}

export default function QuestRunSection({
  currentRun,
  selfParticipantId,
  availableMoves,
  selectedActionKind,
  selectedMoveId,
  selectedTargetRow,
  selectedTargetColumn,
  currentPendingCommand,
  canSubmitCurrentTurn,
  isCommandSubmitting,
  isRunFinished,
  onActionKindChange,
  onMoveChange,
  onTargetRowChange,
  onTargetColumnChange,
  onSubmitCommand,
  onLeaveFinishedRun,
}: QuestRunSectionProps) {
  return (
    <>
      {currentRun ? (
        <QuestBattleStatusPanel
          run={currentRun}
          selfParticipantId={selfParticipantId}
          availableMoves={availableMoves}
          selectedActionKind={selectedActionKind}
          selectedMoveId={selectedMoveId}
          selectedTargetRow={selectedTargetRow}
          selectedTargetColumn={selectedTargetColumn}
          currentPendingCommand={currentPendingCommand}
          canSubmitCurrentTurn={canSubmitCurrentTurn}
          isCommandSubmitting={isCommandSubmitting}
          isRunFinished={isRunFinished}
          onActionKindChange={onActionKindChange}
          onMoveChange={onMoveChange}
          onTargetRowChange={onTargetRowChange}
          onTargetColumnChange={onTargetColumnChange}
          onSubmitCommand={onSubmitCommand}
          onLeaveFinishedRun={onLeaveFinishedRun}
          locale={locale}
        />
      ) : null}

      <QuestLastTurnResultsPanel run={currentRun} locale={locale} />
    </>
  )
}
