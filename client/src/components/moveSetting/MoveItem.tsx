import DragIndicatorIcon from '@mui/icons-material/DragIndicator'
import { Box, Chip, Paper, Stack, Typography } from '@mui/material'
import type { SxProps, Theme } from '@mui/material/styles'
import type { PointerEventHandler } from 'react'
import type { PlayerMoveSlot } from '@/schema/player'
import { type z } from 'zod'
import { moveAilmentTypeSchema, moveBuffStatSchema, moveEffectSummarySchema } from '@/schema/player'
import { resolvePublicAssetPath } from '@/lib/assets'
import { getMoveAttackRangeLabel } from '@/lib/moveAttackRange'
import locale from '../../../locale/player-setting/PlayerSetting.json'

type MoveItemProps = {
  slot: PlayerMoveSlot
  onHandlePointerDown?: PointerEventHandler<HTMLDivElement>
  isDragging?: boolean
}

function resolveElementTypeLabel(elementType: PlayerMoveSlot['elementType']): string | null {
  switch (elementType) {
    case 'Strike':
      return '打属性'
    case 'Slash':
      return '斬属性'
    case 'Pierce':
      return '突属性'
    case 'Fire':
      return '火属性'
    case 'Water':
      return '水属性'
    case 'Earth':
      return '土属性'
    case 'Wind':
      return '風属性'
    case 'Holy':
      return '聖属性'
    case 'None':
      return '無属性'
    default:
      return null
  }
}

function resolveCategoryColor(category: PlayerMoveSlot['category']): 'error' | 'success' | 'warning' | 'default' {
  switch (category) {
    case 'Attack':
      return 'error'
    case 'Support':
      return 'success'
    case 'Hybrid':
      return 'warning'
    default:
      return 'default'
  }
}

function resolveCategoryLabel(category: PlayerMoveSlot['category']): string | null {
  switch (category) {
    case 'Attack':
      return '攻撃'
    case 'Support':
      return '補助'
    case 'Hybrid':
      return '複合'
    default:
      return null
  }
}

function resolveElementTypeChipSx(elementType: PlayerMoveSlot['elementType']): SxProps<Theme> {
  switch (elementType) {
    case 'Strike':
      return { bgcolor: '#6d4c41', color: '#fff', borderRadius: 1 }
    case 'Slash':
      return { bgcolor: '#8e24aa', color: '#fff', borderRadius: 1 }
    case 'Pierce':
      return { bgcolor: '#546e7a', color: '#fff', borderRadius: 1 }
    case 'Fire':
      return { bgcolor: '#e53935', color: '#fff', borderRadius: 1 }
    case 'Water':
      return { bgcolor: '#1e88e5', color: '#fff', borderRadius: 1 }
    case 'Earth':
      return { bgcolor: '#7cb342', color: '#fff', borderRadius: 1 }
    case 'Wind':
      return { bgcolor: '#26a69a', color: '#fff', borderRadius: 1 }
    case 'Holy':
      return { bgcolor: '#fbc02d', color: '#212121', borderRadius: 1 }
    case 'None':
      return { bgcolor: '#9e9e9e', color: '#fff', borderRadius: 1 }
    default:
      return {}
  }
}

type MoveEffectSummary = z.infer<typeof moveEffectSummarySchema>
type BuffStat = z.infer<typeof moveBuffStatSchema>
type AilmentType = z.infer<typeof moveAilmentTypeSchema>

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

function buildEffectChips(effectSummaries: MoveEffectSummary[]): { label: string; color: string }[] {
  const chips: { label: string; color: string }[] = []

  for (const effect of effectSummaries) {
    if (
      (effect.effectType === 'Damage' || effect.effectType === 'Heal' || effect.effectType === 'RestoreMp') &&
      effect.powerRate != null
    ) {
      chips.push({
        label: `x${effect.powerRate}`,
        color: '#2e7d32',
      })
    }
    if (effect.effectType === 'Buff' && effect.buffStat && effect.buffTurns) {
      chips.push({
        label: `${resolveBuffStatLabel(effect.buffStat)} ${effect.buffTurns}T`,
        color: '#1565c0',
      })
    }
    if (effect.effectType === 'Ailment' && effect.ailmentType && effect.ailmentTurns) {
      chips.push({
        label: `${resolveAilmentTypeLabel(effect.ailmentType)} ${effect.ailmentTurns}T`,
        color: '#6a1b9a',
      })
    }
  }

  return chips
}

function resolveTargetTypeLabel(targetType: PlayerMoveSlot['targetType']): string | null {
  switch (targetType) {
    case 'Enemy':
      return '敵対象'
    case 'Ally':
      return '味方対象'
    case 'Self':
      return '自分対象'
    default:
      return null
  }
}

