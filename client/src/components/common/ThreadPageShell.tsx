import { Stack } from '@mui/material'
import type { ReactNode } from 'react'
import GamePageShell from '@/components/common/GamePageShell'

type ThreadPageShellProps = {
  children: ReactNode
}

export default function ThreadPageShell({ children }: ThreadPageShellProps) {
  return (
    <GamePageShell background="#b08a52">
      <Stack spacing={2.5}>{children}</Stack>
    </GamePageShell>
  )
}
