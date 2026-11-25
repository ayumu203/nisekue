import { Box, ButtonBase, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { itemsPanelColors as colors } from '@/constants/styles'
import { ItemIcon } from './ItemIllustrations'
import { getInventoryKey } from './itemsLogic'
import { ActionButton, BagFrame, CoinIcon, NumberField, Operator, TagCard } from './ItemsParts'
import {
  formatEffectAmount,
  formatGold,
  formatPlusValue,
  formatStatusBonus,
  getEffectTypeLabel,
  getEquipmentBonus,
  type StatusChange,
} from './itemUtils'
import locale from '../../../locale/items/Items.json'
import type { InventoryItemView, ItemEquipmentView } from '@/schema/item'

export type BagFilter = 'all' | 'equipment' | 'item'

function FilterChip({ label, selected, onClick }: { label: string; selected: boolean; onClick: () => void }) {
  return (
    <ButtonBase
      onClick={onClick}
      aria-pressed={selected}
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

const slotHeight = { xs: 108, sm: 132 }

// 袋の 1 マス。上に名前、真ん中に絵、左上に強化値、右下に個数
function BagSlot({ item, selected, onSelect }: { item: InventoryItemView; selected: boolean; onSelect: () => void }) {
  const plus = item.kind === 'equipment' ? formatPlusValue(item.plusValue) : ''
  return (
    <ButtonBase
      onClick={onSelect}
      aria-pressed={selected}
      disableRipple
      sx={{
        height: slotHeight,
        minWidth: 0,
        display: 'flex',
        flexDirection: 'column',
        alignItems: 'stretch',
        gap: 0.25,
        pt: 1,
        px: 0.5,
        borderRadius: '12px',
        backgroundColor: colors.slot,
        border: selected ? `3px solid ${colors.brass}` : `2px solid ${colors.slotLine}`,
        boxShadow: selected ? `0 0 0 4px ${colors.brassSoft}` : 'none',
        fontFamily: 'inherit',
      }}
    >
      <Box
        component="span"
        sx={{
          fontSize: '0.75rem',
          fontWeight: 800,
          color: colors.text,
          textAlign: 'center',
          whiteSpace: 'nowrap',
          overflow: 'hidden',
          textOverflow: 'ellipsis',
          letterSpacing: '-0.02em',
        }}
      >
        {item.name}
      </Box>
      <Box component="span" sx={{ flex: 1, position: 'relative', display: 'grid', placeItems: 'center' }}>
        <ItemIcon item={item} size={52} />
        {plus ? (
          <Box
            component="span"
            sx={{
              position: 'absolute',
              left: 2,
              top: 2,
              px: 0.75,
              borderRadius: '6px',
              backgroundColor: colors.leatherDark,
              color: '#ffffff',
              fontSize: '0.8125rem',
              fontWeight: 800,
            }}
          >
            {plus}
          </Box>
        ) : null}
        {item.kind === 'item' ? (
          <Box
            component="span"
            sx={{
              position: 'absolute',
              right: 4,
              bottom: 4,
              fontSize: '0.9375rem',
              fontWeight: 800,
              color: colors.text,
            }}
          >
            ×{item.quantity}
          </Box>
        ) : null}
      </Box>
    </ButtonBase>
  )
}

export function BagPanel({
  items,
  emptySlotCount,
  filter,
  onFilterChange,
  selectedKey,
  onSelect,
  usedSlots,
  capacity,
  gold,
  isLoading,
  fill,
}: {
  items: InventoryItemView[]
  emptySlotCount: number
  filter: BagFilter
  onFilterChange: (filter: BagFilter) => void
  selectedKey: string | null
  onSelect: (item: InventoryItemView) => void
  usedSlots?: number
  capacity?: number
  gold?: number
  isLoading: boolean
  fill: boolean
}) {
  return (
    <BagFrame fill={fill}>
      <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 1.25 }}>
        <FilterChip label={locale.filterAll} selected={filter === 'all'} onClick={() => onFilterChange('all')} />
        <FilterChip
          label={locale.filterEquipment}
          selected={filter === 'equipment'}
          onClick={() => onFilterChange('equipment')}
        />
        <FilterChip label={locale.filterItem} selected={filter === 'item'} onClick={() => onFilterChange('item')} />
      </Box>
      <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto', p: '4px', m: '-4px' }}>
        {isLoading ? (
          <Typography sx={{ color: colors.label }}>{locale.loading}</Typography>
        ) : (
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: 'repeat(4, minmax(0, 1fr))',
                sm: 'repeat(6, minmax(0, 1fr))',
                lg: 'repeat(8, minmax(0, 1fr))',
                xl: 'repeat(10, minmax(0, 1fr))',
              },
              gap: { xs: 0.75, sm: 1.25 },
            }}
          >
            {items.map((item) => {
              const key = getInventoryKey(item)
              return <BagSlot key={key} item={item} selected={key === selectedKey} onSelect={() => onSelect(item)} />
            })}
            {Array.from({ length: emptySlotCount }, (_, index) => (
              <Box
                key={`empty-${index}`}
                aria-hidden="true"
                sx={{ height: slotHeight, border: `2px dashed ${colors.stitch}`, borderRadius: '12px' }}
              />
            ))}
          </Box>
        )}
      </Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'space-between',
          flexWrap: 'wrap',
          gap: 1.5,
          pt: 1.75,
          borderTop: `2px dashed ${colors.stitch}`,
        }}
      >
        <Typography component="span" sx={{ fontSize: '0.9375rem', color: colors.label }}>
          {locale.slotsLabel}{' '}
          <Box component="b" sx={{ fontSize: '1.375rem', color: colors.text }}>
            {usedSlots ?? '-'}
          </Box>{' '}
          / {capacity ?? '-'}
        </Typography>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.25,
            px: 2.25,
            py: 1,
            borderRadius: 999,
            backgroundColor: colors.leatherDark,
            color: '#ffe7a8',
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          <CoinIcon sx={{ fontSize: 22 }} />
          <Box component="b" sx={{ fontSize: '1.25rem' }}>
            {gold != null ? formatGold(gold) : '-'}
          </Box>
        </Box>
      </Box>
    </BagFrame>
  )
}

