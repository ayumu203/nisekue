import { Button, MenuItem, Stack, TextField } from '@mui/material'
import { useState } from 'react'
import { changeDebugJob } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { greenOutlinedInputSx, softGreenButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import type { GetPlayerResponse, PlayerJobCode } from '@/schema/player'
import locale from '../../../locale/debug/Debug.json'

type DebugJobChangeSectionProps = {
  player: GetPlayerResponse
  accessToken: string
  onChanged: () => Promise<void>
}

export default function DebugJobChangeSection({ player, accessToken, onChanged }: DebugJobChangeSectionProps) {
  const [jobCode, setJobCode] = useState<PlayerJobCode | ''>('')
  const { isRunning, message, run } = useDebugAction()
  const candidates = player.jobProfiles.filter((job) => job.code !== 'Apprentice' && job.code !== player.job.code)
  const selected = candidates.find((job) => job.code === jobCode)

  async function handleChange(): Promise<void> {
    if (!selected) {
      return
    }

    await run(
      async () => {
        await changeDebugJob({ job: selected.code }, accessToken)
        setJobCode('')
        await onChanged()
      },
      locale.jobChange.done.replace('{name}', selected.displayName),
    )
  }

  return (
    <DebugSection title={locale.jobChange.title} description={locale.jobChange.description} message={message}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <TextField
          select
          label={locale.jobChange.label}
          size="small"
          value={jobCode}
          onChange={(event) => setJobCode(event.target.value as PlayerJobCode)}
          sx={{ ...greenOutlinedInputSx, minWidth: 240 }}
        >
          {candidates.map((job) => (
            <MenuItem key={job.code} value={job.code}>
              {job.displayName}
            </MenuItem>
          ))}
        </TextField>
        <Button
          variant="contained"
          sx={softGreenButtonSx}
          onClick={() => void handleChange()}
          disabled={!selected || isRunning}
        >
          {locale.jobChange.submit}
        </Button>
      </Stack>
    </DebugSection>
  )
}
