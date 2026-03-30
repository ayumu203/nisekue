import {
  Alert,
  Box,
  Button,
  Chip,
  CircularProgress,
  Container,
  Pagination,
  Paper,
  Stack,
  SvgIcon,
  Tab,
  Tabs,
  TextField,
  Typography,
  type SvgIconProps,
} from '@mui/material'
import { useEffect, useMemo, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import useSWR, { useSWRConfig } from 'swr'
import {
  createMarketListing,
  deleteEquipment,
  deleteItemStack,
  getItems,
  getMarketListings,
  getMyMarketListings,
  purchaseMarketListing,
  synthesizeEquipment,
  useItem as consumeItem,
} from '@/api/item'
import { createPlayer, updatePlayerEquipment } from '@/api/player'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { useAuth } from '@/contexts/useAuth'
import { innerSurfaceSx, outerPagePaperSx } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/items/Items.json'
import type { InventoryItemView, ItemEquipmentView, ItemStackView, MarketListingView } from '@/schema/item'

const PAGE_SIZE = 30
const deepGreen = '#1f4a33'
const deepGreenBorder = '#2e6a49'
const accentBeige = '#f0ddb5'
const accentBeigeBorder = '#d4b57a'

const itemInnerPanelSx = {
  ...innerSurfaceSx,
  backgroundColor: '#ffffff',
  borderColor: deepGreenBorder,
  boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.75)',
} as const
const framedPanelSx = {
  borderRadius: 2,
  border: `1px solid ${deepGreenBorder}`,
  boxShadow: '0 8px 20px rgba(22, 44, 31, 0.14)',
} as const
const listCardSx = {
  p: 1,
  borderRadius: 2,
  background: '#ffffff',
  borderColor: '#c6d6ca',
  boxShadow: '0 4px 12px rgba(22, 44, 31, 0.08)',
  transition: 'transform 140ms ease, box-shadow 140ms ease, border-color 140ms ease',
  '&:hover': {
    transform: 'translateY(-2px)',
    borderColor: deepGreenBorder,
    boxShadow: '0 10px 20px rgba(22, 44, 31, 0.14)',
  },
} as const
const inputSx = {
  '& .MuiOutlinedInput-root': {
    borderRadius: 2,
    backgroundColor: '#ffffff',
    '& fieldset': {
      borderColor: '#9bb89e',
    },
    '&:hover fieldset': {
      borderColor: deepGreenBorder,
    },
    '&.Mui-focused fieldset': {
      borderColor: deepGreenBorder,
      borderWidth: 2,
    },
  },
  '& .MuiInputLabel-root.Mui-focused': {
    color: deepGreen,
  },
} as const
const primaryActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    background: '#52785d',
    color: '#ffffff',
    boxShadow: 'none',
  },
  '&&:hover': {
    background: '#45684f',
    boxShadow: 'none',
  },
} as const
const secondaryActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    borderColor: '#6f8f77',
    color: deepGreen,
    backgroundColor: '#ffffff',
    boxShadow: 'none',
  },
  '&&:hover': {
    borderColor: deepGreenBorder,
    backgroundColor: '#f4f7f4',
    boxShadow: 'none',
  },
} as const
const destructiveActionSx = {
  '&&': {
    borderRadius: 2,
    minHeight: 38,
    borderColor: '#b88d8d',
    color: '#7b4b4b',
    backgroundColor: '#ffffff',
    boxShadow: 'none',
  },
  '&&:hover': {
    borderColor: '#9e6f6f',
    backgroundColor: '#faf5f5',
    boxShadow: 'none',
  },
} as const
const tabsPaperSx = {
  p: 0.75,
  borderRadius: 2,
  border: `1px solid ${deepGreenBorder}`,
  backgroundColor: deepGreen,
} as const
const tabsSx = {
  minHeight: 40,
  '& .MuiTab-root': {
    minHeight: 40,
    borderRadius: 1.5,
    fontWeight: 800,
    color: 'rgba(255,255,255,0.82)',
    opacity: 1,
    transition: 'color 140ms ease, background-color 140ms ease',
    position: 'relative',
    zIndex: 1,
  },
  '& .MuiTab-root.Mui-selected': {
    color: `${deepGreen} !important`,
    backgroundColor: accentBeige,
  },
  '& .MuiTabs-indicator': {
    display: 'none',
  },
} as const

