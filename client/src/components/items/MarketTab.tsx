import { Box, ButtonBase, InputBase, Pagination, Typography } from '@mui/material'
import { itemsPanelColors as colors } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { ItemIcon } from './ItemIllustrations'
import { getListingSummary, getListingTitle } from './itemsLogic'
import {
  ActionButton,
  BagFrame,
  CoinIcon,
  DoubleFrame,
  FrameHeading,
  GoldChangeRow,
  MarketTabIcon,
  Operator,
} from './ItemsParts'
import { formatGold } from './itemUtils'
import locale from '../../../locale/items/Items.json'
import type { MarketListingView } from '@/schema/item'

export type MarketTabKey = 'public' | 'mine'
export type MarketCategoryFilter = 'all' | 'Weapon' | 'Armor' | 'Item' | 'Map'

const categoryFilters: Array<[MarketCategoryFilter, string]> = [
  ['all', locale.filterAll],
  ['Weapon', locale.categoryWeapon],
  ['Armor', locale.categoryArmor],
  ['Item', locale.categoryItem],
  ['Map', locale.categoryMap],
]

function SegmentButton({ label, selected, onClick }: { label: string; selected: boolean; onClick: () => void }) {
  return (
    <ButtonBase
      role="tab"
      aria-selected={selected}
      onClick={onClick}
      disableRipple
      sx={{
        height: 38,
        px: 2.25,
        borderRadius: 999,
        border: `2px solid ${colors.leather}`,
        backgroundColor: selected ? colors.leather : 'transparent',
        color: selected ? '#ffffff' : colors.leather,
        fontFamily: 'inherit',
        fontSize: '0.9375rem',
        fontWeight: 800,
      }}
    >
      {label}
    </ButtonBase>
  )
}

function ListingRow({
  listing,
  selected,
  showSeller,
  onSelect,
}: {
  listing: MarketListingView
  selected: boolean
  showSeller: boolean
  onSelect: () => void
}) {
  const sellerImageSrc = resolveCharacterAssetPath(listing.sellerImagePath)
  return (
    <ButtonBase
      onClick={onSelect}
      aria-pressed={selected}
      disableRipple
      sx={{
        display: 'grid',
        gridTemplateColumns: {
          xs: '52px minmax(0, 1fr) auto',
          md: showSeller ? '64px minmax(0, 1fr) 170px 120px 132px' : '64px minmax(0, 1fr) 120px 132px',
        },
        alignItems: 'center',
        gap: { xs: 1.25, md: 2 },
        px: { xs: 1.25, md: 1.75 },
        py: 1,
        borderRadius: '14px',
        textAlign: 'left',
        fontFamily: 'inherit',
        backgroundColor: colors.slot,
        border: selected ? `3px solid ${colors.brass}` : `2px solid ${colors.slotLine}`,
        boxShadow: selected ? `0 0 0 4px ${colors.brassSoft}` : 'none',
      }}
    >
      <Box
        sx={{
          width: { xs: 52, md: 64 },
          height: { xs: 52, md: 64 },
          display: 'grid',
          placeItems: 'center',
          backgroundColor: '#ffffff',
          borderRadius: '12px',
        }}
      >
        <ItemIcon item={listing} size={44} />
      </Box>
      <Box component="span" sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.25 }}>
        <Box component="span" sx={{ fontSize: '1.125rem', fontWeight: 800, color: colors.text }}>
          {getListingTitle(listing)}
        </Box>
        <Box component="span" sx={{ fontSize: '0.875rem', color: colors.label }}>
          {getListingSummary(listing)}
          <Box component="span" sx={{ display: { md: 'none' } }}>
            {`・${locale.remaining.replace('{count}', String(listing.quantity))}`}
          </Box>
        </Box>
      </Box>
      {showSeller ? (
        <Box component="span" sx={{ display: { xs: 'none', md: 'flex' }, alignItems: 'center', gap: 1, minWidth: 0 }}>
          {sellerImageSrc ? (
            <Box
              component="img"
              src={sellerImageSrc}
              alt=""
              sx={{
                width: 36,
                height: 36,
                objectFit: 'contain',
                objectPosition: 'center bottom',
                borderRadius: '8px',
                backgroundColor: '#f3e9d6',
              }}
            />
          ) : null}
          <Box
            component="span"
            sx={{
              fontSize: '0.9375rem',
              fontWeight: 700,
              color: colors.text,
              overflow: 'hidden',
              textOverflow: 'ellipsis',
              whiteSpace: 'nowrap',
            }}
          >
            {listing.sellerName}
          </Box>
        </Box>
      ) : null}
      <Box component="span" sx={{ display: { xs: 'none', md: 'block' }, fontSize: '0.9375rem', color: colors.label }}>
        {locale.remaining.replace('{count}', String(listing.quantity))}
      </Box>
      <Box
        component="span"
        sx={{
          justifySelf: 'end',
          width: { xs: 104, md: 132 },
          boxSizing: 'border-box',
          textAlign: 'right',
          fontVariantNumeric: 'tabular-nums',
          position: 'relative',
          py: 0.75,
          pr: 1.75,
          pl: 3.25,
          backgroundColor: colors.priceTag,
          border: `2px solid ${colors.brass}`,
          borderRadius: '6px 12px 12px 6px',
          fontSize: { xs: '1rem', md: '1.1875rem' },
          fontWeight: 800,
          color: colors.priceTagText,
        }}
      >
        <Box
          component="span"
          aria-hidden="true"
          sx={{
            position: 'absolute',
            left: 9,
            top: '50%',
            width: 8,
            height: 8,
            mt: '-4px',
            borderRadius: '50%',
            backgroundColor: colors.slot,
            border: `2px solid ${colors.brass}`,
          }}
        />
        {formatGold(listing.unitPrice)}
      </Box>
    </ButtonBase>
  )
}

