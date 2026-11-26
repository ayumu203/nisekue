import { Box, Button, ButtonBase, Typography } from '@mui/material'
import type { PointerEvent } from 'react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { skillPanelColors as colors } from '@/constants/styles'
import {
  buildShortEffect,
  categoryStyles,
  elementStyles,
  resolveElementKey,
} from '@/components/moveSetting/moveDisplay'
import { ElementEmblem, MpCost, SkillChip } from '@/components/moveSetting/SkillParts'
import { getMoveAttackRangeLabel } from '@/lib/moveAttackRange'
import type { PlayerMoveSlot } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'

type SkillBookProps = {
  id?: string
  slots: PlayerMoveSlot[]
  selectedIndex: number
  onSelect: (index: number) => void
  // 並べ替えたあとの並びと、動かした行の移動元・移動先
  onReorder: (next: PlayerMoveSlot[], fromIndex: number, toIndex: number) => void
  hasChanges: boolean
  isSaving: boolean
  onSave: () => void
  onCancel: () => void
  // 狭い画面では列をそろえた表ではなく、2 段の行にする
  compact: boolean
}

// PC の行の列：カーソル／番号とつまみ／紋章／技名と説明／種類／属性／範囲／効果／消費MP
const WIDE_CONTENT_COLUMNS = '52px minmax(0, 1fr) 64px 84px 84px 180px 74px'

function reorderSlots(slots: PlayerMoveSlot[], fromIndex: number, toIndex: number): PlayerMoveSlot[] {
  const next = slots.slice()
  const [moved] = next.splice(fromIndex, 1)
  next.splice(toIndex, 0, moved)
  return next.map((slot, index) => ({ ...slot, slot: index + 1 }))
}

function GripIcon() {
  return (
    <svg width="18" height="18" viewBox="0 0 24 24" fill="currentColor" aria-hidden="true">
      <circle cx="9" cy="6" r="1.7" />
      <circle cx="15" cy="6" r="1.7" />
      <circle cx="9" cy="12" r="1.7" />
      <circle cx="15" cy="12" r="1.7" />
      <circle cx="9" cy="18" r="1.7" />
      <circle cx="15" cy="18" r="1.7" />
    </svg>
  )
}

function ForgetWarning({ wrap = false }: { wrap?: boolean }) {
  return (
    <Typography
      component="span"
      sx={{
        flexShrink: wrap ? 1 : 0,
        fontSize: 13,
        lineHeight: 1.4,
        color: '#ffffff',
        backgroundColor: colors.warning,
        px: 1,
        whiteSpace: wrap ? 'normal' : 'nowrap',
        overflow: 'hidden',
        textOverflow: 'ellipsis',
      }}
    >
      {locale.forgetWarning}
    </Typography>
  )
}

function ElementCell({ slot }: { slot: PlayerMoveSlot }) {
  const style = slot.elementType ? elementStyles[slot.elementType] : null
  if (!style?.label) {
    return <Typography sx={{ color: colors.sub }}>{locale.noElement}</Typography>
  }
  return <SkillChip label={style.label} color={style.color} />
}

