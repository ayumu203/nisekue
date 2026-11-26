import { type z } from 'zod'
import type { PlayerMoveSlot } from '@/schema/player'
import { moveAilmentTypeSchema, moveBuffStatSchema, moveEffectSummarySchema } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'

type MoveEffectSummary = z.infer<typeof moveEffectSummarySchema>
type BuffStat = z.infer<typeof moveBuffStatSchema>
type AilmentType = z.infer<typeof moveAilmentTypeSchema>
export type MoveElementKey = NonNullable<PlayerMoveSlot['elementType']> | 'NoElement'

// 属性ごとの紋章の色と名前。属性のない補助スキルは緑（回復役らしさ）
export const elementStyles: Record<MoveElementKey, { color: string; label: string | null }> = {
  Strike: { color: '#6d4c41', label: '打属性' },
  Slash: { color: '#8e24aa', label: '斬属性' },
  Pierce: { color: '#546e7a', label: '突属性' },
  Fire: { color: '#d84315', label: '火属性' },
  Water: { color: '#1e6fc0', label: '水属性' },
  Earth: { color: '#9a6532', label: '土属性' },
  Wind: { color: '#167f74', label: '風属性' },
  Holy: { color: '#d4a20f', label: '聖属性' },
  None: { color: '#7a7a7a', label: '無属性' },
  NoElement: { color: '#2f9a5a', label: null },
}

export function resolveElementKey(slot: PlayerMoveSlot): MoveElementKey {
  return slot.elementType ?? 'NoElement'
}

export const categoryStyles: Record<NonNullable<PlayerMoveSlot['category']>, { color: string; label: string }> = {
  Attack: { color: '#c0392b', label: '攻撃' },
  Support: { color: '#2e7d32', label: '補助' },
  Hybrid: { color: '#b26a00', label: '複合' },
}

export function resolveTargetLabel(targetType: PlayerMoveSlot['targetType']): string | null {
  switch (targetType) {
    case 'Enemy':
      return locale.targetEnemy
    case 'Ally':
      return locale.targetAlly
    case 'Self':
      return locale.targetSelf
    default:
      return null
  }
}

function resolveBuffStatLabel(stat: BuffStat): string {
  switch (stat) {
    case 'MaxHp':
      return '最大HP'
    case 'MaxMp':
      return '最大MP'
    case 'Strength':
      return '攻撃力'
    case 'Defense':
      return '防御力'
    case 'Intelligence':
      return '知力'
    case 'Luck':
      return '運'
    case 'Speed':
      return '素早さ'
    case 'Accuracy':
      return '命中'
    case 'Evasion':
      return '回避'
    case 'CriticalChance':
      return '会心率'
    case 'DamageReduction':
      return 'ダメージ軽減'
    case 'StrengthIntelligence':
      return '攻撃力・知力'
  }
}

function resolveAilmentTypeLabel(ailmentType: AilmentType): string {
  switch (ailmentType) {
    case 'Paralysis':
      return '麻痺'
    case 'Poison':
      return '毒'
    case 'Sleep':
      return '睡眠'
    case 'Burn':
      return 'やけど'
    case 'Taunt':
      return '挑発'
    case 'PoisonTrap':
      return '毒の罠'
    case 'DamageTrap':
      return 'ダメージの罠'
    case 'InstantDeath':
      return '即死'
    case 'Regeneration':
      return '再生'
    case 'CoverAll':
      return 'かばう'
  }
}

export type MoveEffectEntry = { label: string; value: string }

// 効果を「見出し」と「値」に分ける（詳細のタイル用）。威力・回復は倍率、強化・状態異常はターン数
export function buildEffectEntries(effectSummaries: MoveEffectSummary[] | null | undefined): MoveEffectEntry[] {
  const entries: MoveEffectEntry[] = []

  for (const effect of effectSummaries ?? []) {
    if (effect.effectType === 'Damage' && effect.powerRate != null) {
      entries.push({ label: locale.powerLabel, value: `×${effect.powerRate}` })
    }
    if (effect.effectType === 'Heal' && effect.powerRate != null) {
      entries.push({ label: locale.healLabel, value: `×${effect.powerRate}` })
    }
    if (effect.effectType === 'RestoreMp' && effect.powerRate != null) {
      entries.push({ label: locale.restoreMpLabel, value: `×${effect.powerRate}` })
    }
    if (effect.effectType === 'Buff' && effect.buffStat && effect.buffTurns) {
      entries.push({ label: `${resolveBuffStatLabel(effect.buffStat)}アップ`, value: `${effect.buffTurns} ターン` })
    }
    if (effect.effectType === 'Ailment' && effect.ailmentType && effect.ailmentTurns) {
      entries.push({ label: resolveAilmentTypeLabel(effect.ailmentType), value: `${effect.ailmentTurns} ターン` })
    }
  }

  return entries
}

// 一覧の列に出す短い効果（例：「威力 ×0.85」「防御力↑ 3T」）
export function buildShortEffect(effectSummaries: MoveEffectSummary[] | null | undefined): string {
  const parts: string[] = []

  for (const effect of effectSummaries ?? []) {
    if (effect.effectType === 'Damage' && effect.powerRate != null) {
      parts.push(`${locale.powerLabel} ×${effect.powerRate}`)
    }
    if (effect.effectType === 'Heal' && effect.powerRate != null) {
      parts.push(`${locale.healLabel} ×${effect.powerRate}`)
    }
    if (effect.effectType === 'RestoreMp' && effect.powerRate != null) {
      parts.push(`${locale.restoreMpLabel} ×${effect.powerRate}`)
    }
    if (effect.effectType === 'Buff' && effect.buffStat && effect.buffTurns) {
      parts.push(`${resolveBuffStatLabel(effect.buffStat)}↑ ${effect.buffTurns}T`)
    }
    if (effect.effectType === 'Ailment' && effect.ailmentType && effect.ailmentTurns) {
      parts.push(`${resolveAilmentTypeLabel(effect.ailmentType)} ${effect.ailmentTurns}T`)
    }
  }

  return parts.join(' / ')
}

// 陣形は 3 列（前・中・後）× 左右 2。範囲に当たる位置を [列, 左右] で返す（列 0 が前列、左右 0 が左）
export function resolveRangeCells(slot: PlayerMoveSlot): Array<[number, number]> {
  if (slot.targetType === 'Self') {
    return [[1, 0]]
  }

  switch (slot.attackRange) {
    case 'Single':
      return [[0, 0]]
    case 'Column':
      return [
        [0, 0],
        [1, 0],
        [2, 0],
      ]
    case 'Row':
      return [
        [0, 0],
        [0, 1],
      ]
    case 'Square':
      return [
        [0, 0],
        [0, 1],
        [1, 0],
        [1, 1],
      ]
    case 'All':
      return [0, 1, 2].flatMap((row) => [0, 1].map((column) => [row, column] as [number, number]))
    default:
      return []
  }
}