type PrimaryTab = 'items' | 'market'
type MarketTab = 'public' | 'mine'

function WeaponIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M78 12 91 25 48 68l-8 24-11 11-5-5 11-11 24-8z" fill="#f1e3b2" />
      <path d="m74 16 14 14 7-7-14-14z" fill="#d2b36f" />
      <path d="m39 73 8 8-4 12-16 5 5-16z" fill="#855e32" />
    </SvgIcon>
  )
}

function ArmorIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M38 18h44l8 15-8 17v42L60 104 38 92V50l-8-17z" fill="#d7e4ef" />
      <path d="M46 26h28v18H46z" fill="#86a4bb" />
      <path d="M38 50h44v12H38z" fill="#6c879b" />
    </SvgIcon>
  )
}

function StatBoostIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <circle cx="60" cy="60" r="28" fill="#d94545" />
      <path
        d="M60 20c20 0 36 16 36 36 0 18-14 39-36 48-22-9-36-30-36-48 0-20 16-36 36-36"
        fill="none"
        stroke="#f7d98a"
        strokeWidth="8"
      />
      <path d="M56 42h8v14h14v8H64v14h-8V64H42v-8h14z" fill="#fff5d9" />
    </SvgIcon>
  )
}

function JobChangeIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <circle cx="38" cy="42" r="14" fill="#7aa96f" />
      <path d="M22 90c2-18 12-28 26-28s24 10 26 28" fill="#7aa96f" />
      <path
        d="M68 34h26v10H68zM82 20l18 19-18 19"
        fill="none"
        stroke="#f1d684"
        strokeWidth="10"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
    </SvgIcon>
  )
}

