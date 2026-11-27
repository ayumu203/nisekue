import { Box, ButtonBase, Typography } from '@mui/material'
import { itemsPanelColors as colors } from '@/constants/styles'
import { ItemIcon } from './ItemIllustrations'
import { canForge, getForgeTarget, type ForgeGroup } from './itemsLogic'
import { ActionButton, BagFrame, CoinIcon, DoubleFrame, ForgeTabIcon, FrameHeading, GoldChangeRow } from './ItemsParts'
import { formatGold, formatPlusValue, getSynthesisCost } from './itemUtils'
import locale from '../../../locale/items/Items.json'
import type { ItemEquipmentView } from '@/schema/item'
import { font, palette } from '@/theme/tokens'

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
      <Typography component="h2" sx={{ m: 0, fontSize: font.xl, fontWeight: 800, color: colors.text }}>
        {locale.forgeListTitle}
      </Typography>
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          p: 0.5,
          m: -0.5,
          display: 'flex',
          flexDirection: 'column',
          gap: 1.5,
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
                py: 1.5,
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
                  fontSize: font.lg,
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
                      backgroundColor: ok ? colors.leatherDark : palette.brown200,
                      color: palette.white,
                      fontSize: font.sm,
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
                  fontSize: font.md,
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
        sx={{ fontSize: font.sm, fontWeight: 800, color: after ? colors.forgeEmberText : colors.forgeLabel }}
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
          fontSize: { xs: font.sm, sm: font.lg },
          fontWeight: 800,
          color: palette.white,
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
      <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: { xs: 0.75, sm: 2 } }}>
        <ForgeSlot label={locale.forgeTarget} item={target} />
        <Box
          component="span"
          aria-hidden="true"
          sx={{ fontSize: font.xxxl, fontWeight: 800, color: colors.forgeLabel }}
        >
          ＋
        </Box>
        <ForgeSlot label={locale.forgeMaterial} item={material} />
        <Box
          component="span"
          aria-hidden="true"
          sx={{ fontSize: font.xxxl, fontWeight: 800, color: colors.forgeEmber }}
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
          gap: 2.5,
          py: 2,
          borderTop: `2px solid ${colors.forgeLine}`,
          borderBottom: `2px solid ${colors.forgeLine}`,
        }}
      >
        <Box component="span" sx={{ fontSize: font.md, fontWeight: 800, color: colors.forgeLabel }}>
          {locale.plusValueLabel}
        </Box>
        <Box component="span" sx={{ fontSize: font.display, fontWeight: 800, color: palette.white }}>
          {target ? `+${target.plusValue}` : '-'}
        </Box>
        <Box component="span" aria-hidden="true" sx={{ fontSize: font.xxl, color: colors.forgeLabel }}>
          ▶
        </Box>
        <Box component="span" sx={{ fontSize: font.display, fontWeight: 800, color: colors.forgeEmberText }}>
          {target ? `+${target.plusValue + 1}` : '-'}
        </Box>
      </Box>
      <Box sx={{ mt: 'auto', display: 'flex', flexDirection: 'column', gap: 1.5 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
          <Box component="span" sx={{ fontSize: font.md, fontWeight: 800, color: colors.forgeLabel }}>
            {locale.synthesisCost}
          </Box>
          <Box
            component="span"
            sx={{
              display: 'flex',
              alignItems: 'center',
              gap: 1,
              fontSize: font.xxxl,
              fontWeight: 800,
              fontVariantNumeric: 'tabular-nums',
            }}
          >
            <CoinIcon sx={{ fontSize: font.xxxl }} />
            {cost != null ? formatGold(cost) : '-'}
            <Box component="span" sx={{ fontSize: font.md, color: colors.forgeLabel }}>
              / {locale.perTime}
            </Box>
          </Box>
        </Box>
        <GoldChangeRow
          dark
          from={gold != null ? formatGold(gold) : '-'}
          to={goldAfter != null ? formatGold(goldAfter) : '-'}
        />
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1.5, mt: 0.75 }}>
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
