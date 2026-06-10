import type { BattleColumn, BattleRow, PetBattleMember, PetBattleMemberMove } from '@/schema/petBattle'

export type PetBattleCommandDraft = {
  actionKind: 'NormalAttack' | 'UseMove' | 'Guard' | 'Wait'
  moveId: number | ''
  targetRow: BattleRow | ''
  targetColumn: BattleColumn | ''
}

const battleRowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const battleColumnOrder: BattleColumn[] = ['Left', 'Right']

export function sortMembersByPosition(members: PetBattleMember[]): PetBattleMember[] {
  return [...members].sort((left, right) => {
    if (left.startRow !== right.startRow) {
      return battleRowOrder.indexOf(left.startRow) - battleRowOrder.indexOf(right.startRow)
    }

    return battleColumnOrder.indexOf(left.startColumn) - battleColumnOrder.indexOf(right.startColumn)
  })
}

export function getReachableOpponents(member: PetBattleMember, opponents: PetBattleMember[]): PetBattleMember[] {
  const aliveOpponents = sortMembersByPosition(opponents.filter((opponent) => !opponent.isDead))
  const occupiedRows = Array.from(new Set(aliveOpponents.map((opponent) => opponent.startRow)))
  const reachableRowCount = member.startRow === 'Front' ? 1 : member.startRow === 'Middle' ? 2 : occupiedRows.length
  const reachableRows = new Set<BattleRow>(occupiedRows.slice(0, reachableRowCount))

  return aliveOpponents.filter((opponent) => reachableRows.has(opponent.startRow))
}

export function getAllyTargets(
  allies: PetBattleMember[],
  targetLifeState: 'Alive' | 'Dead' | 'Any',
): PetBattleMember[] {
  return sortMembersByPosition(
    allies.filter((ally) => {
      if (targetLifeState === 'Alive') {
        return !ally.isDead
      }

      if (targetLifeState === 'Dead') {
        return ally.isDead
      }

      return true
    }),
  )
}

export function getTargetCandidates(
  member: PetBattleMember,
  draft: PetBattleCommandDraft,
  ownerMembers: PetBattleMember[],
  opponentMembers: PetBattleMember[],
): PetBattleMember[] {
  if (draft.actionKind === 'NormalAttack') {
    return getReachableOpponents(member, opponentMembers)
  }

  if (draft.actionKind !== 'UseMove') {
    return []
  }

  const move = draft.moveId === '' ? null : (member.moves.find((m) => m.moveId === draft.moveId) ?? null)
  if (move == null) {
    return []
  }

  if (move.targetType === 'Enemy') {
    return getReachableOpponents(member, opponentMembers)
  }

  if (move.targetType === 'Ally') {
    return getAllyTargets(ownerMembers, move.targetLifeState)
  }

  return [member]
}

export function needsTargetSelection(draft: PetBattleCommandDraft, move: PetBattleMemberMove | null): boolean {
  if (draft.actionKind === 'NormalAttack') {
    return true
  }

  if (draft.actionKind !== 'UseMove') {
    return false
  }

  return move != null && (move.targetType === 'Enemy' || move.targetType === 'Ally')
}