function renderItemArtwork(item: InventoryItemView | MarketListingView, sellerImagePath?: string | null) {
  const sellerImageSrc = resolveCharacterAssetPath(sellerImagePath)

  return (
    <Box
      sx={{
        borderRadius: 2,
        border: '1px solid #b08e66',
        background: '#a88d73',
        height: 96,
        display: 'grid',
        placeItems: 'center',
        overflow: 'hidden',
        position: 'relative',
      }}
    >
      {'kind' in item ? (
        item.kind === 'equipment' ? (
          item.equipmentType === 'Weapon' ? (
            <WeaponIllustration sx={{ fontSize: 72, color: '#6d5630' }} />
          ) : (
            <ArmorIllustration sx={{ fontSize: 72, color: '#5a7387' }} />
          )
        ) : item.effectType === 'StatBoost' ? (
          <StatBoostIllustration sx={{ fontSize: 72, color: '#a53636' }} />
        ) : (
          <JobChangeIllustration sx={{ fontSize: 72, color: '#4f7148' }} />
        )
      ) : item.itemName.includes('剣') ? (
        <WeaponIllustration sx={{ fontSize: 72, color: '#6d5630' }} />
      ) : item.itemName.includes('服') || item.itemName.includes('鎧') ? (
        <ArmorIllustration sx={{ fontSize: 72, color: '#5a7387' }} />
      ) : item.itemName.includes('証') ? (
        <JobChangeIllustration sx={{ fontSize: 72, color: '#4f7148' }} />
      ) : (
        <StatBoostIllustration sx={{ fontSize: 72, color: '#a53636' }} />
      )}

      {sellerImageSrc ? (
        <Box
          sx={{
            position: 'absolute',
            right: 8,
            bottom: 8,
            width: 32,
            height: 32,
            borderRadius: 1,
            border: '2px solid #f7e0ae',
            backgroundColor: 'rgba(255, 249, 232, 0.96)',
            overflow: 'hidden',
          }}
        >
          <Box
            component="img"
            src={sellerImageSrc}
            alt=""
            sx={{ width: '100%', height: '100%', objectFit: 'contain', objectPosition: 'center bottom' }}
          />
        </Box>
      ) : null}
    </Box>
  )
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ja-JP', {
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

function getEffectTypeLabel(effectType: ItemStackView['effectType']): string {
  switch (effectType) {
    case 'StatBoost':
      return locale.effectTypeStatBoost
    case 'ChangeJob':
      return locale.effectTypeChangeJob
  }
}

function formatStatusBonus(item: {
  statusBonus: ItemStackView['statusBonus'] | ItemEquipmentView['statusBonus']
  statusBonusPercent?: ItemStackView['statusBonusPercent'] | null
}): string {
  if (!item.statusBonus && !item.statusBonusPercent) {
    return '-'
  }

  const labels: Array<[string, number]> = [
    ['HP', item.statusBonus?.maxHp ?? 0],
    ['MP', item.statusBonus?.maxMp ?? 0],
    ['STR', item.statusBonus?.strength ?? 0],
    ['DEF', item.statusBonus?.defense ?? 0],
    ['INT', item.statusBonus?.intelligence ?? 0],
    ['LUK', item.statusBonus?.luck ?? 0],
    ['SPD', item.statusBonus?.speed ?? 0],
  ]
  const percentLabels: Array<[string, number]> = [
    ['HP', item.statusBonusPercent?.maxHp ?? 0],
    ['MP', item.statusBonusPercent?.maxMp ?? 0],
    ['STR', item.statusBonusPercent?.strength ?? 0],
    ['DEF', item.statusBonusPercent?.defense ?? 0],
    ['INT', item.statusBonusPercent?.intelligence ?? 0],
    ['LUK', item.statusBonusPercent?.luck ?? 0],
    ['SPD', item.statusBonusPercent?.speed ?? 0],
  ]

  const active = labels.filter(([, value]) => value !== 0).map(([label, value]) => `${label}+${value}`)
  const activePercent = percentLabels.filter(([, value]) => value !== 0).map(([label, value]) => `${label}+${value}%`)
  const effects = [...active, ...activePercent]
  return effects.length > 0 ? effects.join(' / ') : '-'
}

function parsePositiveInteger(value: string | undefined, fallback: number): number {
  const parsed = Number.parseInt(value ?? '', 10)
  return Number.isFinite(parsed) && parsed > 0 ? parsed : fallback
}

function paginate<T>(items: T[], page: number): T[] {
  const start = (page - 1) * PAGE_SIZE
  return items.slice(start, start + PAGE_SIZE)
}

function normalizeText(value: string): string {
  return value.trim().toLocaleLowerCase('ja-JP')
}

function matchesMarketSearch(listing: MarketListingView, searchText: string): boolean {
  if (!searchText) {
    return true
  }

  return [listing.itemName, listing.flavorText, listing.sellerName ?? ''].some((value) =>
    normalizeText(value).includes(searchText),
  )
}

function ListingControls({
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

function ItemSummary({ usedSlots, capacity, gold }: { usedSlots?: number; capacity?: number; gold?: number }) {
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

function ControlFrame({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        p: { xs: 1.5, sm: 2 },
      }}
    >
      {children}
    </Box>
  )
}

function PageFrame({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        ...framedPanelSx,
        p: { xs: 1.5, sm: 2 },
        background: deepGreen,
      }}
    >
      <Stack spacing={2}>{children}</Stack>
    </Box>
  )
}

function SectionFrame({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        px: { xs: 0.25, sm: 0.5 },
        py: 0.25,
      }}
    >
      <Stack spacing={2}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          spacing={1}
          alignItems={{ sm: 'center' }}
        >
          <Typography variant="h5" fontWeight={900} color="#ffffff">
            {title}
          </Typography>
          {subtitle ? (
            <Typography variant="body2" color="rgba(255,255,255,0.76)">
              {subtitle}
            </Typography>
          ) : null}
        </Stack>
        {children}
      </Stack>
    </Box>
  )
}

