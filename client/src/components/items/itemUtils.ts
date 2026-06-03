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
  }
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
