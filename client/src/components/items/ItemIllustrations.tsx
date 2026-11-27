import { SvgIcon, type SvgIconProps } from '@mui/material'
import type { InventoryItemView, MarketListingView } from '@/schema/item'

// 絵の色。どの絵も同じ色を使い、少ない図形の平塗りでそろえる
const blade = '#f1e3b2'
const gold = '#d2b36f'
const wood = '#855e32'
const armorLight = '#d7e4ef'
const armorMid = '#86a4bb'
const armorDark = '#6c879b'
const cream = '#fff5d9'

export function WeaponIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M78 12 91 25 48 68l-8 24-11 11-5-5 11-11 24-8z" fill={blade} />
      <path d="m74 16 14 14 7-7-14-14z" fill={gold} />
      <path d="m39 73 8 8-4 12-16 5 5-16z" fill={wood} />
    </SvgIcon>
  )
}

export function GreatswordIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <g transform="rotate(45 60 60)">
        <path d="M45 18 60 4l15 14v60H45z" fill={blade} />
        <path d="M45 18 60 4l15 14z" fill={gold} />
        <path d="M34 78h52v9H34z" fill={gold} />
        <path d="M55 87h10v22H55z" fill={wood} />
      </g>
    </SvgIcon>
  )
}

export function SpearIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <g transform="rotate(45 60 60)">
        <path d="M56 34h8v80h-8z" fill={wood} />
        <path d="m60 2 11 20-11 12-11-12z" fill={blade} />
        <path d="M50 32h20v7H50z" fill={gold} />
      </g>
    </SvgIcon>
  )
}

export function StaffIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <g transform="rotate(35 60 60)">
        <path d="M56 36h8v76h-8z" fill={wood} />
        <circle cx="60" cy="22" r="14" fill="none" stroke={gold} strokeWidth="8" />
        <path d="m60 14 7 8-7 8-7-8z" fill={blade} />
      </g>
    </SvgIcon>
  )
}

export function BowIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M44 10q56 50 0 100" fill="none" stroke={wood} strokeWidth="10" strokeLinecap="round" />
      <path d="M44 12v96" stroke={blade} strokeWidth="3" />
      <path d="M64 52h10v16H64z" fill={gold} />
    </SvgIcon>
  )
}

export function KatanaIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <g transform="rotate(40 60 60)">
        <path d="M55 80c-3-28 0-52 15-76-4 26-5 50-5 76z" fill={blade} />
        <path d="M47 80h26v7H47z" fill={gold} />
        <path d="M55 87h10v24H55z" fill={wood} />
      </g>
    </SvgIcon>
  )
}

export function ArmorIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M38 18h44l8 15-8 17v42L60 104 38 92V50l-8-17z" fill={armorLight} />
      <path d="M46 26h28v18H46z" fill={armorMid} />
      <path d="M38 50h44v12H38z" fill={armorDark} />
    </SvgIcon>
  )
}

export function RobeIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M50 28h20l22 76H28z" fill={armorLight} />
      <path d="M46 16q14-14 28 0l-4 14H50z" fill={armorMid} />
      <path d="M42 60h36l2 9H40z" fill={armorDark} />
    </SvgIcon>
  )
}

export function CloakIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M36 22h48l16 82q-40-10-80 0z" fill={armorMid} />
      <path d="M36 22h48l-6 12H42z" fill={armorDark} />
      <circle cx="60" cy="28" r="5" fill={gold} />
    </SvgIcon>
  )
}

type StatKey = 'maxHp' | 'maxMp' | 'strength' | 'defense' | 'intelligence' | 'luck' | 'speed'

// 上がる能力ごとの円の色
const statColors: Record<StatKey, string> = {
  maxHp: '#d94545',
  maxMp: '#4a78c2',
  strength: '#d9822b',
  defense: '#6c879b',
  intelligence: '#7a5bb0',
  luck: '#4f9a5a',
  speed: '#2a9c98',
}

