import { Box, Button, Chip, Paper, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import { ItemArtwork } from './ItemIllustrations'
import { ListingControls } from './ListingControls'
import { formatDateTime, formatStatusBonus } from './itemUtils'
import { accentBeige, accentBeigeBorder, deepGreen, listCardSx, primaryActionSx } from './ItemsConstants'
import { resolveCharacterAssetPath } from '@/lib/assets'
import locale from '../../../locale/items/Items.json'
import type { MarketListingView } from '@/schema/item'

const categoryLabelMap: Record<string, string> = {
  Weapon: locale.categoryWeapon,
  Armor: locale.categoryArmor,
  Item: locale.categoryItem,
  Map: locale.categoryMap,
}

export function MarketCard({
  listing,
  quantityValue,
  onQuantityChange,
  onPurchase,
  actionDisabled,
  showSeller,
}: {
  listing: MarketListingView
  quantityValue: string
  onQuantityChange: (value: string) => void
  onPurchase?: () => void
  actionDisabled: boolean
  showSeller: boolean
}) {
  const [showDetails, setShowDetails] = useState(false)
  const sellerImageSrc = resolveCharacterAssetPath(listing.sellerImagePath)
  const sellerRoomPath = listing.sellerId ? `/players/${listing.sellerId}/visit` : null
  const equipmentDetail = listing.equipmentDetail ?? null
  const detailRegionId = `market-detail-${listing.listingId}`

  return (
    <Paper
      variant="outlined"
      sx={{
        ...listCardSx,
        minHeight: '100%',
      }}
    >
      <Stack spacing={1}>
        <ItemArtwork item={listing} sellerImagePath={listing.sellerImagePath} />
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
            {listing.itemName}
          </Typography>
        </Box>
        <Typography variant="body2" fontWeight={800} color={deepGreen} sx={{ textAlign: 'center' }}>
          {listing.unitPrice} Gold
        </Typography>
        <Typography variant="caption" color="text.secondary" sx={{ textAlign: 'center', minHeight: 34 }}>
          {listing.flavorText || '-'}
        </Typography>
        {equipmentDetail ? (
          <Button
            variant="text"
            onClick={() => setShowDetails((current) => !current)}
            aria-expanded={showDetails}
            aria-controls={detailRegionId}
            sx={{ alignSelf: 'center', color: deepGreen, fontWeight: 700 }}
          >
            {showDetails ? locale.hideDetails : locale.showDetails}
          </Button>
        ) : null}
        {showDetails && equipmentDetail ? (
          <Stack
            id={detailRegionId}
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
              {locale.effectAmount} {formatStatusBonus(equipmentDetail)}
            </Typography>
            {equipmentDetail.plusValue > 0 ? (
              <Typography variant="body2" fontWeight={700} color={deepGreen}>
                +{equipmentDetail.plusValue}
              </Typography>
            ) : null}
          </Stack>
        ) : null}
        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap justifyContent="center">
          <Chip
            size="small"
            label={categoryLabelMap[listing.listingCategory] ?? listing.listingCategory}
            variant="outlined"
            sx={{ borderRadius: 1, bgcolor: 'rgba(255,255,255,0.92)' }}
          />
          <Chip
            size="small"
            label={`${locale.quantity} ${listing.quantity}`}
            variant="outlined"
            sx={{ borderRadius: 1, bgcolor: 'rgba(255,255,255,0.92)' }}
          />
          <Chip
            size="small"
            label={`${locale.expiresAt} ${formatDateTime(listing.expiresAt)}`}
            variant="outlined"
            sx={{ borderRadius: 1, bgcolor: 'rgba(255,255,255,0.92)' }}
          />
        </Stack>
        {showSeller && listing.sellerName ? (
          <Stack direction="row" spacing={1.25} alignItems="center">
            <Box
              component={sellerRoomPath ? Link : 'div'}
              to={sellerRoomPath ?? undefined}
              aria-label={locale.visitSellerRoom.replace('{name}', listing.sellerName)}
              sx={{
                width: 52,
                height: 52,
                borderRadius: 2,
                border: `2px solid ${accentBeigeBorder}`,
                backgroundColor: 'rgba(255, 249, 232, 0.9)',
                overflow: 'hidden',
                display: 'grid',
                placeItems: 'center',
                flexShrink: 0,
                textDecoration: 'none',
                transition: 'transform 140ms ease, box-shadow 140ms ease',
                ...(sellerRoomPath
                  ? {
                      cursor: 'pointer',
                      '&:hover': {
                        transform: 'translateY(-1px)',
                        boxShadow: '0 6px 14px rgba(22, 44, 31, 0.16)',
                      },
                    }
                  : null),
              }}
            >
              {sellerImageSrc ? (
                <Box
                  component="img"
                  src={sellerImageSrc}
                  alt={listing.sellerName}
                  sx={{ width: '100%', height: '100%', objectFit: 'contain', objectPosition: 'center bottom' }}
                />
              ) : (
                <Typography variant="caption" color="text.secondary">
                  ?
                </Typography>
              )}
            </Box>
            <Stack spacing={0.25} minWidth={0}>
              <Typography variant="caption" color="text.secondary">
                {locale.seller}
              </Typography>
              <Typography variant="body2" fontWeight={700} color={deepGreen}>
                {listing.sellerName}
              </Typography>
            </Stack>
          </Stack>
        ) : null}
        {onPurchase ? (
          <>
            <ListingControls quantityValue={quantityValue} onQuantityChange={onQuantityChange} showUnitPrice={false} />
            <Button variant="contained" onClick={onPurchase} disabled={actionDisabled} sx={primaryActionSx}>
              {locale.actionPurchase}
            </Button>
          </>
        ) : null}
      </Stack>
    </Paper>
  )
}
