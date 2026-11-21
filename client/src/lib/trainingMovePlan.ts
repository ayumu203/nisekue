import type { GetPlayerResponse } from '@/schema/player'

/**
 * チュートリアルで次に技を選んでほしいターンの番号を返す。
 * まだ技を選べる MP が残っていて、通常攻撃のままのターンがあれば、その最初のターンを返す。
 * MP を使い切った（どの技も選べない）か、すべてのターンに技が入っていれば null を返す。
 */
export function getNextMoveTurnIndex(player: GetPlayerResponse, moveIds: Array<number | null>): number | null {
  const moves = player.moveSlots.filter((slot) => slot.moveId !== null)
  if (moves.length === 0) {
    return null
  }

  const mpCostById = new Map(moves.map((slot) => [slot.moveId!, slot.mpCost ?? 0]))
  let remainingMp = player.status.maxMp
  for (const moveId of moveIds) {
    if (moveId === null) {
      continue
    }
    const mpCost = mpCostById.get(moveId) ?? 0
    if (remainingMp >= mpCost) {
      remainingMp -= mpCost
    }
  }

  const cheapestMpCost = Math.min(...moves.map((slot) => slot.mpCost ?? 0))
  if (remainingMp < cheapestMpCost) {
    return null
  }

  const index = moveIds.findIndex((moveId) => moveId === null)
  return index === -1 ? null : index
}
