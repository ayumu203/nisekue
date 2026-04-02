import { Box, Paper, Stack, Typography } from '@mui/material'
import { deepGreen, deepGreenBorder, itemInnerPanelSx } from './ItemsConstants'
import locale from '../../../locale/items/Items.json'

export function ItemSummary({ usedSlots, capacity, gold }: { usedSlots?: number; capacity?: number; gold?: number }) {
  return (
    <Paper variant="outlined" sx={{ ...itemInnerPanelSx, p: 2.5, borderRadius: 3 }}>
      <Stack spacing={1.75}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          spacing={1.5}
          justifyContent="space-between"
          alignItems={{ sm: 'center' }}
        >
          <Stack spacing={0.25}>
            <Typography variant="overline" sx={{ letterSpacing: '0.14em', color: '#5a755f' }}>
              PLAYER STOCK
            </Typography>
            <Typography variant="h6" fontWeight={900}>
              {locale.summaryTitle}
            </Typography>
          </Stack>
        </Stack>
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
            gap: 1.25,
          }}
        >
          <Paper
            variant="outlined"
            sx={{ p: 1.5, borderRadius: 2.5, borderColor: deepGreenBorder, bgcolor: '#ffffff' }}
          >
            <Typography variant="caption" color="text.secondary">
              {locale.capacity}
            </Typography>
            <Typography variant="h5" fontWeight={900} color={deepGreen}>
              {usedSlots ?? '-'}{' '}
              <Typography component="span" variant="body2">
                / {capacity ?? '-'}
              </Typography>
            </Typography>
          </Paper>
          <Paper variant="outlined" sx={{ p: 1.5, borderRadius: 2.5, borderColor: '#e5d8ac', bgcolor: '#fffdf4' }}>
            <Typography variant="caption" color="text.secondary">
              {locale.gold}
            </Typography>
            <Typography variant="h5" fontWeight={900} color="#7a6123">
              {gold ?? '-'}
            </Typography>
          </Paper>
        </Box>
      </Stack>
    </Paper>
  )
}
