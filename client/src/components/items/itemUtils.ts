import locale from '../../../locale/items/Items.json'
import type { ItemEquipmentView, ItemStackView } from '@/schema/item'

export function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ja-JP', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export function getEffectTypeLabel(effectType: ItemStackView['effectType']): string {
  switch (effectType) {
    case 'StatBoost':
      return locale.effectTypeStatBoost
    case 'ChangeJob':
      return locale.effectTypeChangeJob
    case 'ExpMultiplier':
      return locale.effectTypeExpMultiplier
    case 'UnlockMap':
      return locale.effectTypeUnlockMap
    default:
      return effectType satisfies never
  }
}

export function formatEffectAmount(item: ItemStackView): string {
  if (item.effectType === 'ExpMultiplier') {
    return item.expMultiplier != null ? `${item.expMultiplier}倍` : '-'
  }
  return formatStatusBonus(item)
}

export function formatStatusBonus(item: {
  statusBonus: ItemStackView['statusBonus'] | ItemEquipmentView['statusBonus']
  statusBonusPercent?: ItemStackView['statusBonusPercent'] | null
}): string {
  if (!item.statusBonus && !item.statusBonusPercent) {
    return '-'
  }

  const labels: Array<[string, number]> = [
    ['HP', item.statusBonus?.maxHp ?? 0],
    ['MP', item.statusBonus?.maxMp ?? 0],
    ['STR', item.statusBonus?.strength ?? 0],
    ['DEF', item.statusBonus?.defense ?? 0],
    ['INT', item.statusBonus?.intelligence ?? 0],
    ['LUK', item.statusBonus?.luck ?? 0],
    ['SPD', item.statusBonus?.speed ?? 0],
  ]
  const percentLabels: Array<[string, number]> = [
    ['HP', item.statusBonusPercent?.maxHp ?? 0],
    ['MP', item.statusBonusPercent?.maxMp ?? 0],
    ['STR', item.statusBonusPercent?.strength ?? 0],
    ['DEF', item.statusBonusPercent?.defense ?? 0],
    ['INT', item.statusBonusPercent?.intelligence ?? 0],
    ['LUK', item.statusBonusPercent?.luck ?? 0],
    ['SPD', item.statusBonusPercent?.speed ?? 0],
  ]

  const active = labels.filter(([, value]) => value !== 0).map(([label, value]) => `${label}+${value}`)
  const activePercent = percentLabels.filter(([, value]) => value !== 0).map(([label, value]) => `${label}+${value}%`)
  const effects = [...active, ...activePercent]
  return effects.length > 0 ? effects.join(' / ') : '-'
}

export function parsePositiveInteger(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}

export function normalizeText(value: string): string {
  return value.trim().toLocaleLowerCase('ja-JP')
}

type StatusBonus = ItemEquipmentView['statusBonus']

const statusBonusKeys = ['maxHp', 'maxMp', 'strength', 'defense', 'intelligence', 'luck', 'speed'] as const
const statusBonusLabels: Record<(typeof statusBonusKeys)[number], string> = {
  maxHp: 'HP',
  maxMp: 'MP',
  strength: 'STR',
  defense: 'DEF',
  intelligence: 'INT',
  luck: 'LUK',
  speed: 'SPD',
}

// サーバー（EquipmentStatusResolver）と同じ計算：元の値 ×（100 ＋ 強化値）÷ 100（0 に向けて切り捨て）
export function applyPlusValue(bonus: StatusBonus, plusValue: number): StatusBonus {
  const result = { ...bonus }
  for (const key of statusBonusKeys) {
    result[key] = bonus[key] === 0 ? 0 : Math.trunc((bonus[key] * (100 + plusValue)) / 100)
  }
  return result
}

// 強化値を反映した装備の効果
export function getEquipmentBonus(item: Pick<ItemEquipmentView, 'statusBonus' | 'plusValue'>): StatusBonus {
  return applyPlusValue(item.statusBonus, item.plusValue)
}

export type StatusChange = { label: string; from: number; to: number }

// 装備を持ち替えたときの能力の変化（変わるものだけ）
export function compareStatusBonus(current: StatusBonus | null, next: StatusBonus): StatusChange[] {
  return statusBonusKeys
    .map((key) => ({ label: statusBonusLabels[key], from: current?.[key] ?? 0, to: next[key] }))
    .filter((change) => change.from !== change.to)
}

// 1 回の合成費用（サーバーと同じ：合成コスト ×（今の強化値 ＋ 1））
export function getSynthesisCost(target: Pick<ItemEquipmentView, 'synthesisGoldCost' | 'plusValue'>): number {
  return target.synthesisGoldCost * (target.plusValue + 1)
}

export function formatGold(value: number): string {
  return `${value.toLocaleString('ja-JP')} G`
}

export function formatPlusValue(plusValue: number): string {
  return plusValue > 0 ? `+${plusValue}` : ''
}