function EquipmentCard({
  item,
  quantityValue,
  unitPriceValue,
  onQuantityChange,
  onUnitPriceChange,
  onSynthesize,
  onDelete,
  onList,
  onEquip,
  actionDisabled,
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
  onEquip?: () => void
  actionDisabled: boolean
  showActions?: boolean
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
        {renderItemArtwork(item)}
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
            <Typography variant="body2">
              {locale.durability} {item.durability}/{item.maxDurability}
            </Typography>
            <Typography variant="body2">
              {locale.mastery} {item.mastery}/{item.masteryCap}
            </Typography>
            <Typography variant="body2">
              {locale.synthesisCost} {item.synthesisGoldCost} Gold
            </Typography>
          </Stack>
        ) : null}
        {showActions ? (
          <>
            {showListingControls ? (
              <ListingControls
                quantityValue={quantityValue}
                unitPriceValue={unitPriceValue}
                onQuantityChange={onQuantityChange}
                onUnitPriceChange={onUnitPriceChange}
              />
            ) : null}
            {onEquip ? (
              <Button
                variant="outlined"
                onClick={onEquip}
                disabled={actionDisabled}
                sx={secondaryActionSx}
              >
                {locale.actionEquip}
              </Button>
            ) : null}
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <Button variant="contained" onClick={onSynthesize} disabled={actionDisabled} sx={primaryActionSx}>
                {locale.actionSynthesize}
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
          </>
        ) : null}
      </Stack>
    </Paper>
  )
}

