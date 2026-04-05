import type { MoveAttackRange } from '@/schema/player'

export function getMoveAttackRangeLabel(attackRange: MoveAttackRange | null): string | null {
  switch (attackRange) {
    case 'Single':
      return '単体'
    case 'Column':
      return '縦1列'
    case 'Row':
      return '横1列'
    case 'Square':
      return '前方2列'
    case 'All':
      return '全体'
    default:
      return null
  }
}