export default function SkillBook({
  id,
  slots,
  selectedIndex,
  onSelect,
  onReorder,
  hasChanges,
  isSaving,
  onSave,
  onCancel,
  compact,
}: SkillBookProps) {
  const [draggingIndex, setDraggingIndex] = useState<number | null>(null)
  const pointerCaptureRef = useRef<HTMLElement | null>(null)
  const pointerIdRef = useRef<number | null>(null)

  function handlePointerMove(event: PointerEvent<HTMLDivElement>): void {
    if (draggingIndex === null) {
      return
    }

    const hovered = document.elementFromPoint(event.clientX, event.clientY)?.closest('[data-move-slot-index]')
    if (!(hovered instanceof HTMLElement)) {
      return
    }

    const nextIndex = Number(hovered.dataset.moveSlotIndex)
    if (Number.isNaN(nextIndex) || nextIndex === draggingIndex) {
      return
    }

    onReorder(reorderSlots(slots, draggingIndex, nextIndex), draggingIndex, nextIndex)
    setDraggingIndex(nextIndex)
  }

  const finishDragging = useCallback(() => {
    try {
      if (
        pointerCaptureRef.current &&
        pointerIdRef.current !== null &&
        typeof pointerCaptureRef.current.hasPointerCapture === 'function' &&
        pointerCaptureRef.current.hasPointerCapture(pointerIdRef.current)
      ) {
        pointerCaptureRef.current.releasePointerCapture(pointerIdRef.current)
      }
    } finally {
      pointerCaptureRef.current = null
      pointerIdRef.current = null
      setDraggingIndex(null)
    }
  }, [])

  useEffect(() => {
    if (draggingIndex === null) {
      return
    }

    const handleWindowPointerEnd = () => finishDragging()
    window.addEventListener('pointerup', handleWindowPointerEnd)
    window.addEventListener('pointercancel', handleWindowPointerEnd)
    return () => {
      window.removeEventListener('pointerup', handleWindowPointerEnd)
      window.removeEventListener('pointercancel', handleWindowPointerEnd)
    }
  }, [draggingIndex, finishDragging])

  function startDragging(event: PointerEvent<HTMLElement>, index: number): void {
    event.preventDefault()
    pointerIdRef.current = event.pointerId
    if (typeof event.currentTarget.setPointerCapture === 'function') {
      event.currentTarget.setPointerCapture(event.pointerId)
      pointerCaptureRef.current = event.currentTarget
    }
    setDraggingIndex(index)
    onSelect(index)
  }

  return (
    <Box
      id={id}
      component="section"
      aria-label={locale.skillBookLabel}
      sx={{
        flex: compact ? 'none' : 1,
        minWidth: 0,
        minHeight: compact ? 'auto' : 0,
        backgroundColor: colors.cover,
        border: `2px solid ${colors.coverDark}`,
        p: { xs: 1, sm: 1.75 },
        display: 'flex',
        flexDirection: 'column',
        gap: 1.5,
      }}
    >
      <Box
        onPointerMove={handlePointerMove}
        onPointerUp={finishDragging}
        onPointerCancel={finishDragging}
        sx={{
          flex: compact ? 'none' : 1,
          minHeight: 0,
          overflowY: compact ? 'visible' : 'auto',
          border: `3px double ${colors.gold}`,
          backgroundColor: colors.paper,
          px: 1,
          py: 0.75,
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {slots.map((slot, index) => {
          const isEmpty = slot.moveId === null
          const isSelected = index === selectedIndex
          const isDragging = draggingIndex === index
          const element = isEmpty ? null : resolveElementKey(slot)
          const category = slot.category ? categoryStyles[slot.category] : null
          const rangeLabel = getMoveAttackRangeLabel(slot.attackRange)
          const name = slot.moveName ?? (isEmpty ? locale.emptySlot : locale.unknownMove)
          const showWarning = index === 0 && !isEmpty && !hasChanges

          return (
            <Box
              key={`${slot.moveId ?? 'empty'}-${index}`}
              data-move-slot-index={index}
              sx={{
                flex: compact ? 'none' : '1 0 72px',
                display: 'flex',
                alignItems: 'stretch',
                borderBottom: index === slots.length - 1 ? 'none' : `2px solid ${colors.line}`,
                backgroundColor: isSelected ? colors.rowSelected : 'transparent',
                outline: isDragging ? `2px solid ${colors.gold}` : 'none',
                transform: isDragging ? 'translateX(8px)' : 'none',
                transition: 'transform 120ms ease',
                opacity: isEmpty ? 0.6 : 1,
              }}
            >
              {compact ? null : (
                <Box
                  aria-hidden="true"
                  sx={{
                    width: 24,
                    flexShrink: 0,
                    display: 'grid',
                    placeItems: 'center',
                    color: colors.gold,
                    fontSize: 20,
                  }}
                >
                  {isSelected ? '▶' : ''}
                </Box>
              )}
              <Box
                onPointerDown={(event) => startDragging(event, index)}
                aria-label={locale.dragHandle}
                sx={{
                  // 指で押しやすいよう、つまみの当たり判定は 44px 以上にする
                  width: compact ? 44 : 64,
                  flexShrink: 0,
                  display: 'flex',
                  flexDirection: compact ? 'column' : 'row',
                  alignItems: 'center',
                  justifyContent: 'center',
                  gap: 0.5,
                  color: colors.grip,
                  cursor: 'grab',
                  touchAction: 'none',
                }}
              >
                <Typography sx={{ fontSize: compact ? 17 : 20, color: colors.ink, minWidth: 22, textAlign: 'right' }}>
                  {index + 1}
                </Typography>
                <GripIcon />
              </Box>
              <ButtonBase
                onClick={() => onSelect(index)}
                aria-pressed={isSelected}
                sx={{
                  flex: 1,
                  minWidth: 0,
                  fontFamily: 'inherit',
                  color: colors.ink,
                  textAlign: 'left',
                  justifyContent: 'stretch',
                  ...(compact
                    ? { display: 'flex', alignItems: 'center', gap: 1.25, py: 1.25, pr: 1.5 }
                    : {
                        display: 'grid',
                        gridTemplateColumns: WIDE_CONTENT_COLUMNS,
                        alignItems: 'center',
                        columnGap: 1.5,
                        pr: 1.75,
                      }),
                }}
              >
                <ElementEmblem element={element} size={compact ? 50 : 48} />
                {compact ? (
                  <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.75 }}>
                    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
                      <Typography noWrap sx={{ fontSize: 18 }}>
                        {name}
                      </Typography>
                      {typeof slot.mpCost === 'number' ? (
                        <Box sx={{ ml: 'auto' }}>
                          <MpCost mp={slot.mpCost} size={18} fontSize={17} />
                        </Box>
                      ) : null}
                    </Box>
                    {showWarning ? (
                      <Box sx={{ display: 'flex' }}>
                        <ForgetWarning wrap />
                      </Box>
                    ) : null}
                    {isEmpty ? null : (
                      <Box sx={{ display: 'flex', gap: 0.5, overflow: 'hidden' }}>
                        {category ? <SkillChip label={category.label} color={category.color} /> : null}
                        {element && elementStyles[element].label ? (
                          <SkillChip label={elementStyles[element].label ?? ''} color={elementStyles[element].color} />
                        ) : null}
                        {rangeLabel ? <SkillChip label={rangeLabel} color={colors.line} outlined /> : null}
                      </Box>
                    )}
                  </Box>
                ) : (
                  <>
                    <Box sx={{ minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.5 }}>
                      <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, minWidth: 0 }}>
                        <Typography noWrap sx={{ fontSize: 24, flexShrink: 0, maxWidth: '100%' }}>
                          {name}
                        </Typography>
                      </Box>
                      {isEmpty ? null : (
                        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minWidth: 0 }}>
                          {showWarning ? <ForgetWarning /> : null}
                          <Typography noWrap sx={{ fontSize: 15, color: colors.sub, minWidth: 0 }}>
                            {slot.description ?? locale.unknownMoveDescription}
                          </Typography>
                        </Box>
                      )}
                    </Box>
                    <Box>{category ? <SkillChip label={category.label} color={category.color} /> : null}</Box>
                    <Box>{isEmpty ? null : <ElementCell slot={slot} />}</Box>
                    <Typography sx={{ fontSize: 17 }}>{rangeLabel ?? ''}</Typography>
                    <Typography noWrap sx={{ fontSize: 17 }}>
                      {buildShortEffect(slot.effectSummaries)}
                    </Typography>
                    <Box sx={{ justifySelf: 'end' }}>
                      {typeof slot.mpCost === 'number' ? <MpCost mp={slot.mpCost} size={20} fontSize={22} /> : null}
                    </Box>
                  </>
                )}
              </ButtonBase>
            </Box>
          )
        })}
      </Box>

      <Box
        sx={{
          display: compact ? 'grid' : 'flex',
          gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
          justifyContent: 'flex-end',
          gap: 1.5,
          flexShrink: 0,
        }}
      >
        <Button
          variant="outlined"
          onClick={onCancel}
          disabled={isSaving || !hasChanges}
          sx={{
            minHeight: 48,
            minWidth: 120,
            borderRadius: '4px',
            fontSize: 17,
            color: '#e2a08f',
            borderColor: '#e2a08f',
            '&:hover': { borderColor: '#e2a08f', backgroundColor: 'rgba(226, 160, 143, 0.12)' },
            '&.Mui-disabled': { color: 'rgba(255, 255, 255, 0.45)', borderColor: 'rgba(255, 255, 255, 0.25)' },
          }}
        >
          {locale.cancelMoveOrder}
        </Button>
        <Button
          variant="contained"
          onClick={onSave}
          disabled={isSaving || !hasChanges}
          sx={{
            minHeight: 48,
            minWidth: 160,
            borderRadius: '4px',
            fontSize: 17,
            color: '#ffffff',
            backgroundColor: '#3f9f4a',
            boxShadow: 'none',
            '&:hover': { backgroundColor: '#358a3f', boxShadow: 'none' },
            '&.Mui-disabled': { color: 'rgba(255, 255, 255, 0.55)', backgroundColor: 'rgba(63, 159, 74, 0.35)' },
          }}
        >
          {isSaving ? locale.savingMoveOrder : locale.saveMoveOrder}
        </Button>
      </Box>
    </Box>
  )
}