function Emblem({ color, ring }: { color: string; ring: string }) {
  return (
    <>
      <circle cx="60" cy="60" r="28" fill={color} />
      <path
        d="M60 20c20 0 36 16 36 36 0 18-14 39-36 48-22-9-36-30-36-48 0-20 16-36 36-36"
        fill="none"
        stroke={ring}
        strokeWidth="8"
      />
    </>
  )
}

// 固定値で上がるもの（たね・ふたば・つぼみ・かじつ）は＋、割合で上がるもの（しずく・せいすい・いずみ）は％
export function StatBoostIllustration({
  stat = 'maxHp',
  percent = false,
  ...props
}: SvgIconProps & { stat?: StatKey; percent?: boolean }) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <Emblem color={statColors[stat]} ring="#f7d98a" />
      {percent ? (
        <>
          <circle cx="49" cy="49" r="6" fill={cream} />
          <circle cx="71" cy="71" r="6" fill={cream} />
          <path d="M73 45 47 75" stroke={cream} strokeWidth="7" strokeLinecap="round" />
        </>
      ) : (
        <path d="M56 42h8v14h14v8H64v14h-8V64H42v-8h14z" fill={cream} />
      )}
    </SvgIcon>
  )
}

export function ExpMultiplierIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <Emblem color="#5b4e8c" ring="#e8d17a" />
      <text x="60" y="62" textAnchor="middle" fontSize="28" fontWeight="bold" fill={cream}>
        ×
      </text>
    </SvgIcon>
  )
}

// 転職の証。巻物に封蝋とリボン
export function JobChangeIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M24 30h72v52H24z" fill={blade} />
      <path d="M18 24h12v64H18zM90 24h12v64H90z" fill={gold} />
      <path d="M36 42h48v6H36zM36 54h36v6H36z" fill="#d8c48e" />
      <path d="m70 76-6 30 10-7 10 7-6-30z" fill="#7aa96f" />
      <circle cx="74" cy="74" r="12" fill="#d94545" />
    </SvgIcon>
  )
}

export function TreasureMapIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="m20 30 26-8 28 8 26-8v68l-26 8-28-8-26 8z" fill={blade} />
      <path d="m46 22 28 8v68l-28-8z" fill={gold} />
      <path d="m78 48 12 12m0-12L78 60" stroke="#d94545" strokeWidth="6" strokeLinecap="round" />
    </SvgIcon>
  )
}

// マップを解放するアイテム（招待状・道標）
export function SignpostIllustration(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 120 120">
      <path d="M56 40h8v70h-8z" fill={wood} />
      <path d="M28 20h52l12 13-12 13H28z" fill={gold} />
      <path d="M36 30h34v6H36z" fill={wood} />
    </SvgIcon>
  )
}

type IconSource = InventoryItemView | MarketListingView

type IconKind =
  | { kind: 'Sword' | 'Greatsword' | 'Spear' | 'Staff' | 'Bow' | 'Katana' }
  | { kind: 'Armor' | 'Robe' | 'Cloak' }
  | { kind: 'StatBoost'; stat: StatKey; percent: boolean }
  | { kind: 'ExpMultiplier' | 'ChangeJob' | 'TreasureMap' | 'UnlockMap' }

const statKeys: StatKey[] = ['maxHp', 'maxMp', 'strength', 'defense', 'intelligence', 'luck', 'speed']

// 強化アイテムの名前の頭と、上がる能力
const statPrefixes: [string, StatKey][] = [
  ['いのち', 'maxHp'],
  ['まほう', 'maxMp'],
  ['ちから', 'strength'],
  ['まもり', 'defense'],
  ['かしこさ', 'intelligence'],
  ['しあわせ', 'luck'],
  ['すばやさ', 'speed'],
]

// 装備は種類の列がないので名前で分ける。名前の装備が増えたらここを見直す
function resolveWeapon(name: string): IconKind {
  if (name.includes('大剣') || name.includes('つるぎ')) return { kind: 'Greatsword' }
  if (name.includes('太刀')) return { kind: 'Katana' }
  if (name.includes('槍') || name.includes('やり')) return { kind: 'Spear' }
  if (name.includes('杖')) return { kind: 'Staff' }
  if (name.includes('弓')) return { kind: 'Bow' }
  return { kind: 'Sword' }
}

