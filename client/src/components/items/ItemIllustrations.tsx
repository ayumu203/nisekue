import { SvgIcon, type SvgIconProps } from '@mui/material'
import type { InventoryItemView, MarketListingView } from '@/schema/item'

export function WeaponIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M78 12 91 25 48 68l-8 24-11 11-5-5 11-11 24-8z" fill="#f1e3b2" />
      <path d="m74 16 14 14 7-7-14-14z" fill="#d2b36f" />
      <path d="m39 73 8 8-4 12-16 5 5-16z" fill="#855e32" />
    </SvgIcon>
  )
}

export function ArmorIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M38 18h44l8 15-8 17v42L60 104 38 92V50l-8-17z" fill="#d7e4ef" />
      <path d="M46 26h28v18H46z" fill="#86a4bb" />
      <path d="M38 50h44v12H38z" fill="#6c879b" />
    </SvgIcon>
  )
}

export function StatBoostIllustration(props: SvgIconProps) {
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

export function JobChangeIllustration(props: SvgIconProps) {
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

export function ExpMultiplierIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <circle cx="60" cy="60" r="28" fill="#5b4e8c" />
      <path
        d="M60 20c20 0 36 16 36 36 0 18-14 39-36 48-22-9-36-30-36-48 0-20 16-36 36-36"
        fill="none"
        stroke="#e8d17a"
        strokeWidth="8"
      />
      <text x="60" y="62" textAnchor="middle" fontSize="28" fontWeight="bold" fill="#fff5d9">
        ×
      </text>
    </SvgIcon>
  )
}

type IconSource = InventoryItemView | MarketListingView

type IconKind = 'Weapon' | 'Armor' | 'StatBoost' | 'ExpMultiplier' | 'ChangeJob'

function resolveIconKind(item: IconSource): IconKind {
  if ('kind' in item) {
    if (item.kind === 'equipment') {
      return item.equipmentType
    }
    if (item.effectType === 'StatBoost' || item.effectType === 'ExpMultiplier') {
      return item.effectType
    }
    return 'ChangeJob'
  }

  // マーケットの出品は、装備なら種別、アイテムは名前から絵を選ぶ（出品に効果の種別が含まれないため）
  if (item.equipmentDetail) {
    return item.equipmentDetail.equipmentType
  }
  if (item.itemName.includes('証')) return 'ChangeJob'
  if (item.itemName.includes('経験')) return 'ExpMultiplier'
  return 'StatBoost'
}

// 品物の絵（枠なし）。マス・札・出品の行で使う
export function ItemIcon({ item, size }: { item: IconSource; size: number }) {
  const sx = { fontSize: size }
  switch (resolveIconKind(item)) {
    case 'Weapon':
      return <WeaponIllustration sx={sx} />
    case 'Armor':
      return <ArmorIllustration sx={sx} />
    case 'ExpMultiplier':
      return <ExpMultiplierIllustration sx={sx} />
    case 'ChangeJob':
      return <JobChangeIllustration sx={sx} />
    default:
      return <StatBoostIllustration sx={sx} />
  }
}
