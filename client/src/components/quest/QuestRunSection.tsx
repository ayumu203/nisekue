import QuestBattleStatusPanel from '@/components/quest/QuestBattleStatusPanel'
import QuestLastTurnResultsPanel from '@/components/quest/QuestLastTurnResultsPanel'
import QuestRunResultPanel from '@/components/quest/QuestRunResultPanel'
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
  currentPendingCommand: {
    actionKind: QuestActionKind
    submittedAt: string
  } | null
  canSubmitCurrentTurn: boolean
  isRunLoading: boolean
  isCommandSubmitting: boolean
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
  onActionKindChange,
  onMoveChange,
  onTargetRowChange,
  onTargetColumnChange,
  onSubmitCommand,
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
          selfParticipantId={selfParticipantId}
          availableMoves={availableMoves}
          selectedActionKind={selectedActionKind}
          selectedMoveId={selectedMoveId}
          selectedTargetRow={selectedTargetRow}
          selectedTargetColumn={selectedTargetColumn}
          currentPendingCommand={currentPendingCommand}
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

      {currentRun?.status === 'InProgress' ? <QuestLastTurnResultsPanel run={currentRun} locale={locale} /> : null}
    </>
  )
}
