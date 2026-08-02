import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { ItemArtwork } from './ItemIllustrations'
import { ListingControls } from './ListingControls'
import { formatStatusBonus } from './itemUtils'
import {
  accentBeige,
  accentBeigeBorder,
  deepGreen,
  destructiveActionSx,
  listCardSx,
  primaryActionSx,
  secondaryActionSx,
} from './ItemsConstants'
import locale from '../../../locale/items/Items.json'
import type { ItemEquipmentView } from '@/schema/item'

export function EquipmentCard({
  item,
  quantityValue,
  unitPriceValue,
  onQuantityChange,
  onUnitPriceChange,
  onSynthesize,
  onDelete,
  onList,
  onStartListing,
  onCancelListing,
  onEquip,
  actionDisabled,
  isListingMode = false,
  showActions = true,
}: {
  item: ItemEquipmentView
  quantityValue: string
  unitPriceValue: string
  onQuantityChange: (value: string) => void
  onUnitPriceChange: (value: string) => void
  onSynthesize: () => void
  onDelete: () => void
  onList: () => void
  onStartListing: () => void
  onCancelListing: () => void
  onEquip?: () => void
  actionDisabled: boolean
  isListingMode?: boolean
  showActions?: boolean
}) {
  const [showDetails, setShowDetails] = useState(false)

  return (
    <Paper
      variant="outlined"
      sx={{
        ...listCardSx,
        minHeight: '100%',
      }}
    >
      <Stack spacing={1}>
        <ItemArtwork item={item} />
        <Box
          sx={{
            px: 1,
            py: 0.75,
            borderRadius: 1.5,
            background: accentBeige,
            border: `1px solid ${accentBeigeBorder}`,
          }}
        >
          <Typography variant="body2" fontWeight={900} sx={{ lineHeight: 1.2, color: '#4c3b20', textAlign: 'center' }}>
            {item.name}
          </Typography>
        </Box>
        <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'center', minHeight: 34 }}>
          {item.flavorText || '-'}
        </Typography>
        <Button
          variant="text"
          onClick={() => setShowDetails((current) => !current)}
          sx={{ alignSelf: 'center', color: deepGreen, fontWeight: 700 }}
        >
          {showDetails ? locale.hideDetails : locale.showDetails}
        </Button>
        {showDetails ? (
          <Stack
            spacing={0.75}
            sx={{
              px: 1,
              py: 1,
              borderRadius: 1.5,
              backgroundColor: '#f8fbf8',
              border: '1px solid #d5e0d6',
            }}
          >
            <Typography variant="body2">
              {locale.effectAmount} {formatStatusBonus(item)}
            </Typography>
            {item.plusValue > 0 ? (
              <Typography variant="body2" fontWeight={700} color={deepGreen}>
                +{item.plusValue}
              </Typography>
            ) : null}
            <Typography variant="body2">
              {locale.synthesisCost} {item.synthesisGoldCost} Gold
            </Typography>
          </Stack>
        ) : null}
        {showActions ? (
          <>
            {isListingMode ? (
              <ListingControls
                quantityValue={quantityValue}
                unitPriceValue={unitPriceValue}
                onQuantityChange={onQuantityChange}
                onUnitPriceChange={onUnitPriceChange}
              />
            ) : null}
            {!isListingMode && onEquip ? (
              <Button variant="contained" onClick={onEquip} disabled={actionDisabled} sx={primaryActionSx}>
                {locale.actionEquip}
              </Button>
            ) : null}
            {isListingMode ? (
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="center" alignItems="center">
                <Button variant="contained" sx={secondaryActionSx} onClick={onList} disabled={actionDisabled}>
                  {locale.actionListConfirm}
                </Button>
                <Button variant="outlined" onClick={onCancelListing} disabled={actionDisabled} sx={destructiveActionSx}>
                  {locale.actionCancel}
                </Button>
              </Stack>
            ) : (
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1} justifyContent="center" alignItems="center">
                <Button variant="outlined" onClick={onSynthesize} disabled={actionDisabled} sx={secondaryActionSx}>
                  {locale.actionSynthesize}
                </Button>
                <Button variant="outlined" sx={secondaryActionSx} onClick={onStartListing} disabled={actionDisabled}>
                  {locale.actionList}
                </Button>
                <Button variant="outlined" sx={destructiveActionSx} onClick={onDelete} disabled={actionDisabled}>
                  {locale.actionDelete}
                </Button>
              </Stack>
            )}
          </>
        ) : null}
      </Stack>
    </Paper>
  )
}
