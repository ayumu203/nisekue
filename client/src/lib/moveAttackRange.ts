import type { MoveAttackRange } from '@/schema/player'

export function getMoveAttackRangeLabel(attackRange: MoveAttackRange | null): string | null {
  switch (attackRange) {
    case 'Single':
      return '単体'
    case 'Column':
    case 'AcrossRows':
      return '同側縦列'
    case 'Row':
    case 'AcrossColumns':
      return '同列2体'
    case 'Square':
      return '範囲'
    case 'All':
      return '全体'
    default:
      return null
  }
}