function EquippedSlot({
  label,
  item,
  selected,
  onSelect,
}: {
  label: string
  item?: ItemEquipmentView
  selected: boolean
  onSelect: () => void
}) {
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 0.75, width: { xs: 96, sm: 150 } }}>
      <Typography component="span" sx={{ fontSize: '0.875rem', fontWeight: 800, color: colors.label }}>
        {label}
      </Typography>
      <ButtonBase
        onClick={onSelect}
        disabled={!item}
        aria-pressed={selected}
        aria-label={item ? `${label} ${item.name}` : label}
        disableRipple
        sx={{
          width: { xs: 84, sm: 112 },
          height: { xs: 84, sm: 112 },
          display: 'grid',
          placeItems: 'center',
          backgroundColor: '#ffffff',
          borderRadius: '16px',
          border: selected
            ? `3px solid ${colors.brass}`
            : item
              ? `3px solid ${colors.slotLine}`
              : `2px dashed ${colors.stitch}`,
          boxShadow: selected ? `0 0 0 4px ${colors.brassSoft}` : 'none',
        }}
      >
        {item ? <ItemIcon item={item} size={68} /> : null}
      </ButtonBase>
      <Typography
        component="span"
        sx={{ fontSize: '0.9375rem', fontWeight: 800, color: colors.text, textAlign: 'center', minHeight: '1.5em' }}
      >
        {item ? `${item.name} ${formatPlusValue(item.plusValue)}`.trim() : locale.emptyEquipped}
      </Typography>
    </Box>
  )
}

