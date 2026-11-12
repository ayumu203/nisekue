import { Box, Button, TextField } from '@mui/material'
import { useState } from 'react'
import { updateDebugStatus } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { greenOutlinedInputSx, softGoldButtonSx, softGreenButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import { debugUpdateStatusRequestSchema } from '@/schema/debug'
import type { DebugUpdateStatusRequest } from '@/schema/debug'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/debug/Debug.json'

type DebugStatusSectionProps = {
  player: GetPlayerResponse
  accessToken: string
  onChanged: () => Promise<void>
}

const statusKeys = ['maxHp', 'maxMp', 'strength', 'defense', 'intelligence', 'luck', 'speed'] as const

function toForm(player: GetPlayerResponse): Record<(typeof statusKeys)[number], string> {
  const base = player.baseStatus
  return {
    maxHp: String(base.maxHp),
    maxMp: String(base.maxMp),
    strength: String(base.strength),
    defense: String(base.defense),
    intelligence: String(base.intelligence),
    luck: String(base.luck),
    speed: String(base.speed),
  }
}

export default function DebugStatusSection({ player, accessToken, onChanged }: DebugStatusSectionProps) {
  const [form, setForm] = useState(() => toForm(player))
  const { isRunning, message, run } = useDebugAction()

  async function handleSubmit(): Promise<void> {
    await run(async () => {
      const parsed = debugUpdateStatusRequestSchema.safeParse(
        Object.fromEntries(statusKeys.map((key) => [key, Number(form[key])])) as DebugUpdateStatusRequest,
      )
      if (!parsed.success) {
        throw new Error(parsed.error.issues[0]?.message ?? locale.status.invalid)
      }

      await updateDebugStatus(parsed.data, accessToken)
      await onChanged()
    }, locale.status.done)
  }

  return (
    <DebugSection title={locale.status.title} description={locale.status.description} message={message}>
      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(4, 1fr)' }, gap: 1.5 }}>
        {statusKeys.map((key) => (
          <TextField
            key={key}
            label={locale.status.labels[key]}
            type="number"
            size="small"
            value={form[key]}
            onChange={(event) => setForm((current) => ({ ...current, [key]: event.target.value }))}
            slotProps={{ htmlInput: { min: key === 'maxHp' ? 1 : 0, step: 1 } }}
            sx={greenOutlinedInputSx}
          />
        ))}
      </Box>
      <Box sx={{ display: 'flex', gap: 1 }}>
        <Button variant="contained" sx={softGreenButtonSx} onClick={() => void handleSubmit()} disabled={isRunning}>
          {locale.status.submit}
        </Button>
        <Button variant="contained" sx={softGoldButtonSx} onClick={() => setForm(toForm(player))} disabled={isRunning}>
          {locale.status.reset}
        </Button>
      </Box>
    </DebugSection>
  )
}
