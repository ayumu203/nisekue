import { Chip, Paper, Stack, Typography } from '@mui/material'
import type { PlayerMoveSlot } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'

type MoveItemProps = {
  slot: PlayerMoveSlot
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

export default function MoveItem({ slot }: MoveItemProps) {
  const isEmpty = slot.moveId === null
  const moveName = slot.moveName ?? (slot.moveId === null ? locale.emptySlot : locale.unknownMove)

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
          <Typography variant="subtitle2" fontWeight={700}>
            {moveName}
          </Typography>
          <Typography variant="caption" color="text.secondary">
            {typeof slot.mpCost === 'number' ? `MP ${slot.mpCost}` : ''}
          </Typography>
        </Stack>

        {isEmpty ? null : (
          <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
            {slot.category ? (
              <Chip size="small" label={slot.category} color={resolveCategoryColor(slot.category)} />
            ) : null}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