// キャラの両脇に武器・防具のマスを置いた姿見。袋の装備を選ぶと、持ち替えたときの能力の変化を出す
export function MirrorPanel({
  weapon,
  armor,
  playerImageSrc,
  selectedKey,
  onSelect,
  changes,
}: {
  weapon?: ItemEquipmentView
  armor?: ItemEquipmentView
  playerImageSrc: string | null
  selectedKey: string | null
  onSelect: (item: ItemEquipmentView) => void
  changes: StatusChange[] | null
}) {
  return (
    <TagCard>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'flex-end',
          justifyContent: 'center',
          gap: 1,
          px: 2.5,
          pt: 2.25,
          pb: 2,
        }}
      >
        <EquippedSlot
          label={locale.weaponSlot}
          item={weapon}
          selected={weapon?.playerEquipmentId === selectedKey}
          onSelect={() => weapon && onSelect(weapon)}
        />
        <Box
          sx={{ width: { xs: 110, sm: 190 }, height: { xs: 140, sm: 210 }, display: 'flex', alignItems: 'flex-end' }}
        >
          {playerImageSrc ? (
            <Box
              component="img"
              src={playerImageSrc}
              alt={locale.playerImageAlt}
              sx={{ width: '100%', height: '100%', objectFit: 'contain', objectPosition: 'center bottom' }}
            />
          ) : null}
        </Box>
        <EquippedSlot
          label={locale.armorSlot}
          item={armor}
          selected={armor?.playerEquipmentId === selectedKey}
          onSelect={() => armor && onSelect(armor)}
        />
      </Box>
      {changes && changes.length > 0 ? (
        <Box
          sx={{
            mx: 2.75,
            mb: 2.25,
            px: 2,
            py: 1.5,
            borderRadius: '12px',
            backgroundColor: colors.brassSoft,
            display: 'flex',
            flexWrap: 'wrap',
            columnGap: 3,
            rowGap: 0.75,
          }}
        >
          {changes.map((change) => {
            const diff = change.to - change.from
            return (
              <Box key={change.label} component="span" sx={{ fontSize: '1.125rem', color: colors.text }}>
                {change.label} <b>{change.from}</b> → <b>{change.to}</b>{' '}
                <Box component="b" sx={{ fontSize: '1rem', color: diff > 0 ? colors.up : colors.down }}>
                  {diff > 0 ? `▲ ${diff}` : `▼ ${-diff}`}
                </Box>
              </Box>
            )
          })}
        </Box>
      ) : null}
    </TagCard>
  )
}

function DetailRow({ label, value, first }: { label: string; value: ReactNode; first: boolean }) {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'space-between',
        gap: 2,
        py: 1,
        borderTop: first ? 'none' : `1px dashed ${colors.stitch}`,
        fontSize: '1rem',
      }}
    >
      <Box component="span" sx={{ color: colors.label }}>
        {label}
      </Box>
      <Box component="b" sx={{ color: colors.text, textAlign: 'right' }}>
        {value}
      </Box>
    </Box>
  )
}

export type ListingDraft = { unitPrice: string; quantity: string }

