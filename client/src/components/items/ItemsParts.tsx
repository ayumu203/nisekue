import { Box, ButtonBase, InputBase, SvgIcon, type SvgIconProps } from '@mui/material'
import type { ReactNode } from 'react'
import { itemsPanelColors as colors } from '@/constants/styles'
import locale from '../../../locale/items/Items.json'
import { font, palette, withAlpha } from '@/theme/tokens'

export type ItemsTabKey = 'bag' | 'forge' | 'market'

function BagTabIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24" sx={{ fill: 'none', stroke: 'currentColor', strokeWidth: 2, ...props.sx }}>
      <path d="M8 8V6a4 4 0 0 1 8 0v2" strokeLinecap="round" />
      <path d="M4 8h16l-1.5 12h-13z" strokeLinejoin="round" />
      <path d="M9 13h6" strokeLinecap="round" />
    </SvgIcon>
  )
}

export function ForgeTabIcon(props: SvgIconProps) {
  return (
    <SvgIcon
      {...props}
      viewBox="0 0 24 24"
      sx={{
        fill: 'none',
        stroke: 'currentColor',
        strokeWidth: 2,
        strokeLinecap: 'round',
        strokeLinejoin: 'round',
        ...props.sx,
      }}
    >
      <path d="M4 15h13a3 3 0 0 0 3-3H8" />
      <path d="M7 15v3h9v-3" />
      <path d="M5 20h13" />
      <path d="m13 4 4 4-3 3-4-4z" />
      <path d="m11 9-4 4" />
    </SvgIcon>
  )
}

export function MarketTabIcon(props: SvgIconProps) {
  return (
    <SvgIcon
      {...props}
      viewBox="0 0 24 24"
      sx={{
        fill: 'none',
        stroke: 'currentColor',
        strokeWidth: 2,
        strokeLinecap: 'round',
        strokeLinejoin: 'round',
        ...props.sx,
      }}
    >
      <path d="M3 9 5 4h14l2 5" />
      <path d="M3 9a3 3 0 0 0 6 0 3 3 0 0 0 6 0 3 3 0 0 0 6 0" />
      <path d="M5 12v8h14v-8" />
      <path d="M10 20v-5h4v5" />
    </SvgIcon>
  )
}

export function CoinIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <circle cx="12" cy="12" r="9" fill={palette.gold300} stroke={palette.gold700} strokeWidth="2" />
      <path
        d="M12 7v10M9 9.5h4.5a1.5 1.5 0 0 1 0 3h-3a1.5 1.5 0 0 0 0 3H15"
        stroke={palette.gold700}
        strokeWidth="1.6"
        fill="none"
      />
    </SvgIcon>
  )
}

const tabs: Array<{ key: ItemsTabKey; label: string; icon: ReactNode }> = [
  { key: 'bag', label: locale.bagTab, icon: <BagTabIcon /> },
  { key: 'forge', label: locale.forgeTab, icon: <ForgeTabIcon /> },
  { key: 'market', label: locale.marketTab, icon: <MarketTabIcon /> },
]

