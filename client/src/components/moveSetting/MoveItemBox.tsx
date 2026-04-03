import { Alert, Box, Button, Chip, Paper, Stack, Typography } from '@mui/material'
import type { PointerEvent } from 'react'
import { useCallback, useEffect, useRef, useState } from 'react'
import { updatePlayerMoveSet } from '@/api/player'
import MoveItem from '@/components/moveSetting/MoveItem'
import { resolveCharacterAssetPath } from '@/lib/assets'
import type { GetPlayerResponse, PlayerMoveSlot } from '@/schema/player'
import locale from '../../../locale/player-setting/PlayerSetting.json'

type MoveItemBoxProps = {
  player: GetPlayerResponse
  accessToken: string
  onSaved: () => Promise<void>
}

function reorderSlots(slots: PlayerMoveSlot[], fromIndex: number, toIndex: number): PlayerMoveSlot[] {
  const next = slots.slice()
  const [moved] = next.splice(fromIndex, 1)
  next.splice(toIndex, 0, moved)
  return next.map((slot, index) => ({ ...slot, slot: index + 1 }))
}

export default function MoveItemBox({ player, accessToken, onSaved }: MoveItemBoxProps) {
  const [editableSlots, setEditableSlots] = useState(player.moveSlots)
  const [draggingIndex, setDraggingIndex] = useState<number | null>(null)
  const pointerCaptureRef = useRef<HTMLDivElement | null>(null)
  const pointerIdRef = useRef<number | null>(null)
  const [isSaving, setIsSaving] = useState(false)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)

  useEffect(() => {
    setEditableSlots(player.moveSlots)
  }, [player.moveSlots])

  const equippedMoves = editableSlots.filter((slot) => slot.moveId !== null)
  const attackCount = equippedMoves.filter((slot) => slot.category === 'Attack').length
  const supportCount = equippedMoves.filter((slot) => slot.category === 'Support').length
  const hybridCount = equippedMoves.filter((slot) => slot.category === 'Hybrid').length
  const playerImageSrc = resolveCharacterAssetPath(player.imagePath)
  const hasChanges = editableSlots.some((slot, index) => slot.moveId !== player.moveSlots[index]?.moveId)

  async function handleSave(): Promise<void> {
    setIsSaving(true)
    setFeedback(null)
    try {
      const response = await updatePlayerMoveSet(
        {
          moveIds: editableSlots.map((slot) => slot.moveId),
        },
        accessToken,
      )
      setFeedback({ type: 'success', message: response.message || locale.moveOrderSaved })
      await onSaved()
    } catch (error) {
      setFeedback({
        type: 'error',
        message: error instanceof Error ? error.message : locale.moveOrderSaveFailed,
      })
    } finally {
      setIsSaving(false)
    }
  }

  function handleCancel(): void {
    setEditableSlots(player.moveSlots)
    setDraggingIndex(null)
    setFeedback(null)
  }

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

    setEditableSlots((current) => reorderSlots(current, draggingIndex, nextIndex))
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
  }, [setDraggingIndex])

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

  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 3,
        p: { xs: 2, sm: 2.5 },
        backgroundColor: '#f6f0de',
        borderColor: '#cdb884',
      }}
    >
      <Stack spacing={2}>
        <Paper
          variant="outlined"
          sx={{
            borderRadius: 3,
            p: { xs: 2, sm: 2.5 },
            borderColor: '#b8ab7a',
            backgroundColor: '#44644a',
          }}
        >
          <Stack spacing={1.5}>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={2} alignItems={{ xs: 'stretch', sm: 'center' }}>
              <Box
                sx={{
                  width: { xs: '100%', sm: 140 },
                  minWidth: { sm: 140 },
                  aspectRatio: '338 / 350',
                  borderRadius: 3,
                  border: '2px solid #b8ab7a',
                  backgroundColor: 'rgba(255, 249, 232, 0.9)',
                  overflow: 'hidden',
                  display: 'grid',
                  placeItems: 'center',
                  p: 1,
                }}
              >
                {playerImageSrc ? (
                  <Box
                    component="img"
                    src={playerImageSrc}
                    alt={player.userName ?? 'player'}
                    sx={{
                      width: '100%',
                      height: '100%',
                      objectFit: 'contain',
                      objectPosition: 'center bottom',
                      display: 'block',
                    }}
                  />
                ) : (
                  <Typography variant="body2" color="text.secondary">
                    画像なし
                  </Typography>
                )}
              </Box>

              <Stack spacing={1.5} sx={{ flex: 1, minWidth: 0 }}>
                <Stack spacing={0.5}>
                  <Typography variant="overline" sx={{ letterSpacing: '0.18em', color: 'rgba(242, 235, 207, 0.8)' }}>
                    TACTICAL LOADOUT
                  </Typography>
                  <Typography variant="h4" fontWeight={900} lineHeight={1.1} color="#fff8ea">
                    {locale.moveListTitle}
                  </Typography>
                  <Typography variant="body2" sx={{ color: 'rgba(242, 235, 207, 0.82)' }}>
                    {locale.moveListDescription}
                  </Typography>
                </Stack>

                <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
                  <Chip
                    size="small"
                    label={`レベル ${player.level}`}
                    sx={{ fontWeight: 700, bgcolor: 'rgba(255, 249, 232, 0.94)', color: '#35513a' }}
                  />
                  <Chip
                    size="small"
                    label={`職業Lv ${player.jobLevel}`}
                    sx={{ fontWeight: 700, bgcolor: 'rgba(255, 249, 232, 0.94)', color: '#35513a' }}
                  />
                  <Chip
                    size="small"
                    label={`${player.job.displayName}`}
                    sx={{ fontWeight: 700, bgcolor: 'rgba(239, 226, 183, 0.96)', color: '#5f4a18' }}
                  />
                </Stack>
              </Stack>
            </Stack>

            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: 'repeat(2, minmax(0, 1fr))', sm: 'repeat(4, minmax(0, 1fr))' },
                gap: 1,
              }}
            >
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#d2c08b', bgcolor: 'rgba(255, 249, 232, 0.92)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  スロット
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#324c36">
                  {equippedMoves.length} / {editableSlots.length}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#d8baa3', bgcolor: 'rgba(255, 246, 242, 0.92)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  攻撃
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#8a2f24">
                  {attackCount}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#b5c79d', bgcolor: 'rgba(242, 248, 235, 0.94)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  補助
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#456132">
                  {supportCount}
                </Typography>
              </Paper>
              <Paper
                variant="outlined"
                sx={{ borderRadius: 2.5, p: 1.25, borderColor: '#d8c07d', bgcolor: 'rgba(255, 248, 224, 0.92)' }}
              >
                <Typography variant="caption" color="text.secondary">
                  特殊
                </Typography>
                <Typography variant="h6" fontWeight={900} color="#7b5a15">
                  {hybridCount}
                </Typography>
              </Paper>
            </Box>
          </Stack>
        </Paper>

        {feedback ? <Alert severity={feedback.type}>{feedback.message}</Alert> : null}

        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          spacing={1.5}
          alignItems={{ sm: 'center' }}
        >
          <Typography variant="h6" fontWeight={900}>
            {locale.editingOrderTitle}
          </Typography>
          <Stack direction="row" spacing={1}>
            <Button variant="outlined" onClick={handleCancel} disabled={isSaving || !hasChanges}>
              {locale.cancelMoveOrder}
            </Button>
            <Button variant="contained" onClick={() => void handleSave()} disabled={isSaving || !hasChanges}>
              {isSaving ? locale.savingMoveOrder : locale.saveMoveOrder}
            </Button>
          </Stack>
        </Stack>

        <Box onPointerMove={handlePointerMove} onPointerUp={finishDragging} onPointerCancel={finishDragging}>
          <Stack
            sx={{
              display: 'grid',
              gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
              gap: 1.5,
            }}
          >
            {editableSlots.map((slot, index) => (
              <Box
                key={`${slot.moveId ?? 'empty'}-${index}`}
                data-move-slot-index={index}
                sx={{
                  transition: 'transform 120ms ease, opacity 120ms ease',
                  opacity: draggingIndex === index ? 0.9 : 1,
                  transform: draggingIndex === index ? 'scale(1.01)' : 'none',
                }}
              >
                <MoveItem
                  slot={slot}
                  isDragging={draggingIndex === index}
                  onHandlePointerDown={(event) => {
                    event.preventDefault()
                    pointerIdRef.current = event.pointerId
                    if (typeof event.currentTarget.setPointerCapture === 'function') {
                      event.currentTarget.setPointerCapture(event.pointerId)
                      pointerCaptureRef.current = event.currentTarget
                    }
                    setDraggingIndex(index)
                  }}
                />
              </Box>
            ))}
          </Stack>
        </Box>
      </Stack>
    </Paper>
  )
}