function ConsumableCard({
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
        {renderItemArtwork(item)}
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

function MarketCard({
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
  const sellerImageSrc = resolveCharacterAssetPath(listing.sellerImagePath)
  const sellerRoomPath = listing.sellerId ? `/players/${listing.sellerId}/visit` : null

  return (
    <Paper
      variant="outlined"
      sx={{
        ...listCardSx,
        minHeight: '100%',
      }}
    >
      <Stack spacing={1}>
        {renderItemArtwork(listing, listing.sellerImagePath)}
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
        <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap justifyContent="center">
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

export default function Items() {
  const { session, isLoading } = useAuth()
  const { mutate: mutateCache } = useSWRConfig()
  const [primaryTab, setPrimaryTab] = useState<PrimaryTab>('items')
  const [marketTab, setMarketTab] = useState<MarketTab>('public')
  const [marketPage, setMarketPage] = useState(1)
  const [myMarketPage, setMyMarketPage] = useState(1)
  const [marketSearch, setMarketSearch] = useState('')
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)
  const [busyKey, setBusyKey] = useState<string | null>(null)
  const [quantities, setQuantities] = useState<Record<string, string>>({})
  const [unitPrices, setUnitPrices] = useState<Record<string, string>>({})

  const itemsSWRKey = session?.user.id ? ([`items`, session.user.id] as const) : null
  const {
    data: items,
    error: itemsError,
    isLoading: isItemsLoading,
    mutate: mutateItems,
  } = useSWR(itemsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    try {
      return await getItems(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''
      if (!message.includes(locale.playerNotFoundMessage)) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getItems(session.access_token)
    }
  })

  const marketSWRKey = session?.user.id ? ([`market`, session.user.id] as const) : null
  const {
    data: marketListings,
    error: marketError,
    isLoading: isMarketLoading,
    mutate: mutateMarket,
  } = useSWR(marketSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getMarketListings(session.access_token)
  })

  const myListingsSWRKey = session?.user.id ? ([`my-market`, session.user.id] as const) : null
  const {
    data: myListings,
    error: myListingsError,
    isLoading: isMyListingsLoading,
    mutate: mutateMyListings,
  } = useSWR(myListingsSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionInfoMissing)
    }

    return getMyMarketListings(session.access_token)
  })

  const equippedItems = useMemo(() => items?.equippedItems ?? [], [items])
  const normalizedMarketSearch = useMemo(() => normalizeText(marketSearch), [marketSearch])

  const inventoryEquipments = useMemo(() => {
    return (items?.inventoryItems ?? []).filter((item): item is ItemEquipmentView => item.kind === 'equipment')
  }, [items])

  const inventoryConsumables = useMemo(() => {
    return (items?.inventoryItems ?? []).filter((item): item is ItemStackView => item.kind === 'item')
  }, [items])

  const publicMarketListings = useMemo(
    () => (marketListings ?? []).filter((item) => matchesMarketSearch(item, normalizedMarketSearch)),
    [marketListings, normalizedMarketSearch],
  )
  const ownMarketListings = useMemo(
    () => (myListings ?? []).filter((item) => matchesMarketSearch(item, normalizedMarketSearch)),
    [myListings, normalizedMarketSearch],
  )

  const currentMarketListings = marketTab === 'public' ? publicMarketListings : ownMarketListings
  const marketPageCount = Math.max(1, Math.ceil(currentMarketListings.length / PAGE_SIZE))
  const visibleMarketPage = marketTab === 'public' ? marketPage : myMarketPage
  const pagedMarketListings = useMemo(
    () => paginate(currentMarketListings, visibleMarketPage),
    [currentMarketListings, visibleMarketPage],
  )

  useEffect(() => {
    if (marketPage > Math.max(1, Math.ceil(publicMarketListings.length / PAGE_SIZE))) {
      setMarketPage(1)
    }
  }, [marketPage, publicMarketListings.length])

  useEffect(() => {
    if (myMarketPage > Math.max(1, Math.ceil(ownMarketListings.length / PAGE_SIZE))) {
      setMyMarketPage(1)
    }
  }, [myMarketPage, ownMarketListings.length])

  async function refreshAll(): Promise<void> {
    await Promise.all([
      mutateItems(),
      mutateMarket(),
      mutateMyListings(),
      session?.user.id ? mutateCache([`player`, session.user.id]) : Promise.resolve(),
    ])
  }

  function setQuantityValue(key: string, value: string): void {
    setQuantities((current) => ({ ...current, [key]: value }))
  }

  function setUnitPriceValue(key: string, value: string): void {
    setUnitPrices((current) => ({ ...current, [key]: value }))
  }

  async function runAction(actionKey: string, action: () => Promise<{ message: string }>): Promise<void> {
    setBusyKey(actionKey)
    setFeedback(null)
    try {
      const result = await action()
      setFeedback({ type: 'success', message: result.message })
      await refreshAll()
    } catch (error) {
      setFeedback({
        type: 'error',
        message: error instanceof Error ? error.message : locale.actionFailed,
      })
    } finally {
      setBusyKey(null)
    }
  }

  async function handleUseItem(item: ItemStackView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`use:${item.itemStackId}`, () =>
      consumeItem(
        item.itemStackId,
        { quantity: parsePositiveInteger(quantities[item.itemStackId], 1) },
        session.access_token,
      ),
    )
  }

  async function handleDeleteItem(item: ItemStackView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`delete-item:${item.itemStackId}`, () =>
      deleteItemStack(
        item.itemStackId,
        { quantity: parsePositiveInteger(quantities[`delete:${item.itemStackId}`] ?? quantities[item.itemStackId], 1) },
        session.access_token,
      ),
    )
  }

  async function handleDeleteEquipment(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`delete-equipment:${item.playerEquipmentId}`, () =>
      deleteEquipment(item.playerEquipmentId, session.access_token),
    )
  }

  async function handleSynthesize(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`synthesize:${item.playerEquipmentId}`, () =>
      synthesizeEquipment(item.playerEquipmentId, session.access_token),
    )
  }

  async function handleEquip(item: ItemEquipmentView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`equip:${item.playerEquipmentId}`, () =>
      updatePlayerEquipment(
        {
          equipmentType: item.equipmentType,
          playerEquipmentId: item.playerEquipmentId,
        },
        session.access_token,
      ),
    )
  }

  async function handleListInventoryItem(item: InventoryItemView): Promise<void> {
    if (!session?.access_token) return

    const key = item.kind === 'item' ? item.itemStackId : item.playerEquipmentId
    await runAction(`list:${key}`, () =>
      createMarketListing(
        item.kind === 'item'
          ? {
              itemStackId: item.itemStackId,
              playerEquipmentId: null,
              quantity: parsePositiveInteger(quantities[key], 1),
              unitPrice: parsePositiveInteger(unitPrices[`price:${key}`], 10),
            }
          : {
              playerEquipmentId: item.playerEquipmentId,
              itemStackId: null,
              quantity: 1,
              unitPrice: parsePositiveInteger(unitPrices[`price:${key}`], 10),
            },
        session.access_token,
      ),
    )
  }

  async function handlePurchase(listing: MarketListingView): Promise<void> {
    if (!session?.access_token) return

    await runAction(`purchase:${listing.listingId}`, () =>
      purchaseMarketListing(
        listing.listingId,
        { quantity: parsePositiveInteger(quantities[`purchase:${listing.listingId}`], 1) },
        session.access_token,
      ),
    )
  }

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <CircularProgress size={20} />
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 6 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={2.5}>
          <PageFrame>
            <ControlFrame>
              <Stack spacing={2}>
                <Stack direction="row" alignItems="center" spacing={1.5}>
                  <HomeNavIconButton ariaLabel={locale.backToHome} />
                  <Box>
                    <Typography variant="overline" sx={{ letterSpacing: '0.16em', color: 'rgba(255,255,255,0.66)' }}>
                      ITEM CONTROL
                    </Typography>
                    <Typography variant="h4" fontWeight={900} color="#ffffff">
                      {locale.title}
                    </Typography>
                  </Box>
                </Stack>
              </Stack>
            </ControlFrame>

            {feedback ? <Alert severity={feedback.type}>{feedback.message}</Alert> : null}
            {itemsError ? <Alert severity="warning">{itemsError.message}</Alert> : null}
            {marketError ? <Alert severity="warning">{marketError.message}</Alert> : null}
            {myListingsError ? <Alert severity="warning">{myListingsError.message}</Alert> : null}

            <ControlFrame>
              <Stack spacing={2}>
                <ItemSummary usedSlots={items?.usedSlots} capacity={items?.capacity} gold={items?.gold} />

                <Paper variant="outlined" sx={tabsPaperSx}>
                  <Tabs
                    value={primaryTab}
                    onChange={(_, value: PrimaryTab) => setPrimaryTab(value)}
                    variant="fullWidth"
                    sx={tabsSx}
                  >
                    <Tab value="items" label={locale.itemsTab} />
                    <Tab value="market" label={locale.marketTab} />
                  </Tabs>
                </Paper>
              </Stack>
            </ControlFrame>

            {primaryTab === 'items' ? (
              <Stack spacing={2}>
                <SectionFrame title={locale.equippedTitle}>
                  <Stack spacing={2}>
                    {isItemsLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : equippedItems.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
                          gap: 1.5,
                        }}
                      >
                        {equippedItems.map((item) => (
                          <EquipmentCard
                            key={item.playerEquipmentId}
                            item={item}
                            quantityValue="1"
                            unitPriceValue="10"
                            onQuantityChange={() => undefined}
                            onUnitPriceChange={() => undefined}
                            onSynthesize={() => undefined}
                            onDelete={() => undefined}
                            onList={() => undefined}
                            actionDisabled
                            showActions={false}
                          />
                        ))}
                      </Box>
                    ) : (
                      <Typography variant="body2">{locale.emptyEquipped}</Typography>
                    )}
                  </Stack>
                </SectionFrame>

                <SectionFrame title={locale.inventoryEquipmentsTitle}>
                  <Stack spacing={2}>
                    {isItemsLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : inventoryEquipments.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: 'repeat(2, minmax(0, 1fr))',
                            sm: 'repeat(3, minmax(0, 1fr))',
                            xl: 'repeat(4, minmax(0, 1fr))',
                          },
                          gap: 1.25,
                        }}
                      >
                        {inventoryEquipments.map((item) => {
                          const key = item.playerEquipmentId
                          return (
                          <EquipmentCard
                            key={key}
                            item={item}
                            quantityValue={quantities[key] ?? '1'}
                            unitPriceValue={unitPrices[`price:${key}`] ?? '10'}
                            onQuantityChange={(value) => setQuantityValue(key, value)}
                            onUnitPriceChange={(value) => setUnitPriceValue(`price:${key}`, value)}
                            onSynthesize={() => void handleSynthesize(item)}
                            onDelete={() => void handleDeleteEquipment(item)}
                            onList={() => void handleListInventoryItem(item)}
                            onEquip={() => void handleEquip(item)}
                            actionDisabled={busyKey !== null}
                          />
                          )
                        })}
                      </Box>
                    ) : (
                      <Typography variant="body2">{locale.emptyInventory}</Typography>
                    )}
                  </Stack>
                </SectionFrame>

                <SectionFrame title={locale.inventoryConsumablesTitle}>
                  <Stack spacing={2}>
                    {isItemsLoading ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.loading}</Typography>
                      </Stack>
                    ) : inventoryConsumables.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: 'repeat(2, minmax(0, 1fr))',
                            sm: 'repeat(3, minmax(0, 1fr))',
                            xl: 'repeat(4, minmax(0, 1fr))',
                          },
                          gap: 1.25,
                        }}
                      >
                        {inventoryConsumables.map((item) => {
                          const key = item.itemStackId
                          return (
                            <ConsumableCard
                              key={key}
                              item={item}
                              quantityValue={quantities[key] ?? '1'}
                              unitPriceValue={unitPrices[`price:${key}`] ?? '10'}
                              onQuantityChange={(value) => setQuantityValue(key, value)}
                              onUnitPriceChange={(value) => setUnitPriceValue(`price:${key}`, value)}
                              onUse={() => void handleUseItem(item)}
                              onDelete={() => void handleDeleteItem(item)}
                              onList={() => void handleListInventoryItem(item)}
                              actionDisabled={busyKey !== null}
                            />
                          )
                        })}
                      </Box>
                    ) : (
                      <Typography variant="body2">{locale.emptyInventory}</Typography>
                    )}
                  </Stack>
                </SectionFrame>
              </Stack>
            ) : (
              <Stack spacing={2}>
                <ControlFrame>
                  <Stack spacing={2}>
                    <Paper variant="outlined" sx={{ ...itemInnerPanelSx, p: 2, borderRadius: 3 }}>
                      <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                        <TextField
                          label={locale.searchLabel}
                          placeholder={locale.searchPlaceholderMarket}
                          value={marketSearch}
                          onChange={(event) => {
                            setMarketSearch(event.target.value)
                            setMarketPage(1)
                            setMyMarketPage(1)
                          }}
                          fullWidth
                          sx={inputSx}
                        />
                      </Stack>
                    </Paper>

                    <Paper variant="outlined" sx={tabsPaperSx}>
                      <Tabs
                        value={marketTab}
                        onChange={(_, value: MarketTab) => {
                          setMarketTab(value)
                        }}
                        variant="fullWidth"
                        sx={tabsSx}
                      >
                        <Tab value="public" label={locale.publicMarketTab} />
                        <Tab value="mine" label={locale.myListingsTab} />
                      </Tabs>
                    </Paper>
                  </Stack>
                </ControlFrame>

                <SectionFrame title={marketTab === 'public' ? locale.marketTitle : locale.myListingsTitle}>
                  <Stack spacing={2}>
                    {(marketTab === 'public' ? isMarketLoading : isMyListingsLoading) ? (
                      <Stack direction="row" spacing={1} alignItems="center">
                        <CircularProgress size={16} />
                        <Typography variant="body2">{locale.marketLoading}</Typography>
                      </Stack>
                    ) : pagedMarketListings.length > 0 ? (
                      <Box
                        sx={{
                          display: 'grid',
                          gridTemplateColumns: {
                            xs: 'repeat(2, minmax(0, 1fr))',
                            sm: 'repeat(3, minmax(0, 1fr))',
                            xl: 'repeat(4, minmax(0, 1fr))',
                          },
                          gap: 1.25,
                        }}
                      >
                        {pagedMarketListings.map((listing) => (
                          <MarketCard
                            key={listing.listingId}
                            listing={listing}
                            quantityValue={quantities[`purchase:${listing.listingId}`] ?? '1'}
                            onQuantityChange={(value) => setQuantityValue(`purchase:${listing.listingId}`, value)}
                            onPurchase={marketTab === 'public' ? () => void handlePurchase(listing) : undefined}
                            actionDisabled={busyKey !== null}
                            showSeller={marketTab === 'public'}
                          />
                        ))}
                      </Box>
                    ) : (
                      <Typography variant="body2">
                        {normalizedMarketSearch ? locale.noSearchResults : locale.emptyMarket}
                      </Typography>
                    )}

                    {currentMarketListings.length > PAGE_SIZE ? (
                      <Stack alignItems="center">
                        <Pagination
                          page={visibleMarketPage}
                          count={marketPageCount}
                          color="primary"
                          onChange={(_, nextPage) => {
                            if (marketTab === 'public') {
                              setMarketPage(nextPage)
                            } else {
                              setMyMarketPage(nextPage)
                            }
                          }}
                        />
                      </Stack>
                    ) : null}
                  </Stack>
                </SectionFrame>
              </Stack>
            )}
          </PageFrame>
        </Stack>
      </Paper>
    </Container>
  )
}
