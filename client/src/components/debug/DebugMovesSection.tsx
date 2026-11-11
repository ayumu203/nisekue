import { Autocomplete, Box, Button, MenuItem, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { clearDebugMoveSlot, getDebugMoves, learnDebugMove } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { useDebugAction } from '@/hooks/useDebugAction'
import type { DebugMoveListResponse } from '@/schema/debug'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/debug/Debug.json'

type DebugMovesSectionProps = {
  player: GetPlayerResponse
  accessToken: string
  onChanged: () => Promise<void>
}

type MoveOption = DebugMoveListResponse[number]

const AUTO_SLOT = 'auto'

export default function DebugMovesSection({ player, accessToken, onChanged }: DebugMovesSectionProps) {
  const { data: moves = [], isLoading } = useSWR(['debug-moves'], () => getDebugMoves(accessToken), {
    revalidateOnFocus: false,
  })
  const [selectedMove, setSelectedMove] = useState<MoveOption | null>(null)
  const [slot, setSlot] = useState<string>(AUTO_SLOT)
  const { isRunning, message, run } = useDebugAction()

  async function handleLearn(): Promise<void> {
    if (!selectedMove) {
      return
    }

    await run(
      async () => {
        await learnDebugMove(
          { moveId: selectedMove.moveId, slotIndex: slot === AUTO_SLOT ? null : Number(slot) },
          accessToken,
        )
        await onChanged()
      },
      locale.moves.learned.replace('{name}', selectedMove.name),
    )
  }

  async function handleClear(slotIndex: number): Promise<void> {
    await run(async () => {
      await clearDebugMoveSlot(slotIndex, accessToken)
      await onChanged()
    }, locale.moves.cleared)
  }

  return (
    <DebugSection title={locale.moves.title} description={locale.moves.description} message={message}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <Autocomplete
          options={moves}
          loading={isLoading}
          value={selectedMove}
          onChange={(_, value) => setSelectedMove(value)}
          getOptionLabel={(option) => `${option.moveId}: ${option.name}`}
          isOptionEqualToValue={(option, value) => option.moveId === value.moveId}
          renderInput={(params) => <TextField {...params} label={locale.moves.moveLabel} size="small" />}
          sx={{ flex: 1, minWidth: 240 }}
        />
        <TextField
          select
          label={locale.moves.slotLabel}
          size="small"
          value={slot}
          onChange={(event) => setSlot(event.target.value)}
          sx={{ minWidth: 160 }}
        >
          <MenuItem value={AUTO_SLOT}>{locale.moves.autoSlot}</MenuItem>
          {player.moveSlots.map((moveSlot) => (
            <MenuItem key={moveSlot.slot} value={String(moveSlot.slot - 1)}>
              {locale.moves.slot.replace('{slot}', String(moveSlot.slot))}
            </MenuItem>
          ))}
        </TextField>
        <Button variant="contained" onClick={() => void handleLearn()} disabled={!selectedMove || isRunning}>
          {locale.moves.submit}
        </Button>
      </Stack>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', sm: '1fr 1fr' }, gap: 1 }}>
        {player.moveSlots.map((moveSlot) => (
          <Stack key={moveSlot.slot} direction="row" alignItems="center" spacing={1}>
            <Typography variant="body2" sx={{ flex: 1 }}>
              {locale.moves.slot.replace('{slot}', String(moveSlot.slot))}: {moveSlot.moveName ?? locale.moves.empty}
            </Typography>
            {moveSlot.moveId !== null && (
              <Button size="small" onClick={() => void handleClear(moveSlot.slot - 1)} disabled={isRunning}>
                {locale.moves.clear}
              </Button>
            )}
          </Stack>
        ))}
      </Box>
    </DebugSection>
  )
}
