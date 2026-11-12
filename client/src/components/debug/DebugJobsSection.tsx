import { Box, Button, Checkbox, FormControlLabel, Stack } from '@mui/material'
import { useState } from 'react'
import { masterDebugJobs } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { softGoldButtonSx, softGreenButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import { debugMasterJobsRequestSchema } from '@/schema/debug'
import type { DebugMasterJobsResponse } from '@/schema/debug'
import type { GetPlayerResponse, PlayerJobCode } from '@/schema/player'
import locale from '../../../locale/debug/Debug.json'

type DebugJobsSectionProps = {
  player: GetPlayerResponse
  accessToken: string
  onChanged: () => Promise<void>
}

function describeResult(result: DebugMasterJobsResponse): string {
  const parts = [locale.jobs.done.replace('{count}', String(result.newlyMasteredJobs.length))]
  if (result.newlyLearnedMoveIds.length > 0) {
    parts.push(locale.jobs.learned.replace('{count}', String(result.newlyLearnedMoveIds.length)))
  }
  return parts.join(' ')
}

export default function DebugJobsSection({ player, accessToken, onChanged }: DebugJobsSectionProps) {
  const [selected, setSelected] = useState<Set<PlayerJobCode>>(new Set())
  const [unlockRoadmap, setUnlockRoadmap] = useState(true)
  const { isRunning, message, run } = useDebugAction()
  const masteredCodes = new Set(player.masteredJobs.map((job) => job.code))

  function toggle(code: PlayerJobCode): void {
    setSelected((current) => {
      const next = new Set(current)
      if (next.has(code)) {
        next.delete(code)
      } else {
        next.add(code)
      }
      return next
    })
  }

  async function master(jobs: PlayerJobCode[]): Promise<void> {
    let result: DebugMasterJobsResponse | null = null
    await run(
      async () => {
        const parsed = debugMasterJobsRequestSchema.safeParse({ jobs, unlockRoadmap })
        if (!parsed.success) {
          throw new Error(parsed.error.issues[0]?.message)
        }

        result = await masterDebugJobs(parsed.data, accessToken)
        await onChanged()
      },
      () => (result ? describeResult(result) : ''),
    )
  }

  return (
    <DebugSection title={locale.jobs.title} description={locale.jobs.description} message={message}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5} alignItems={{ sm: 'center' }}>
        <Button
          variant="contained"
          sx={softGreenButtonSx}
          onClick={() => void master([player.job.code])}
          disabled={isRunning}
        >
          {locale.jobs.currentJob}
        </Button>
        <FormControlLabel
          control={<Checkbox checked={unlockRoadmap} onChange={(event) => setUnlockRoadmap(event.target.checked)} />}
          label={locale.jobs.unlockRoadmap}
        />
      </Stack>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr 1fr', sm: 'repeat(3, 1fr)' }, gap: 0.5 }}>
        {player.jobProfiles.map((job) => {
          const notes = [
            job.code === player.job.code ? locale.jobs.current : null,
            masteredCodes.has(job.code) ? locale.jobs.mastered : null,
          ].filter((note) => note !== null)
          return (
            <FormControlLabel
              key={job.code}
              control={<Checkbox size="small" checked={selected.has(job.code)} onChange={() => toggle(job.code)} />}
              label={notes.length > 0 ? `${job.displayName}（${notes.join('・')}）` : job.displayName}
            />
          )
        })}
      </Box>

      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        <Button
          variant="contained"
          sx={softGreenButtonSx}
          onClick={() => void master([...selected])}
          disabled={isRunning || selected.size === 0}
        >
          {locale.jobs.selected}
        </Button>
        <Button
          variant="contained"
          sx={softGoldButtonSx}
          onClick={() => setSelected(new Set(player.jobProfiles.map((job) => job.code)))}
        >
          {locale.jobs.selectAll}
        </Button>
        <Button variant="contained" sx={softGoldButtonSx} onClick={() => setSelected(new Set())}>
          {locale.jobs.clear}
        </Button>
      </Stack>
    </DebugSection>
  )
}
