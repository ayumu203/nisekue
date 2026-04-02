import { TextField, Stack } from '@mui/material'
import { inputSx } from './ItemsConstants'
import locale from '../../../locale/items/Items.json'

export function ListingControls({
  quantityValue,
  unitPriceValue,
  onQuantityChange,
  onUnitPriceChange,
  showUnitPrice = true,
}: {
  quantityValue: string
  unitPriceValue?: string
  onQuantityChange: (value: string) => void
  onUnitPriceChange?: (value: string) => void
  showUnitPrice?: boolean
}) {
  return (
    <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
      <TextField
        label={locale.quantity}
        size="small"
        value={quantityValue}
        onChange={(event) => onQuantityChange(event.target.value)}
        inputProps={{ inputMode: 'numeric', pattern: '[0-9]*', min: 1 }}
        sx={{ minWidth: { xs: '100%', sm: 100 }, ...inputSx }}
      />
      {showUnitPrice ? (
        <TextField
          label={locale.unitPrice}
          size="small"
          value={unitPriceValue}
          onChange={(event) => onUnitPriceChange?.(event.target.value)}
          inputProps={{ inputMode: 'numeric', pattern: '[0-9]*', min: 1 }}
          sx={{ minWidth: { xs: '100%', sm: 120 }, ...inputSx }}
        />
      ) : null}
    </Stack>
  )
}