export function MarketList({
  tab,
  onTabChange,
  search,
  onSearchChange,
  category,
  onCategoryChange,
  listings,
  selectedId,
  onSelect,
  page,
  pageCount,
  onPageChange,
  isLoading,
  isFiltered,
  fill,
}: {
  tab: MarketTabKey
  onTabChange: (tab: MarketTabKey) => void
  search: string
  onSearchChange: (value: string) => void
  category: MarketCategoryFilter
  onCategoryChange: (value: MarketCategoryFilter) => void
  listings: MarketListingView[]
  selectedId: string | null
  onSelect: (listing: MarketListingView) => void
  page: number
  pageCount: number
  onPageChange: (page: number) => void
  isLoading: boolean
  isFiltered: boolean
  fill: boolean
}) {
  return (
    <BagFrame fill={fill}>
      {/* 露店の日よけ */}
      <Box
        aria-hidden="true"
        sx={{
          height: 34,
          mx: { xs: -1.25, sm: -2.5 },
          mt: { xs: -1.5, sm: -2.25 },
          borderRadius: '10px 10px 0 0',
          background: `repeating-linear-gradient(90deg, ${colors.leather} 0 56px, #fbf1dc 56px 112px)`,
          boxShadow: 'inset 0 -6px 0 rgba(0,0,0,0.12)',
          flex: 'none',
        }}
      />
      <Box sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1.5 }}>
        <Box role="tablist" aria-label={locale.marketTabsLabel} sx={{ display: 'flex', gap: 1 }}>
          <SegmentButton
            label={locale.publicMarketTab}
            selected={tab === 'public'}
            onClick={() => onTabChange('public')}
          />
          <SegmentButton label={locale.myListingsTab} selected={tab === 'mine'} onClick={() => onTabChange('mine')} />
        </Box>
        <InputBase
          value={search}
          onChange={(event) => onSearchChange(event.target.value)}
          placeholder={locale.searchPlaceholderMarket}
          inputProps={{ 'aria-label': locale.searchLabel }}
          sx={{
            flex: 1,
            minWidth: 220,
            height: 42,
            px: 2,
            border: `2px solid ${colors.slotLine}`,
            borderRadius: 999,
            backgroundColor: '#ffffff',
            fontFamily: 'inherit',
            fontSize: '0.9375rem',
          }}
        />
      </Box>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.5, borderBottom: `2px dashed ${colors.stitch}` }}>
        {categoryFilters.map(([value, label]) => (
          <ButtonBase
            key={value}
            onClick={() => onCategoryChange(value)}
            aria-pressed={category === value}
            disableRipple
            sx={{
              height: 38,
              px: 1.75,
              borderBottom: `3px solid ${category === value ? colors.leather : 'transparent'}`,
              fontFamily: 'inherit',
              fontSize: '0.9375rem',
              fontWeight: 800,
              color: category === value ? colors.text : colors.label,
            }}
          >
            {label}
          </ButtonBase>
        ))}
      </Box>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          p: '4px',
          m: '-4px',
          display: 'flex',
          flexDirection: 'column',
          gap: 1,
        }}
      >
        {isLoading ? <Typography sx={{ color: colors.label }}>{locale.marketLoading}</Typography> : null}
        {!isLoading && listings.length === 0 ? (
          <Typography sx={{ color: colors.label }}>
            {isFiltered ? locale.noSearchResults : locale.emptyMarket}
          </Typography>
        ) : null}
        {listings.map((listing) => (
          <ListingRow
            key={listing.listingId}
            listing={listing}
            selected={listing.listingId === selectedId}
            showSeller={tab === 'public'}
            onSelect={() => onSelect(listing)}
          />
        ))}
      </Box>
      {pageCount > 1 ? (
        <Pagination
          page={page}
          count={pageCount}
          onChange={(_, next) => onPageChange(next)}
          sx={{
            alignSelf: 'center',
            '& .MuiPaginationItem-root': { fontFamily: 'inherit', fontWeight: 800, color: colors.leather },
            '& .MuiPaginationItem-root.Mui-selected': { backgroundColor: colors.leather, color: '#ffffff' },
          }}
        />
      ) : null}
    </BagFrame>
  )
}

const stepButtonSx = {
  width: 46,
  height: 46,
  borderRadius: '10px',
  border: `2px solid ${colors.leather}`,
  backgroundColor: '#ffffff',
  color: colors.leather,
  fontSize: '1.375rem',
  fontWeight: 800,
  '&.Mui-disabled': { opacity: 0.4 },
} as const

