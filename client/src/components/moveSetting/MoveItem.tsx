import { Chip, Paper, Stack, Typography } from '@mui/material'
import type { SxProps, Theme } from '@mui/material/styles'
import type { PlayerMoveSlot } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'

type MoveItemProps = {
  slot: PlayerMoveSlot
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

export default function MoveItem({ slot }: MoveItemProps) {
  const isEmpty = slot.moveId === null
  const moveName = slot.moveName ?? (slot.moveId === null ? locale.emptySlot : locale.unknownMove)
  const moveDescription = slot.description ?? locale.unknownMoveDescription
  const categoryLabel = resolveCategoryLabel(slot.category)
  const elementTypeLabel = resolveElementTypeLabel(slot.elementType)

  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 2,
        p: 1.5,
      }}
    >
      <Stack spacing={1}>
        <Stack direction="row" justifyContent="space-between" alignItems="flex-end">
          <Typography variant="subtitle1" fontWeight={700}>
            {moveName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {typeof slot.mpCost === 'number' ? `MP ${slot.mpCost}` : ''}
          </Typography>
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
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
