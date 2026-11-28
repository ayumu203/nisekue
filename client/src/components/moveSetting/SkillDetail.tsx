import { useState } from 'react'
import { Box, ButtonBase, Typography } from '@mui/material'
import { skillPanelColors as colors } from '@/constants/styles'
import {
  actorRowLabels,
  actorRows,
  buildEffectEntries,
  categoryStyles,
  elementStyles,
  isActorRowRelevant,
  resolveElementKey,
  resolveReachableRangeCells,
  resolveReachableRowCount,
  resolveTargetLabel,
  type ActorRow,
} from '@/components/moveSetting/moveDisplay'
import { ElementEmblem, MpCost, SkillChip } from '@/components/moveSetting/SkillParts'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import { getMoveAttackRangeLabel } from '@/lib/moveAttackRange'
import type { PlayerMoveSlot } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'
import { font, palette, withAlpha } from '@/theme/tokens'

// 範囲の図に並べる絵。陣形の [前・中・後列][左・右] の順。見た目のための固定の絵
const BATTLEFIELD_IMAGE = 'image/quest/moonlit-castle-battlefield.svg'
const ENEMY_IMAGES = [
  ['image/training/01_heishi.png', 'image/training/02_heishi.png'],
  ['image/training/04_magic.png', 'image/training/05_magic.png'],
  ['image/training/07_rev.png', 'image/training/10_load.png'],
]
const ALLY_IMAGES = [
  ['image/character/ch017_in.png', 'image/character/ch018_in.png'],
  [null, 'image/character/ch020_in.png'],
  ['image/character/ch022_in.png', 'image/character/ch015_in.png'],
]

type ActorRowSelectorProps = {
  value: ActorRow
  onChange: (row: ActorRow) => void
  compact: boolean
}

// 技を使う人の位置の切り替え。位置によって届く敵の列が変わる
function ActorRowSelector({ value, onChange, compact }: ActorRowSelectorProps) {
  return (
    <Box
      role="group"
      aria-label={locale.actorRowLabel}
      sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: compact ? 0.5 : 1 }}
    >
      {actorRows.map((row) => {
        const selected = row === value
        return (
          <ButtonBase
            key={row}
            aria-pressed={selected}
            onClick={() => onChange(row)}
            sx={{
              minHeight: 44,
              borderRadius: '4px',
              border: `2px solid ${colors.gold}`,
              backgroundColor: selected ? colors.gold : 'transparent',
              color: selected ? palette.brown900 : colors.ink,
              fontFamily: 'inherit',
              fontSize: compact ? font.sm : font.md,
            }}
          >
            {actorRowLabels[row]}
          </ButtonBase>
        )
      })}
    </Box>
  )
}

type RangeFigureProps = {
  slot: PlayerMoveSlot
  playerImagePath: string | null
  actorRow: ActorRow
  compact: boolean
}