// 購入の箱。品物、購入数（単価 × 数 = 合計）、所持金の増減、購入ボタン。自分の出品では購入の操作を出さない
export function PurchasePanel({
  listing,
  isMine,
  quantity,
  onQuantityChange,
  gold,
  onPurchase,
  busy,
  fill,
}: {
  listing: MarketListingView | null
  isMine: boolean
  quantity: number
  onQuantityChange: (value: number) => void
  gold?: number
  onPurchase: () => void
  busy: boolean
  fill: boolean
}) {
  if (!listing) {
    return (
      <DoubleFrame fill={fill}>
        <FrameHeading icon={<MarketTabIcon />} title={isMine ? locale.myListingsTab : locale.purchaseTitle} />
      </DoubleFrame>
    )
  }

  const count = isMine ? listing.quantity : quantity
  const total = listing.unitPrice * count
  const goldAfter = gold != null ? gold - total : null

  return (
    <DoubleFrame fill={fill}>
      <FrameHeading icon={<MarketTabIcon />} title={isMine ? locale.myListingsTab : locale.purchaseTitle} />
      <Box sx={{ display: 'flex', alignItems: 'center', gap: 2.5 }}>
        <Box
          sx={{
            width: { xs: 100, sm: 140 },
            height: { xs: 100, sm: 140 },
            flex: 'none',
            display: 'grid',
            placeItems: 'center',
            backgroundColor: '#ffffff',
            border: `3px solid ${colors.slotLine}`,
            borderRadius: '16px',
          }}
        >
          <ItemIcon item={listing} size={92} />
        </Box>
        <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Typography component="span" sx={{ fontSize: '1.5rem', fontWeight: 800, color: colors.text }}>
            {getListingTitle(listing)}
          </Typography>
          <Typography component="span" sx={{ fontSize: '0.9375rem', color: colors.label }}>
            {getListingSummary(listing)}
          </Typography>
        </Box>
      </Box>
      <Box
        sx={{
          display: 'flex',
          flexDirection: 'column',
          gap: 1.5,
          py: 2,
          borderTop: `2px dashed ${colors.stitch}`,
          borderBottom: `2px dashed ${colors.stitch}`,
        }}
      >
        <Typography component="span" sx={{ fontSize: '0.9375rem', fontWeight: 800, color: colors.label }}>
          {isMine
            ? locale.remaining.replace('{count}', String(listing.quantity))
            : `${locale.purchaseQuantity} (${quantity}/${listing.quantity})`}
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
          <Box component="span" sx={{ fontSize: '1.5rem', fontWeight: 800, color: colors.text }}>
            {formatGold(listing.unitPrice)}
          </Box>
          <Operator>×</Operator>
          {isMine ? (
            <Box component="span" sx={{ fontSize: '1.5rem', fontWeight: 800, color: colors.text }}>
              {listing.quantity}
            </Box>
          ) : (
            <Box sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
              <ButtonBase
                aria-label={locale.decrease}
                onClick={() => onQuantityChange(quantity - 1)}
                disabled={quantity <= 1}
                disableRipple
                sx={stepButtonSx}
              >
                −
              </ButtonBase>
              <InputBase
                value={String(quantity)}
                onChange={(event) => {
                  const next = Number.parseInt(event.target.value.replace(/[^0-9]/g, ''), 10)
                  onQuantityChange(Number.isFinite(next) ? next : 1)
                }}
                inputProps={{ inputMode: 'numeric', 'aria-label': locale.purchaseQuantity }}
                sx={{
                  width: 60,
                  height: 46,
                  border: `2px solid ${colors.slotLine}`,
                  borderRadius: '10px',
                  backgroundColor: '#ffffff',
                  fontFamily: 'inherit',
                  fontSize: '1.25rem',
                  fontWeight: 800,
                  '& input': { textAlign: 'center', p: 0 },
                }}
              />
              <ButtonBase
                aria-label={locale.increase}
                onClick={() => onQuantityChange(quantity + 1)}
                disabled={quantity >= listing.quantity}
                disableRipple
                sx={stepButtonSx}
              >
                ＋
              </ButtonBase>
            </Box>
          )}
          <Operator>=</Operator>
          <Box
            component="span"
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              fontSize: '1.75rem',
              fontWeight: 800,
              color: colors.text,
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            <CoinIcon sx={{ fontSize: 26 }} />
            {formatGold(total)}
          </Box>
        </Box>
      </Box>
      {isMine ? null : (
        <Box sx={{ mt: 'auto', display: 'flex', flexDirection: 'column', gap: 1.25 }}>
          <GoldChangeRow
            from={gold != null ? formatGold(gold) : '-'}
            to={goldAfter != null ? formatGold(goldAfter) : '-'}
          />
          <Box sx={{ mt: 0.75 }}>
            <ActionButton kind="primary" onClick={onPurchase} disabled={busy || goldAfter == null || goldAfter < 0}>
              {locale.actionPurchase}
            </ActionButton>
          </Box>
        </Box>
      )}
    </DoubleFrame>
  )
}
