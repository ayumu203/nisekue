import { Button, Stack, TextField, Typography } from '@mui/material'
import { useState } from 'react'
import { levelUpDebugPlayer } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { useDebugAction } from '@/hooks/useDebugAction'
import { debugLevelUpRequestSchema } from '@/schema/debug'
import type { DebugLevelUpResponse } from '@/schema/debug'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/debug/Debug.json'

type DebugLevelSectionProps = {
  player: GetPlayerResponse
  accessToken: string
  onChanged: () => Promise<void>
}

const MAX_LEVEL = 100

function describeResult(result: DebugLevelUpResponse): string {
  const parts = [locale.level.done.replace('{level}', String(result.level))]
  if (result.newlyLearnedMoveIds.length > 0) {
    parts.push(locale.level.learned.replace('{count}', String(result.newlyLearnedMoveIds.length)))
  }
  if (result.hasMasteredCurrentJob) {
    parts.push(locale.level.mastered)
  }
  return parts.join(' ')
}

export default function DebugLevelSection({ player, accessToken, onChanged }: DebugLevelSectionProps) {
  const [targetLevel, setTargetLevel] = useState(String(Math.min(player.level + 1, MAX_LEVEL)))
  const { isRunning, message, run } = useDebugAction()

  async function levelUpTo(level: number): Promise<void> {
    let result: DebugLevelUpResponse | null = null
    await run(
      async () => {
        const parsed = debugLevelUpRequestSchema.safeParse({ level })
        if (!parsed.success) {
          throw new Error(parsed.error.issues[0]?.message)
        }

        result = await levelUpDebugPlayer(parsed.data, accessToken)
        await onChanged()
      },
      () => (result ? describeResult(result) : ''),
    )
  }

  return (
    <DebugSection title={locale.level.title} description={locale.level.description} message={message}>
      <Typography variant="body2">
        {locale.level.current.replace('{level}', String(player.level)).replace('{job}', player.job.displayName)}
      </Typography>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <TextField
          label={locale.level.label}
          type="number"
          size="small"
          value={targetLevel}
          onChange={(event) => setTargetLevel(event.target.value)}
          slotProps={{ htmlInput: { min: player.level, max: MAX_LEVEL, step: 1 } }}
          sx={{ width: 160 }}
        />
        <Button variant="contained" onClick={() => void levelUpTo(Number(targetLevel))} disabled={isRunning}>
          {locale.level.submit}
        </Button>
        <Button
          variant="outlined"
          onClick={() => void levelUpTo(MAX_LEVEL)}
          disabled={isRunning || player.level >= MAX_LEVEL}
        >
          {locale.level.max}
        </Button>
      </Stack>
    </DebugSection>
  )
}
