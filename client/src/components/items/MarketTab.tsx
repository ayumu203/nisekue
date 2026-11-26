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
import { font, palette, withAlpha } from '@/theme/tokens'

export type MarketTabKey = 'public' | 'mine'
export type MarketCategoryFilter = 'all' | 'Weapon' | 'Armor' | 'Item' | 'Map'

const categoryFilters: Array<[MarketCategoryFilter, string]> = [
  ['all', locale.filterAll],
  ['Weapon', locale.categoryWeapon],
  ['Armor', locale.categoryArmor],
  ['Item', locale.categoryItem],
  ['Map', locale.categoryMap],
]

function UnderlineTab({ label, selected, onClick }: { label: string; selected: boolean; onClick: () => void }) {
  return (
    <ButtonBase
      onClick={onClick}
      aria-pressed={selected}
      disableRipple
      sx={{
        height: 38,
        px: { xs: 1.5, sm: 2 },
        borderBottom: `3px solid ${selected ? colors.leather : 'transparent'}`,
        fontFamily: 'inherit',
        fontSize: font.md,
        fontWeight: 800,
        color: selected ? colors.text : colors.label,
        whiteSpace: 'nowrap',
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
          // 価格は折り返さず、大きな額のときだけ列を広げる
          md: showSeller
            ? '64px minmax(0, 1fr) 170px 120px minmax(132px, auto)'
            : '64px minmax(0, 1fr) 120px minmax(132px, auto)',
        },
        alignItems: 'center',
        gap: { xs: 1.5, md: 2 },
        px: { xs: 1.5, md: 2 },
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
          backgroundColor: palette.white,
          borderRadius: '12px',
        }}
      >
        <ItemIcon item={listing} size={44} />
      </Box>
      <Box component="span" sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.25 }}>
        <Box
          component="span"
          sx={{
            fontSize: { xs: font.md, md: font.lg },
            fontWeight: 800,
            color: colors.text,
            overflow: 'hidden',
            textOverflow: 'ellipsis',
            whiteSpace: 'nowrap',
          }}
        >
          {getListingTitle(listing)}
        </Box>
        {/* PC：種類と効果。スマホは効果を並べると読みにくいので、売り手だけにする（自分の出品は残りの数） */}
        <Box component="span" sx={{ display: { xs: 'none', md: 'block' }, fontSize: font.sm, color: colors.label }}>
          {getListingSummary(listing)}
        </Box>
        <Box
          component="span"
          sx={{
            display: { xs: 'flex', md: 'none' },
            alignItems: 'center',
            gap: 0.75,
            fontSize: font.sm,
            color: colors.label,
            minWidth: 0,
          }}
        >
          {showSeller && sellerImageSrc ? (
            <Box
              component="img"
              src={sellerImageSrc}
              alt=""
              sx={{
                width: 24,
                height: 24,
                flex: 'none',
                objectFit: 'contain',
                objectPosition: 'center bottom',
                borderRadius: '6px',
                backgroundColor: palette.cream,
              }}
            />
          ) : null}
          <Box component="span" sx={{ overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
            {showSeller ? listing.sellerName : locale.remaining.replace('{count}', String(listing.quantity))}
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
                backgroundColor: palette.cream,
              }}
            />
          ) : null}
          <Box
            component="span"
            sx={{
              fontSize: font.md,
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
      <Box component="span" sx={{ display: { xs: 'none', md: 'block' }, fontSize: font.md, color: colors.label }}>
        {locale.remaining.replace('{count}', String(listing.quantity))}
      </Box>
      <Box
        component="span"
        sx={{
          justifySelf: 'end',
          minWidth: { xs: 88, md: 132 },
          whiteSpace: 'nowrap',
          boxSizing: 'border-box',
          textAlign: 'right',
          fontVariantNumeric: 'tabular-nums',
          position: 'relative',
          py: 0.75,
          pr: { xs: 1.5, md: 2 },
          pl: { xs: 3, md: 3 },
          backgroundColor: colors.priceTag,
          border: `2px solid ${colors.brass}`,
          borderRadius: '6px 12px 12px 6px',
          fontSize: { xs: font.md, md: font.xl },
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
            mt: -0.5,
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
          mx: { xs: -1.5, sm: -2.5 },
          mt: { xs: -1.5, sm: -2.5 },
          borderRadius: '10px 10px 0 0',
          background: `repeating-linear-gradient(90deg, ${colors.leather} 0 56px, ${palette.cream} 56px 112px)`,
          boxShadow: `inset 0 -6px 0 ${withAlpha(palette.navy, 0.1)}`,
          flex: 'none',
        }}
      />
      <InputBase
        value={search}
        onChange={(event) => onSearchChange(event.target.value)}
        placeholder={locale.searchPlaceholderMarket}
        inputProps={{ 'aria-label': locale.searchLabel }}
        sx={{
          flex: 'none',
          height: 42,
          px: 2,
          border: `2px solid ${colors.slotLine}`,
          borderRadius: 999,
          backgroundColor: palette.white,
          fontFamily: 'inherit',
          fontSize: font.md,
        }}
      />
      {/* 種類の絞り込みと「自分の出品」を 1 行の下線タブにまとめる。自分の出品は区切り線の右に置く */}
      <Box
        sx={{
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: 0.5,
          borderBottom: `2px dashed ${colors.stitch}`,
        }}
      >
        {categoryFilters.map(([value, label]) => (
          <UnderlineTab
            key={value}
            label={label}
            selected={tab === 'public' && category === value}
            onClick={() => {
              onTabChange('public')
              onCategoryChange(value)
            }}
          />
        ))}
        <Box
          aria-hidden="true"
          sx={{ width: '2px', height: 22, mx: { xs: 0.5, sm: 1 }, backgroundColor: colors.stitch }}
        />
        <UnderlineTab
          label={locale.myListingsTab}
          selected={tab === 'mine'}
          onClick={() => {
            onTabChange('mine')
            onCategoryChange('all')
          }}
        />
      </Box>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          p: 0.5,
          m: -0.5,
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
            '& .MuiPaginationItem-root.Mui-selected': { backgroundColor: colors.leather, color: palette.white },
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
  backgroundColor: palette.white,
  color: colors.leather,
  fontSize: font.xxl,
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
            backgroundColor: palette.white,
            border: `3px solid ${colors.slotLine}`,
            borderRadius: '16px',
          }}
        >
          <ItemIcon item={listing} size={92} />
        </Box>
        <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 1 }}>
          <Typography component="span" sx={{ fontSize: font.xxl, fontWeight: 800, color: colors.text }}>
            {getListingTitle(listing)}
          </Typography>
          <Typography component="span" sx={{ fontSize: font.md, color: colors.label }}>
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
        <Typography component="span" sx={{ fontSize: font.md, fontWeight: 800, color: colors.label }}>
          {isMine
            ? locale.remaining.replace('{count}', String(listing.quantity))
            : `${locale.purchaseQuantity} (${quantity}/${listing.quantity})`}
        </Typography>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: 1 }}>
          <Box component="span" sx={{ fontSize: font.xxl, fontWeight: 800, color: colors.text }}>
            {formatGold(listing.unitPrice)}
          </Box>
          <Operator>×</Operator>
          {isMine ? (
            <Box component="span" sx={{ fontSize: font.xxl, fontWeight: 800, color: colors.text }}>
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
                  backgroundColor: palette.white,
                  fontFamily: 'inherit',
                  fontSize: font.xl,
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
              fontSize: font.xxxl,
              fontWeight: 800,
              color: colors.text,
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            <CoinIcon sx={{ fontSize: font.xxxl }} />
            {formatGold(total)}
          </Box>
        </Box>
      </Box>
      {isMine ? null : (
        <Box sx={{ mt: 'auto', display: 'flex', flexDirection: 'column', gap: 1.5 }}>
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
