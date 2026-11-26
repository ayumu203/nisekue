import { Box, ButtonBase, Typography } from '@mui/material'
import { itemsPanelColors as colors } from '@/constants/styles'
import { ItemIcon } from './ItemIllustrations'
import { canForge, getForgeTarget, type ForgeGroup } from './itemsLogic'
import { ActionButton, BagFrame, CoinIcon, DoubleFrame, ForgeTabIcon, FrameHeading, GoldChangeRow } from './ItemsParts'
import { formatGold, formatPlusValue, getSynthesisCost } from './itemUtils'
import locale from '../../../locale/items/Items.json'
import type { ItemEquipmentView } from '@/schema/item'

export function ForgeList({
  groups,
  selectedId,
  onSelect,
  isLoading,
  fill,
}: {
  groups: ForgeGroup[]
  selectedId: number | null
  onSelect: (group: ForgeGroup) => void
  isLoading: boolean
  fill: boolean
}) {
  return (
    <BagFrame fill={fill}>
      <Typography component="h2" sx={{ m: 0, fontSize: '1.25rem', fontWeight: 800, color: colors.text }}>
        {locale.forgeListTitle}
      </Typography>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          p: '4px',
          m: '-4px',
          display: 'flex',
          flexDirection: 'column',
          gap: 1.25,
        }}
      >
        {isLoading ? <Typography sx={{ color: colors.label }}>{locale.loading}</Typography> : null}
        {!isLoading && groups.length === 0 ? (
          <Typography sx={{ color: colors.label }}>{locale.emptyForge}</Typography>
        ) : null}
        {groups.map((group) => {
          const ok = canForge(group)
          const selected = group.equipmentId === selectedId
          const target = getForgeTarget(group)
          return (
            <ButtonBase
              key={group.equipmentId}
              onClick={() => onSelect(group)}
              disabled={!ok}
              aria-pressed={selected}
              disableRipple
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'flex-start',
                gap: 2,
                px: 2,
                py: 1.25,
                borderRadius: '14px',
                textAlign: 'left',
                fontFamily: 'inherit',
                backgroundColor: ok ? colors.slot : 'transparent',
                border: selected
                  ? `3px solid ${colors.brass}`
                  : ok
                    ? `2px solid ${colors.slotLine}`
                    : `2px dashed ${colors.stitch}`,
                boxShadow: selected ? `0 0 0 4px ${colors.brassSoft}` : 'none',
                '&.Mui-disabled': { opacity: 0.6 },
              }}
            >
              <ItemIcon item={target} size={56} />
              <Box
                component="span"
                sx={{
                  width: { xs: 'auto', sm: 160 },
                  flex: { xs: 1, sm: 'none' },
                  fontSize: '1.125rem',
                  fontWeight: 800,
                  color: colors.text,
                }}
              >
                {target.name}
              </Box>
              <Box component="span" sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, flex: { sm: 1 } }}>
                {group.copies.map((copy) => (
                  <Box
                    key={copy.playerEquipmentId}
                    component="span"
                    sx={{
                      minWidth: 44,
                      height: 30,
                      px: 1,
                      display: 'grid',
                      placeItems: 'center',
                      borderRadius: '8px',
                      backgroundColor: ok ? colors.leatherDark : '#d9cbb5',
                      color: '#ffffff',
                      fontSize: '0.875rem',
                      fontWeight: 800,
                    }}
                  >
                    +{copy.plusValue}
                  </Box>
                ))}
              </Box>
              <Box
                component="span"
                sx={{
                  display: { xs: 'none', sm: 'block' },
                  width: 200,
                  textAlign: 'right',
                  fontSize: '0.9375rem',
                  color: colors.label,
                }}
              >
                {ok ? (
                  <>
                    <Box component="b" sx={{ color: colors.text }}>
                      {formatGold(getSynthesisCost(target))}
                    </Box>{' '}
                    / {locale.perTime}
                  </>
                ) : null}
              </Box>
            </ButtonBase>
          )
        })}
      </Box>
    </BagFrame>
  )
}