// 袋の上に付いたしおりのようなタブ。選んでいるタブは袋の布地とつながって見える
export function ItemsTabs({
  value,
  onChange,
  startAction,
}: {
  value: ItemsTabKey
  onChange: (value: ItemsTabKey) => void
  // スマホのホームボタン（ヘッダーがないため、タブの左に置く）
  startAction?: ReactNode
}) {
  // ほかの画面の帯と同じく、タブ（スマホはホームボタンも）を隙間なく 1 本の帯につなげて、袋の上辺にする
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'stretch',
        flex: 'none',
        height: { xs: 48, sm: 56 },
        border: `3px solid ${colors.leather}`,
        borderBottom: 'none',
        borderRadius: '18px 18px 0 0',
        overflow: 'hidden',
        backgroundColor: colors.tabOff,
      }}
    >
      {startAction ? (
        <Box
          sx={{
            display: 'flex',
            alignItems: 'center',
            px: 0.25,
            backgroundColor: colors.leather,
            // ホームボタンとタブの区切り（ほかの画面の帯と同じ白い線）
            borderRight: `2px solid ${withAlpha(palette.white, 0.9)}`,
          }}
        >
          {startAction}
        </Box>
      ) : null}
      <Box
        role="tablist"
        aria-label={locale.tabsLabel}
        sx={{ display: 'flex', alignItems: 'stretch', flex: 1, minWidth: 0 }}
      >
        {tabs.map((tab, index) => {
          const selected = tab.key === value
          const previousSelected = index > 0 && tabs[index - 1].key === value
          return (
            <ButtonBase
              key={tab.key}
              role="tab"
              aria-selected={selected}
              onClick={() => onChange(tab.key)}
              disableRipple
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'center',
                gap: 1,
                flex: 1,
                minWidth: 0,
                px: 0.5,
                // 選んでいないタブどうしの区切り
                borderLeft:
                  index === 0 || selected || previousSelected ? 'none' : `1px solid ${withAlpha(palette.white, 0.25)}`,
                // 選んでいるタブは袋の布地の色にして、袋とつなげる
                backgroundColor: selected ? colors.canvas : colors.tabOff,
                color: selected ? colors.text : palette.white,
                fontFamily: 'inherit',
                fontSize: { xs: font.md, sm: font.lg },
                fontWeight: 800,
                whiteSpace: 'nowrap',
                '& .MuiSvgIcon-root': { display: { xs: 'none', sm: 'inline-block' }, fontSize: font.xxl },
              }}
            >
              {tab.icon}
              {tab.label}
            </ButtonBase>
          )
        })}
      </Box>
    </Box>
  )
}

// 道具袋の本体（革の枠と、内側の縫い目）。PC は高さいっぱいに広げ、中身は袋の中でスクロールする
export function BagFrame({ children, fill }: { children: ReactNode; fill: boolean }) {
  return (
    <Box
      sx={{
        flex: fill ? 1 : 'none',
        minHeight: 0,
        display: 'flex',
        flexDirection: 'column',
        backgroundColor: colors.canvas,
        border: `3px solid ${colors.leather}`,
        // 上辺はタブの帯になるので、上の枠と角丸を付けない
        borderTop: 'none',
        borderRadius: '0 0 18px 18px',
        p: { xs: 0.75, sm: 1.5 },
        boxShadow: `0 6px 0 ${colors.leatherDark}`,
      }}
    >
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          border: `2px dashed ${colors.stitch}`,
          borderRadius: '12px',
          px: { xs: 1.5, sm: 2.5 },
          py: { xs: 1.5, sm: 2.5 },
          display: 'flex',
          flexDirection: 'column',
          gap: 2,
        }}
      >
        {children}
      </Box>
    </Box>
  )
}

// 右側の札（姿見・選んだ品の札）
export function TagCard({ children, fill = false }: { children: ReactNode; fill?: boolean }) {
  return (
    <Box
      component="section"
      sx={{
        flex: fill ? 1 : 'none',
        minHeight: 0,
        backgroundColor: colors.slot,
        border: `3px solid ${colors.leather}`,
        borderRadius: '18px',
        boxShadow: `0 6px 0 ${colors.leatherDark}`,
        overflow: 'auto',
      }}
    >
      {children}
    </Box>
  )
}

// 鍛冶場・購入の箱（外の枠と、内側の線の二重）
export function DoubleFrame({ children, dark = false, fill }: { children: ReactNode; dark?: boolean; fill: boolean }) {
  return (
    <Box
      component="section"
      sx={{
        flex: fill ? 1 : 'none',
        minHeight: 0,
        display: 'flex',
        backgroundColor: dark ? colors.forge : colors.canvas,
        border: `3px solid ${colors.leather}`,
        borderRadius: '18px',
        boxShadow: `0 6px 0 ${colors.leatherDark}`,
        p: { xs: 0.75, sm: 1.5 },
        color: dark ? colors.forgeText : colors.text,
      }}
    >
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflow: 'auto',
          display: 'flex',
          flexDirection: 'column',
          gap: { xs: 2, sm: 3 },
          border: dark ? `2px solid ${colors.forgeLine}` : `2px dashed ${colors.stitch}`,
          borderRadius: '12px',
          px: { xs: 2, sm: 3 },
          py: { xs: 2, sm: 3 },
        }}
      >
        {children}
      </Box>
    </Box>
  )
}

