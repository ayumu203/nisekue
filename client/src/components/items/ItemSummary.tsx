import { Box, Paper, Typography } from '@mui/material'
import { deepGreen, itemInnerPanelSx } from './ItemsConstants'
import locale from '../../../locale/items/Items.json'

export function ItemSummary({ usedSlots, capacity, gold }: { usedSlots?: number; capacity?: number; gold?: number }) {
  return (
    <Paper variant="outlined" sx={{ ...itemInnerPanelSx, p: 2.5, borderRadius: 3 }}>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
          gap: 1.25,
        }}
      >
        <Box>
          <Typography variant="caption" color="text.secondary">
            {locale.capacity}
          </Typography>
          <Typography variant="h5" fontWeight={900} color={deepGreen}>
            {usedSlots ?? '-'}{' '}
            <Typography component="span" variant="body2">
              / {capacity ?? '-'}
            </Typography>
          </Typography>
        </Box>
        <Box>
          <Typography variant="caption" color="text.secondary">
            {locale.gold}
          </Typography>
          <Typography variant="h5" fontWeight={900} color="#7a6123">
            {gold ?? '-'}
          </Typography>
        </Box>
      </Box>
    </Paper>
  )
}