function ForgeSlot({ label, item, after = false }: { label: string; item?: ItemEquipmentView; after?: boolean }) {
  const plusValue = item ? item.plusValue + (after ? 1 : 0) : 0
  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: 1, minWidth: 0 }}>
      <Typography
        component="span"
        sx={{ fontSize: '0.875rem', fontWeight: 800, color: after ? colors.forgeEmberText : colors.forgeLabel }}
      >
        {label}
      </Typography>
      <Box
        sx={{
          width: { xs: 84, sm: 124 },
          height: { xs: 84, sm: 124 },
          display: 'grid',
          placeItems: 'center',
          backgroundColor: colors.forgeSlot,
          borderRadius: '16px',
          border: item
            ? `3px solid ${after ? colors.forgeEmber : colors.forgeSlotLine}`
            : `2px dashed ${colors.forgeSlotLine}`,
        }}
      >
        {item ? <ItemIcon item={item} size={84} /> : null}
      </Box>
      <Typography
        component="span"
        sx={{
          fontSize: { xs: '0.875rem', sm: '1.0625rem' },
          fontWeight: 800,
          color: '#ffffff',
          textAlign: 'center',
          minHeight: '1.5em',
        }}
      >
        {item ? `${item.name} ${formatPlusValue(plusValue) || '+0'}` : ''}
      </Typography>
    </Box>
  )
}

// 鍛冶場：合成先＋素材 → 合成後、強化値の変化、1 回の費用と所持金の増減
export function ForgePanel({
  target,
  material,
  gold,
  canChangeMaterial,
  onChangeMaterial,
  onSynthesize,
  busy,
  fill,
}: {
  target?: ItemEquipmentView
  material?: ItemEquipmentView
  gold?: number
  canChangeMaterial: boolean
  onChangeMaterial: () => void
  onSynthesize: () => void
  busy: boolean
  fill: boolean
}) {
  const cost = target ? getSynthesisCost(target) : null
  const goldAfter = cost != null && gold != null ? gold - cost : null
  const canPay = goldAfter != null && goldAfter >= 0

  return (
    <DoubleFrame dark fill={fill}>
      <FrameHeading dark icon={<ForgeTabIcon />} title={locale.forgeTitle} />
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: { xs: 0.75, sm: 1.75 } }}>
        <ForgeSlot label={locale.forgeTarget} item={target} />
        <Box
          component="span"
          aria-hidden="true"
          sx={{ fontSize: '1.875rem', fontWeight: 800, color: colors.forgeLabel }}
        >
          ＋
        </Box>
        <ForgeSlot label={locale.forgeMaterial} item={material} />
        <Box
          component="span"
          aria-hidden="true"
          sx={{ fontSize: '1.875rem', fontWeight: 800, color: colors.forgeEmber }}
        >
          →
        </Box>
        <ForgeSlot label={locale.forgeAfter} item={target} after />
      </Box>
      <Box
        sx={{
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 2.25,
          py: 1.75,
          borderTop: `2px solid ${colors.forgeLine}`,
          borderBottom: `2px solid ${colors.forgeLine}`,
        }}
      >
        <Box component="span" sx={{ fontSize: '0.9375rem', fontWeight: 800, color: colors.forgeLabel }}>
          {locale.plusValueLabel}
        </Box>
        <Box component="span" sx={{ fontSize: '2.125rem', fontWeight: 800, color: '#ffffff' }}>
          {target ? `+${target.plusValue}` : '-'}
        </Box>
        <Box component="span" aria-hidden="true" sx={{ fontSize: '1.5rem', color: colors.forgeLabel }}>
          ▶
        </Box>
        <Box component="span" sx={{ fontSize: '2.125rem', fontWeight: 800, color: colors.forgeEmberText }}>
          {target ? `+${target.plusValue + 1}` : '-'}
        </Box>
      </Box>
      <Box sx={{ mt: 'auto', display: 'flex', flexDirection: 'column', gap: 1.25 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box component="span" sx={{ fontSize: '0.9375rem', fontWeight: 800, color: colors.forgeLabel }}>
            {locale.synthesisCost}
          </Box>
          <Box
            component="span"
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              fontSize: '1.625rem',
              fontWeight: 800,
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            <CoinIcon sx={{ fontSize: 26 }} />
            {cost != null ? formatGold(cost) : '-'}
            <Box component="span" sx={{ fontSize: '1rem', color: colors.forgeLabel }}>
              / {locale.perTime}
            </Box>
          </Box>
        </Box>
        <GoldChangeRow
          dark
          from={gold != null ? formatGold(gold) : '-'}
          to={goldAfter != null ? formatGold(goldAfter) : '-'}
        />
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.25, mt: 0.75 }}>
          <ActionButton kind="forge" onClick={onSynthesize} disabled={busy || !target || !material || !canPay}>
            {locale.actionSynthesize}
          </ActionButton>
          <ActionButton kind="forgeSecondary" onClick={onChangeMaterial} disabled={busy || !canChangeMaterial}>
            {locale.actionChangeMaterial}
          </ActionButton>
        </Box>
      </Box>
    </DoubleFrame>
  )
}