// 選んだ品の札。品物の情報と、装備・使用・出品・廃棄の操作。出品の入力もこの中で開く
export function ItemDetailPanel({
  item,
  isEquipped,
  isListing,
  listingDraft,
  onListingDraftChange,
  onEquip,
  onUse,
  onStartListing,
  onCancelListing,
  onList,
  onDelete,
  busy,
  fill,
}: {
  item: InventoryItemView | null
  isEquipped: boolean
  isListing: boolean
  listingDraft: ListingDraft
  onListingDraftChange: (draft: ListingDraft) => void
  onEquip: () => void
  onUse: () => void
  onStartListing: () => void
  onCancelListing: () => void
  onList: () => void
  onDelete: () => void
  busy: boolean
  fill: boolean
}) {
  if (!item) {
    return (
      <TagCard fill={fill}>
        <Typography sx={{ p: 3, color: colors.label }}>{locale.emptyInventory}</Typography>
      </TagCard>
    )
  }

  const title =
    item.kind === 'equipment'
      ? `${item.name} ${formatPlusValue(item.plusValue)}`.trim()
      : `${item.name} × ${item.quantity}`

  const rows: Array<[string, ReactNode]> =
    item.kind === 'equipment'
      ? [
          [locale.kindLabel, item.equipmentType === 'Weapon' ? locale.categoryWeapon : locale.categoryArmor],
          [locale.effectLabel, formatStatusBonus({ statusBonus: getEquipmentBonus(item) })],
          [locale.plusValueLabel, item.plusValue > 0 ? `+${item.plusValue}` : '0'],
        ]
      : [
          [locale.kindLabel, getEffectTypeLabel(item.effectType)],
          [locale.effectLabel, formatEffectAmount(item)],
          ...(item.requiredLevel !== null
            ? ([[locale.requiredLevel, item.requiredLevel]] as Array<[string, ReactNode]>)
            : []),
          ...(item.changeJobTo ? ([[locale.changeJobTo, item.changeJobTo]] as Array<[string, ReactNode]>) : []),
        ]

  const unitPrice = Number.parseInt(listingDraft.unitPrice, 10) || 0
  const quantity = item.kind === 'equipment' ? 1 : Number.parseInt(listingDraft.quantity, 10) || 0

  let actions: ReactNode = null
  if (isListing) {
    actions = (
      <>
        <Box
          sx={{
            borderRadius: '12px',
            backgroundColor: colors.brassSoft,
            p: 2,
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            flexWrap: 'wrap',
            gap: 1.25,
          }}
        >
          <NumberField
            label={locale.unitPrice}
            unit="G"
            value={listingDraft.unitPrice}
            onChange={(value) => onListingDraftChange({ ...listingDraft, unitPrice: value })}
          />
          <Operator>×</Operator>
          <NumberField
            label={locale.quantity}
            unit={locale.quantityUnit}
            value={item.kind === 'equipment' ? '1' : listingDraft.quantity}
            onChange={(value) => onListingDraftChange({ ...listingDraft, quantity: value })}
            disabled={item.kind === 'equipment'}
            width={80}
          />
          <Operator>=</Operator>
          <Box component="span" sx={{ fontSize: '1.625rem', fontWeight: 800, color: colors.text }}>
            {formatGold(unitPrice * quantity)}
          </Box>
        </Box>
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.25 }}>
          <ActionButton kind="primary" onClick={onList} disabled={busy || unitPrice < 1 || quantity < 1}>
            {locale.actionList}
          </ActionButton>
          <ActionButton kind="secondary" onClick={onCancelListing} disabled={busy}>
            {locale.actionCancel}
          </ActionButton>
        </Box>
      </>
    )
  } else if (!isEquipped) {
    actions = (
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 1.25 }}>
        {item.kind === 'equipment' ? (
          <ActionButton kind="primary" onClick={onEquip} disabled={busy || item.status === 'Broken'}>
            {locale.actionEquip}
          </ActionButton>
        ) : (
          <ActionButton kind="primary" onClick={onUse} disabled={busy}>
            {locale.actionUse}
          </ActionButton>
        )}
        <ActionButton kind="secondary" onClick={onStartListing} disabled={busy}>
          {locale.actionList}
        </ActionButton>
        <ActionButton kind="danger" onClick={onDelete} disabled={busy}>
          {locale.actionDelete}
        </ActionButton>
      </Box>
    )
  }

  return (
    <TagCard fill={fill}>
      <Box sx={{ p: { xs: 2, sm: 2.75 }, display: 'flex', flexDirection: 'column', gap: 1.75 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Box
            sx={{
              width: 88,
              height: 88,
              flex: 'none',
              display: 'grid',
              placeItems: 'center',
              backgroundColor: '#ffffff',
              border: `3px solid ${colors.slotLine}`,
              borderRadius: '16px',
            }}
          >
            <ItemIcon item={item} size={64} />
          </Box>
          <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
            <Typography component="h2" sx={{ m: 0, fontSize: '1.5rem', fontWeight: 800, color: colors.text }}>
              {title}
            </Typography>
            {item.flavorText ? (
              <Typography sx={{ fontSize: '0.9375rem', color: colors.label }}>{item.flavorText}</Typography>
            ) : null}
          </Box>
        </Box>
        <Box>
          {rows.map(([label, value], index) => (
            <DetailRow key={label} label={label} value={value} first={index === 0} />
          ))}
        </Box>
        {actions}
      </Box>
    </TagCard>
  )
}