// 鍛冶場・購入の見出し（アイコン＋見出し、下に区切り線）
export function FrameHeading({ icon, title, dark = false }: { icon: ReactNode; title: string; dark?: boolean }) {
  return (
    <Box
      sx={{
        display: 'flex',
        alignItems: 'center',
        gap: 1.5,
        pb: 2,
        borderBottom: dark ? `2px solid ${colors.forgeLine}` : `2px dashed ${colors.stitch}`,
        color: dark ? colors.forgeEmber : colors.leather,
        '& .MuiSvgIcon-root': { fontSize: font.xxxl },
      }}
    >
      {icon}
      <Box
        component="h2"
        sx={{ m: 0, fontSize: font.xxl, fontWeight: 800, color: dark ? colors.forgeText : colors.text }}
      >
        {title}
      </Box>
    </Box>
  )
}

type ActionKind = 'primary' | 'secondary' | 'danger' | 'forge' | 'forgeSecondary'

const actionColors: Record<ActionKind, { bg: string; border: string; text: string }> = {
  primary: { bg: colors.leather, border: colors.leather, text: palette.white },
  secondary: { bg: colors.slot, border: colors.leather, text: colors.leather },
  danger: { bg: colors.slot, border: colors.danger, text: colors.dangerText },
  forge: { bg: colors.forgeButton, border: colors.forgeButton, text: palette.white },
  forgeSecondary: { bg: 'transparent', border: colors.forgeMuted, text: colors.forgeText },
}

// 画面の中の操作ボタン。どのボタンも高さ・文字・枠をそろえる
export function ActionButton({
  kind,
  children,
  onClick,
  disabled,
}: {
  kind: ActionKind
  children: ReactNode
  onClick?: () => void
  disabled?: boolean
}) {
  const c = actionColors[kind]
  return (
    <ButtonBase
      onClick={onClick}
      disabled={disabled}
      disableRipple
      sx={{
        width: '100%',
        height: 52,
        borderRadius: '12px',
        border: `3px solid ${c.border}`,
        backgroundColor: c.bg,
        color: c.text,
        fontFamily: 'inherit',
        fontSize: font.lg,
        fontWeight: 800,
        '&.Mui-disabled': { opacity: 0.45 },
      }}
    >
      {children}
    </ButtonBase>
  )
}

// 数を入れる欄（出品の単価・数量）
export function NumberField({
  value,
  onChange,
  label,
  unit,
  disabled,
  width = 96,
}: {
  value: string
  onChange: (value: string) => void
  label: string
  unit: string
  disabled?: boolean
  width?: number
}) {
  return (
    <Box component="label" sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
      <InputBase
        value={value}
        onChange={(event) => onChange(event.target.value.replace(/[^0-9]/g, ''))}
        disabled={disabled}
        inputProps={{ inputMode: 'numeric', 'aria-label': label }}
        sx={{
          width,
          height: 50,
          px: 1.5,
          boxSizing: 'border-box',
          border: `2px solid ${colors.slotLine}`,
          borderRadius: '10px',
          backgroundColor: palette.white,
          fontFamily: 'inherit',
          fontSize: font.xxl,
          fontWeight: 800,
          color: colors.text,
          '& input': { textAlign: 'right', p: 0 },
          '&.Mui-disabled': { backgroundColor: colors.canvas },
        }}
      />
      <Box component="span" sx={{ fontSize: font.lg, fontWeight: 800, color: colors.text }}>
        {unit}
      </Box>
    </Box>
  )
}

// 「×」「=」の記号
export function Operator({ children, dark = false }: { children: ReactNode; dark?: boolean }) {
  return (
    <Box
      component="span"
      aria-hidden="true"
      sx={{ fontSize: font.xxl, fontWeight: 800, color: dark ? colors.forgeLabel : colors.label }}
    >
      {children}
    </Box>
  )
}

// 「所持金 12,480 G → 11,280 G」の 1 行
export function GoldChangeRow({ from, to, dark = false }: { from: string; to: string; dark?: boolean }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 1 }}>
      <Box component="span" sx={{ fontSize: font.md, fontWeight: 800, color: dark ? colors.forgeLabel : colors.label }}>
        {locale.goldLabel}
      </Box>
      <Box component="span" sx={{ fontSize: font.xl, fontWeight: 800, fontVariantNumeric: 'tabular-nums' }}>
        {from}{' '}
        <Box component="span" aria-hidden="true" sx={{ color: dark ? colors.forgeEmber : colors.leather }}>
          →
        </Box>{' '}
        {to}
      </Box>
    </Box>
  )
}
