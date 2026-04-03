import { Box, SvgIcon, type SvgIconProps } from '@mui/material'
import { resolveCharacterAssetPath } from '@/lib/assets'
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

export function ItemArtwork({
  item,
  sellerImagePath,
}: {
  item: InventoryItemView | MarketListingView
  sellerImagePath?: string | null
}) {
  const sellerImageSrc = resolveCharacterAssetPath(sellerImagePath)
  const marketEquipmentType = 'equipmentDetail' in item ? item.equipmentDetail?.equipmentType : null

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
      ) : marketEquipmentType === 'Weapon' ? (
        <WeaponIllustration sx={{ fontSize: 72, color: '#6d5630' }} />
      ) : marketEquipmentType === 'Armor' ? (
        <ArmorIllustration sx={{ fontSize: 72, color: '#5a7387' }} />
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
