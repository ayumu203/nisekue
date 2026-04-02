import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { ItemArtwork } from './ItemIllustrations'
import { ListingControls } from './ListingControls'
import { formatStatusBonus, getEffectTypeLabel } from './itemUtils'
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
import type { ItemStackView } from '@/schema/item'

export function ConsumableCard({
  item,
  quantityValue,
  unitPriceValue,
  onQuantityChange,
  onUnitPriceChange,
  onUse,
  onDelete,
  onList,
  actionDisabled,
}: {
  item: ItemStackView
  quantityValue: string
  unitPriceValue: string
  onQuantityChange: (value: string) => void
  onUnitPriceChange: (value: string) => void
  onUse: () => void
  onDelete: () => void
  onList: () => void
  actionDisabled: boolean
}) {
  const [showDetails, setShowDetails] = useState(false)
  const [showListingControls, setShowListingControls] = useState(false)

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
        <Typography variant="body2" fontWeight={700} color={deepGreen} sx={{ textAlign: 'center' }}>
          {item.quantity}個
        </Typography>
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
              {locale.effectType} {getEffectTypeLabel(item.effectType)}
            </Typography>
            <Typography variant="body2">
              {locale.effectAmount} {formatStatusBonus(item)}
            </Typography>
            {item.requiredLevel !== null ? (
              <Typography variant="body2">
                {locale.requiredLevel} {item.requiredLevel}
              </Typography>
            ) : null}
            {item.changeJobTo ? (
              <Typography variant="body2">
                {locale.changeJobTo} {item.changeJobTo}
              </Typography>
            ) : null}
          </Stack>
        ) : null}
        {showListingControls ? (
          <ListingControls
            quantityValue={quantityValue}
            unitPriceValue={unitPriceValue}
            onQuantityChange={onQuantityChange}
            onUnitPriceChange={onUnitPriceChange}
          />
        ) : null}
        <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
          <Button variant="contained" onClick={onUse} disabled={actionDisabled} sx={primaryActionSx}>
            {locale.actionUse}
          </Button>
          {showListingControls ? (
            <Button variant="contained" sx={secondaryActionSx} onClick={onList} disabled={actionDisabled}>
              {locale.actionListConfirm}
            </Button>
          ) : (
            <Button
              variant="outlined"
              sx={secondaryActionSx}
              onClick={() => setShowListingControls(true)}
              disabled={actionDisabled}
            >
              {locale.actionList}
            </Button>
          )}
          <Button variant="outlined" sx={destructiveActionSx} onClick={onDelete} disabled={actionDisabled}>
            {locale.actionDelete}
          </Button>
        </Stack>
        {showListingControls ? (
          <Button
            variant="text"
            onClick={() => setShowListingControls(false)}
            disabled={actionDisabled}
            sx={{ alignSelf: 'flex-end', color: 'text.secondary' }}
          >
            {locale.actionClose}
          </Button>
        ) : null}
      </Stack>
    </Paper>
  )
}