export default function MoveItem({ slot, onHandlePointerDown, isDragging = false }: MoveItemProps) {
  const isEmpty = slot.moveId === null
  const moveName = slot.moveName ?? (slot.moveId === null ? locale.emptySlot : locale.unknownMove)
  const moveDescription = slot.description ?? locale.unknownMoveDescription
  const categoryLabel = resolveCategoryLabel(slot.category)
  const elementTypeLabel = resolveElementTypeLabel(slot.elementType)
  const targetTypeLabel = resolveTargetTypeLabel(slot.targetType)
  const attackRangeLabel = getMoveAttackRangeLabel(slot.attackRange)
  const effectChips = slot.effectSummaries ? buildEffectChips(slot.effectSummaries) : []
  const effectImageSrc = slot.effectImagePath ? resolvePublicAssetPath(slot.effectImagePath) : null

  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 2,
        p: 1.5,
        backgroundColor: isEmpty ? '#fffdf8' : '#fff9ef',
        borderColor: '#dacb9d',
      }}
    >
      <Stack spacing={1}>
        {isEmpty || !effectImageSrc ? null : (
          <Box
            sx={{
              borderRadius: 2,
              border: '1px solid #ead9b2',
              background: 'linear-gradient(135deg, rgba(255,252,245,0.95), rgba(246,235,205,0.82))',
              height: 84,
              display: 'grid',
              placeItems: 'center',
              overflow: 'hidden',
            }}
          >
            <Box
              component="img"
              src={effectImageSrc}
              alt={moveName}
              sx={{
                width: '100%',
                height: '100%',
                objectFit: 'contain',
                p: 1,
              }}
            />
          </Box>
        )}

        <Stack
          direction="row"
          justifyContent="space-between"
          alignItems="center"
          spacing={1}
          sx={{
            p: 1,
            borderRadius: 1.5,
            backgroundColor: '#4a6c51',
            border: '1px solid #c0b07a',
          }}
        >
          <Stack direction="row" spacing={1} alignItems="center" sx={{ minWidth: 0, flex: 1 }}>
            <Box
              onPointerDown={onHandlePointerDown}
              aria-label={locale.dragHandle}
              sx={{
                width: 34,
                height: 34,
                borderRadius: 1,
                display: 'grid',
                placeItems: 'center',
                bgcolor: isDragging ? 'rgba(238, 219, 155, 0.28)' : 'rgba(255, 255, 255, 0.12)',
                border: '1px solid rgba(255, 255, 255, 0.18)',
                color: '#fff8e8',
                cursor: 'grab',
                touchAction: 'none',
                flexShrink: 0,
              }}
            >
              <DragIndicatorIcon fontSize="small" />
            </Box>
            <Typography variant="h6" fontWeight={900} sx={{ lineHeight: 1.2, color: '#fff8e8' }}>
              {moveName}
            </Typography>
          </Stack>
          {typeof slot.mpCost === 'number' ? (
            <Chip
              size="small"
              label={`消費MP ${slot.mpCost}`}
              sx={{
                fontWeight: 800,
                bgcolor: '#eedb9b',
                color: '#5b4717',
                borderRadius: 1,
                border: '1px solid #cfb067',
              }}
            />
          ) : null}
        </Stack>

        {isEmpty ? null : (
          <Typography variant="body2" color="text.secondary">
            {moveDescription}
          </Typography>
        )}

        {isEmpty ? null : (
          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            {slot.category && categoryLabel ? (
              <Chip
                size="small"
                label={categoryLabel}
                color={resolveCategoryColor(slot.category)}
                sx={{ borderRadius: 1 }}
              />
            ) : null}
            {elementTypeLabel ? (
              <Chip size="small" label={elementTypeLabel} sx={resolveElementTypeChipSx(slot.elementType)} />
            ) : null}
            {targetTypeLabel ? (
              <Chip size="small" label={targetTypeLabel} variant="outlined" sx={{ borderRadius: 1 }} />
            ) : null}
            {attackRangeLabel ? (
              <Chip
                size="small"
                label={attackRangeLabel}
                variant="outlined"
                sx={{ borderRadius: 1, bgcolor: 'rgba(255,255,255,0.7)' }}
              />
            ) : null}
            {effectChips.map((chip) => (
              <Chip
                key={chip.label}
                size="small"
                label={chip.label}
                sx={{ borderRadius: 1, bgcolor: chip.color, color: '#fff', fontWeight: 700 }}
              />
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