// 戦場の絵の上に陣形（3 列×左右 2）を並べ、技が当たる相手の足もとに属性の色の輪を敷く
// 敵が対象の技では、使う人の位置から届かない列を暗くして「射程外」と出す
function RangeFigure({ slot, playerImagePath, actorRow, compact }: RangeFigureProps) {
  const isAllySide = slot.targetType === 'Ally' || slot.targetType === 'Self'
  const showsReach = isActorRowRelevant(slot)
  const hitCells = resolveReachableRangeCells(slot, actorRow)
  const reachableRowCount = showsReach ? resolveReachableRowCount(actorRow) : 3
  const outOfReachRows = [0, 1, 2].filter((row) => row >= reachableRowCount)
  const color = elementStyles[resolveElementKey(slot)].color
  const rangeLabel = getMoveAttackRangeLabel(slot.attackRange) ?? ''
  const ariaTemplate = showsReach ? locale.rangeAriaWithRow : locale.rangeAria
  // 手前（前列）ほど大きく描く
  const heights = isAllySide ? (compact ? [84, 72, 60] : [190, 165, 140]) : compact ? [62, 54, 46] : [130, 110, 92]

  return (
    <Box
      role="img"
      aria-label={ariaTemplate
        .replace('{{row}}', actorRowLabels[actorRow])
        .replace('{{side}}', isAllySide ? locale.targetAlly : locale.targetEnemy)
        .replace('{{range}}', rangeLabel)}
      sx={{
        flex: 1,
        minHeight: compact ? 260 : 320,
        border: `2px solid ${colors.gold}`,
        borderRadius: '4px',
        overflow: 'hidden',
        p: compact ? 1 : 2,
        backgroundImage: `linear-gradient(${withAlpha(palette.navy, 0.25)}, ${withAlpha(palette.navy, 0.25)}), url(${resolvePublicAssetPath(BATTLEFIELD_IMAGE)})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
        display: 'grid',
        gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
        gridTemplateRows: 'repeat(3, minmax(0, 1fr))',
        gap: 0.5,
      }}
    >
      {[2, 1, 0].flatMap((row) =>
        [0, 1].map((column) => {
          const isHit = hitCells.some(([r, c]) => r === row && c === column)
          const height = heights[row]
          // 味方の中列の左は自分の位置。自分の画像を出す
          const src = isAllySide
            ? (ALLY_IMAGES[row][column] ?? resolveCharacterAssetPath(playerImagePath))
            : ENEMY_IMAGES[row][column]
          const ringWidth = height + (compact ? 20 : 30)

          return (
            <Box
              key={`${row}-${column}`}
              sx={{
                // 前列を下に描く。射程外の帯を重ねるため、置く位置を明示する
                gridRow: 3 - row,
                gridColumn: column + 1,
                position: 'relative',
                display: 'flex',
                alignItems: 'flex-end',
                justifyContent: 'center',
                pb: compact ? 0.75 : 1.5,
                minHeight: 0,
              }}
            >
              {isHit ? (
                <Box
                  sx={{
                    position: 'absolute',
                    bottom: 2,
                    left: '50%',
                    width: ringWidth,
                    height: compact ? 14 : 22,
                    ml: `-${ringWidth / 2}px`,
                    borderRadius: '50%',
                    border: `${compact ? 2 : 3}px solid ${color}`,
                    backgroundColor: `${color}55`,
                  }}
                />
              ) : null}
              {src ? (
                <Box sx={{ position: 'relative', display: 'flex', justifyContent: 'center', maxHeight: '100%' }}>
                  <Box
                    component="img"
                    src={src.startsWith('image/') ? resolvePublicAssetPath(src) : src}
                    alt=""
                    sx={{
                      display: 'block',
                      height,
                      maxHeight: '100%',
                      maxWidth: '100%',
                      objectFit: 'contain',
                      ...(isHit ? {} : { filter: 'grayscale(1) brightness(0.6)', opacity: 0.45 }),
                    }}
                  />
                  {isHit && !isAllySide ? (
                    <Box
                      sx={{
                        position: 'absolute',
                        top: compact ? -6 : -8,
                        right: compact ? -14 : -22,
                        width: compact ? 20 : 28,
                        height: compact ? 20 : 28,
                        borderRadius: '50%',
                        backgroundColor: color,
                        display: 'grid',
                        placeItems: 'center',
                      }}
                    >
                      <svg
                        width={compact ? 13 : 18}
                        height={compact ? 13 : 18}
                        viewBox="0 0 24 24"
                        fill="none"
                        stroke={palette.white}
                        strokeWidth="2.6"
                        strokeLinecap="round"
                        aria-hidden="true"
                      >
                        <circle cx="12" cy="12" r="6" />
                        <path d="M12 2v5M12 17v5M2 12h5M17 12h5" />
                      </svg>
                    </Box>
                  ) : null}
                </Box>
              ) : null}
            </Box>
          )
        }),
      )}
      {outOfReachRows.map((row) => (
        <Box
          key={`out-of-reach-${row}`}
          sx={{
            gridRow: 3 - row,
            gridColumn: '1 / -1',
            position: 'relative',
            zIndex: 1,
            borderRadius: '4px',
            backgroundColor: withAlpha(palette.navy, 0.5),
            display: 'flex',
            alignItems: 'flex-start',
            p: compact ? 0.5 : 1,
          }}
        >
          <Typography
            component="span"
            sx={{
              fontSize: compact ? font.xs : font.sm,
              lineHeight: 1.4,
              px: 1,
              py: 0.25,
              borderRadius: '3px',
              backgroundColor: palette.brown900,
              border: `1px solid ${colors.gold}`,
              color: colors.ink,
            }}
          >
            {locale.outOfReach}
          </Typography>
        </Box>
      ))}
    </Box>
  )
}

function Tile({ label, value, compact }: { label: string; value: string; compact: boolean }) {
  return (
    <Box
      sx={{
        backgroundColor: palette.brown900,
        borderRadius: '4px',
        px: compact ? 1.5 : 2,
        py: compact ? 1 : 2,
        display: 'flex',
        flexDirection: 'column',
        gap: 0.25,
      }}
    >
      <Typography sx={{ fontSize: compact ? font.sm : font.md, color: colors.sub }}>{label}</Typography>
      <Typography sx={{ fontSize: compact ? font.xl : font.xxl, color: colors.ink }}>{value}</Typography>
    </Box>
  )
}

type SkillDetailProps = {
  slot: PlayerMoveSlot | null
  playerImagePath: string | null
  compact: boolean
}

// 選んだスキルの詳細：紋章・技名・種類と属性・消費MP、説明、対象と効果、範囲の図
export default function SkillDetail({ slot, playerImagePath, compact }: SkillDetailProps) {
  // 初期値は後衛（技の範囲がすべて届く）。スキルを切り替えても保持する
  const [actorRow, setActorRow] = useState<ActorRow>('Back')

  if (!slot || slot.moveId === null) {
    return null
  }

  const element = resolveElementKey(slot)
  const elementLabel = elementStyles[element].label
  const category = slot.category ? categoryStyles[slot.category] : null
  const targetLabel = resolveTargetLabel(slot.targetType)
  const effects = buildEffectEntries(slot.effectSummaries)

  return (
    <Box
      sx={{
        flex: 1,
        minHeight: 0,
        border: `3px double ${colors.gold}`,
        backgroundColor: colors.paper,
        px: compact ? 2 : 3,
        py: compact ? 2 : 3,
        display: 'flex',
        flexDirection: 'column',
        gap: compact ? 1.5 : 3,
        overflowY: 'auto',
      }}
    >
      <Box sx={{ display: 'flex', alignItems: 'center', gap: compact ? 1.5 : 2.5 }}>
        <ElementEmblem element={element} size={compact ? 64 : 104} />
        <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: compact ? 0.75 : 1.5 }}>
          <Typography
            component="h2"
            sx={{ fontSize: compact ? font.xxl : font.display, lineHeight: 1.2, color: colors.ink }}
          >
            {slot.moveName ?? locale.unknownMove}
          </Typography>
          <Box sx={{ display: 'flex', gap: 0.75 }}>
            {category ? <SkillChip label={category.label} color={category.color} /> : null}
            {elementLabel ? <SkillChip label={elementLabel} color={elementStyles[element].color} /> : null}
          </Box>
        </Box>
        {typeof slot.mpCost === 'number' ? (
          <MpCost mp={slot.mpCost} size={compact ? 28 : 44} fontSize={compact ? 30 : 48} />
        ) : null}
      </Box>
      <Typography sx={{ fontSize: compact ? font.md : font.xl, lineHeight: 1.7, color: colors.ink }}>
        {slot.description ?? locale.unknownMoveDescription}
      </Typography>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: compact ? 1 : 1.5 }}>
        {targetLabel ? <Tile label={locale.targetLabel} value={targetLabel} compact={compact} /> : null}
        {effects.map((effect) => (
          <Tile key={`${effect.label}-${effect.value}`} label={effect.label} value={effect.value} compact={compact} />
        ))}
      </Box>
      {isActorRowRelevant(slot) ? <ActorRowSelector value={actorRow} onChange={setActorRow} compact={compact} /> : null}
      <RangeFigure slot={slot} playerImagePath={playerImagePath} actorRow={actorRow} compact={compact} />
    </Box>
  )
}
