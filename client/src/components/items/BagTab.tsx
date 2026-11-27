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
import { font, palette } from '@/theme/tokens'

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
          fontSize: font.xs,
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
              color: palette.white,
              fontSize: font.sm,
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
              fontSize: font.md,
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
      <Box sx={{ flex: 1, minHeight: 0, overflow: 'auto', p: 0.5, m: -0.5 }}>
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
              gap: { xs: 0.75, sm: 1.5 },
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
          pt: 2,
          borderTop: `2px dashed ${colors.stitch}`,
        }}
      >
        <Typography component="span" sx={{ fontSize: font.md, color: colors.label }}>
          {locale.slotsLabel}{' '}
          <Box component="b" sx={{ fontSize: font.xxl, color: colors.text }}>
            {usedSlots ?? '-'}
          </Box>{' '}
          / {capacity ?? '-'}
        </Typography>
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            gap: 1.5,
            px: 2.5,
            py: 1,
            borderRadius: 999,
            backgroundColor: colors.leatherDark,
            color: palette.gold300,
            fontVariantNumeric: 'tabular-nums',
          }}
        >
          <CoinIcon sx={{ fontSize: font.xxl }} />
          <Box component="b" sx={{ fontSize: font.xl }}>
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
      <Typography component="span" sx={{ fontSize: font.sm, fontWeight: 800, color: colors.label }}>
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
          backgroundColor: palette.white,
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
        sx={{ fontSize: font.md, fontWeight: 800, color: colors.text, textAlign: 'center', minHeight: '1.5em' }}
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
          pt: 2.5,
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
            mx: { xs: 2, sm: 3 },
            mb: 2.5,
            px: 2,
            py: 1.5,
            borderRadius: '12px',
            backgroundColor: colors.brassSoft,
            display: 'grid',
            // 列をそろえて、どの行も能力名・数字の開始位置が同じになるようにする
            gridTemplateColumns: {
              xs: 'minmax(0, 1fr)',
              sm: 'repeat(2, minmax(0, 1fr))',
              lg: 'repeat(3, minmax(0, 1fr))',
            },
            columnGap: 2,
            rowGap: 0.75,
          }}
        >
          {changes.map((change) => {
            const diff = change.to - change.from
            return (
              <Box
                key={change.label}
                sx={{
                  display: 'grid',
                  gridTemplateColumns: '2.8em 2.4em 1.6em 2.4em auto',
                  alignItems: 'baseline',
                  fontSize: font.lg,
                  color: colors.text,
                  fontVariantNumeric: 'tabular-nums',
                  whiteSpace: 'nowrap',
                }}
              >
                <span>{change.label}</span>
                <b>{change.from}</b>
                <Box component="span" aria-hidden="true">
                  →
                </Box>
                <b>{change.to}</b>
                <Box component="b" sx={{ fontSize: font.md, color: diff > 0 ? colors.up : colors.down }}>
                  {diff > 0 ? `▲${diff}` : `▼${-diff}`}
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
        fontSize: font.md,
      }}
    >
      <Box component="span" sx={{ color: colors.label, flex: 'none', whiteSpace: 'nowrap' }}>
        {label}
      </Box>
      <Box component="b" sx={{ minWidth: 0, color: colors.text, textAlign: 'right' }}>
        {value}
      </Box>
    </Box>
  )
}

// 「MP+4 / STR+12 / DEF+3」を、1 つずつ折り返さない塊にして右寄せで並べる
function EffectList({ text }: { text: string }) {
  return (
    <Box
      component="span"
      sx={{ display: 'flex', flexWrap: 'wrap', justifyContent: 'flex-end', columnGap: 1.5, rowGap: 0.25 }}
    >
      {text.split(' / ').map((part) => (
        <Box key={part} component="span" sx={{ whiteSpace: 'nowrap' }}>
          {part}
        </Box>
      ))}
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
          [
            locale.effectLabel,
            <EffectList key="effect" text={formatStatusBonus({ statusBonus: getEquipmentBonus(item) })} />,
          ],
          [locale.plusValueLabel, item.plusValue > 0 ? `+${item.plusValue}` : '0'],
        ]
      : [
          [locale.kindLabel, getEffectTypeLabel(item.effectType)],
          [locale.effectLabel, <EffectList key="effect" text={formatEffectAmount(item)} />],
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
            gap: 1.5,
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
          <Box component="span" sx={{ fontSize: font.xxxl, fontWeight: 800, color: colors.text }}>
            {formatGold(unitPrice * quantity)}
          </Box>
        </Box>
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.5 }}>
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
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 1.5 }}>
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
      <Box sx={{ p: { xs: 2, sm: 3 }, display: 'flex', flexDirection: 'column', gap: 2 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 2 }}>
          <Box
            sx={{
              width: 88,
              height: 88,
              flex: 'none',
              display: 'grid',
              placeItems: 'center',
              backgroundColor: palette.white,
              border: `3px solid ${colors.slotLine}`,
              borderRadius: '16px',
            }}
          >
            <ItemIcon item={item} size={64} />
          </Box>
          <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
            <Typography component="h2" sx={{ m: 0, fontSize: font.xxl, fontWeight: 800, color: colors.text }}>
              {title}
            </Typography>
            {item.flavorText ? (
              <Typography sx={{ fontSize: font.md, color: colors.label }}>{item.flavorText}</Typography>
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
