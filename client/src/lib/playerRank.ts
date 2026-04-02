export function resolveRankColor(rank: string | undefined): string | undefined {
  switch (rank) {
    case 'G':
      return '#3b82f6'
    case 'F':
      return '#4f79e2'
    case 'E':
      return '#6366f1'
    case 'D':
      return '#8b5cf6'
    case 'C':
      return '#a855f7'
    case 'B':
      return '#d946ef'
    case 'A':
      return '#ef4444'
    case 'S':
      return '#f97316'
    case 'SS':
      return '#f59e0b'
    case 'SSS':
      return '#d4af37'
    default:
      return undefined
  }
}
