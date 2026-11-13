import { Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { grantDebugGold } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { greenOutlinedInputSx, softGreenButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import { debugGrantGoldRequestSchema } from '@/schema/debug'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/debug/Debug.json'

type DebugGoldSectionProps = {
  player: GetPlayerResponse
  accessToken: string
  onChanged: () => Promise<void>
}

export default function DebugGoldSection({ player, accessToken, onChanged }: DebugGoldSectionProps) {
  const [amount, setAmount] = useState('100000')
  const { isRunning, message, run } = useDebugAction()

  async function handleGrant(): Promise<void> {
    let gold = 0
    await run(
      async () => {
        const parsed = debugGrantGoldRequestSchema.safeParse({ amount: Number(amount) })
        if (!parsed.success) {
          throw new Error(parsed.error.issues[0]?.message)
        }

        gold = await grantDebugGold(parsed.data, accessToken)
        await onChanged()
      },
      () => locale.gold.done.replace('{gold}', gold.toLocaleString()),
    )
  }

  return (
    <DebugSection title={locale.gold.title} description={locale.gold.description} message={message}>
      <Typography variant="body2">{locale.gold.current.replace('{gold}', player.gold.toLocaleString())}</Typography>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <TextField
          label={locale.gold.label}
          type="number"
          size="small"
          value={amount}
          onChange={(event) => setAmount(event.target.value)}
          slotProps={{ htmlInput: { min: 1, step: 1 } }}
          sx={{ ...greenOutlinedInputSx, width: 200 }}
        />
        <Button variant="contained" sx={softGreenButtonSx} onClick={() => void handleGrant()} disabled={isRunning}>
          {locale.gold.submit}
        </Button>
      </Stack>
    </DebugSection>
  )
}