function resolveArmor(name: string): IconKind {
  if (name.includes('ローブ') || name.includes('ころも') || name.includes('服')) return { kind: 'Robe' }
  if (name.includes('マント')) return { kind: 'Cloak' }
  return { kind: 'Armor' }
}

function resolveEquipment(equipmentType: 'Weapon' | 'Armor', name: string): IconKind {
  return equipmentType === 'Weapon' ? resolveWeapon(name) : resolveArmor(name)
}

// 出品には効果の種類が含まれないので、アイテムは名前で分ける
function resolveItemByName(name: string): IconKind {
  if (name.includes('古地図')) return { kind: 'TreasureMap' }
  if (name.includes('の証')) return { kind: 'ChangeJob' }
  if (name.includes('招待状') || name.includes('道標')) return { kind: 'UnlockMap' }
  if (name.includes('けいけん') || name.includes('経験')) return { kind: 'ExpMultiplier' }
  const stat = statPrefixes.find(([prefix]) => name.startsWith(prefix))?.[1] ?? 'maxHp'
  const percent = ['しずく', 'せいすい', 'いずみ'].some((word) => name.includes(word))
  return { kind: 'StatBoost', stat, percent }
}

function resolveIconKind(item: IconSource): IconKind {
  if (!('kind' in item)) {
    if (item.equipmentDetail) return resolveEquipment(item.equipmentDetail.equipmentType, item.itemName)
    return resolveItemByName(item.itemName)
  }

  if (item.kind === 'equipment') return resolveEquipment(item.equipmentType, item.name)

  switch (item.effectType) {
    case 'ChangeJob':
      return { kind: 'ChangeJob' }
    case 'ExpMultiplier':
      return { kind: 'ExpMultiplier' }
    case 'UnlockMap':
      return { kind: 'UnlockMap' }
    default: {
      // 古地図は効果の種類が StatBoost のまま（宝の地図メニューから使う）なので名前で分ける
      if (item.name.includes('古地図')) return { kind: 'TreasureMap' }
      const percentStat = statKeys.find((key) => (item.statusBonusPercent?.[key] ?? 0) > 0)
      if (percentStat) return { kind: 'StatBoost', stat: percentStat, percent: true }
      const flatStat = statKeys.find((key) => (item.statusBonus?.[key] ?? 0) !== 0)
      if (flatStat) return { kind: 'StatBoost', stat: flatStat, percent: false }
      return resolveItemByName(item.name)
    }
  }
}

// 品物の絵（枠なし）。マス・札・出品の行で使う
export function ItemIcon({ item, size }: { item: IconSource; size: number }) {
  const sx = { fontSize: size }
  const icon = resolveIconKind(item)
  switch (icon.kind) {
    case 'Sword':
      return <WeaponIllustration sx={sx} />
    case 'Greatsword':
      return <GreatswordIllustration sx={sx} />
    case 'Spear':
      return <SpearIllustration sx={sx} />
    case 'Staff':
      return <StaffIllustration sx={sx} />
    case 'Bow':
      return <BowIllustration sx={sx} />
    case 'Katana':
      return <KatanaIllustration sx={sx} />
    case 'Armor':
      return <ArmorIllustration sx={sx} />
    case 'Robe':
      return <RobeIllustration sx={sx} />
    case 'Cloak':
      return <CloakIllustration sx={sx} />
    case 'StatBoost':
      return <StatBoostIllustration sx={sx} stat={icon.stat} percent={icon.percent} />
    case 'ExpMultiplier':
      return <ExpMultiplierIllustration sx={sx} />
    case 'ChangeJob':
      return <JobChangeIllustration sx={sx} />
    case 'TreasureMap':
      return <TreasureMapIllustration sx={sx} />
    case 'UnlockMap':
      return <SignpostIllustration sx={sx} />
  }
}
