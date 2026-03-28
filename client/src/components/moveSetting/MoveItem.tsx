import { Box, Chip, Paper, Stack, Typography } from '@mui/material'
import type { SxProps, Theme } from '@mui/material/styles'
import type { PlayerMoveSlot } from '@/schema/player'
import { resolvePublicAssetPath } from '@/lib/assets'
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

function resolveAttackRangeLabel(attackRange: PlayerMoveSlot['attackRange']): string | null {
  switch (attackRange) {
    case 'Single':
      return '単体'
    case 'Column':
      return '縦列'
    case 'Row':
      return '横列'
    case 'Square':
      return '範囲'
    case 'All':
      return '全体'
    default:
      return null
  }
}

export default function MoveItem({ slot }: MoveItemProps) {
  const isEmpty = slot.moveId === null
  const moveName = slot.moveName ?? (slot.moveId === null ? locale.emptySlot : locale.unknownMove)
  const moveDescription = slot.description ?? locale.unknownMoveDescription
  const categoryLabel = resolveCategoryLabel(slot.category)
  const elementTypeLabel = resolveElementTypeLabel(slot.elementType)
  const targetTypeLabel = resolveTargetTypeLabel(slot.targetType)
  const attackRangeLabel = resolveAttackRangeLabel(slot.attackRange)
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
          <Typography variant="h6" fontWeight={900} sx={{ lineHeight: 1.2, color: '#fff8e8' }}>
            {moveName}
          </Typography>
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
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
