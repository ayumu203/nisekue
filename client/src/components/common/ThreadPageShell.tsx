import { Stack } from '@mui/material'
import type { ReactNode } from 'react'
import GamePageFrame from '@/components/common/GamePageFrame'

type ThreadPageShellProps = {
  children: ReactNode
}

export default function ThreadPageShell({ children }: ThreadPageShellProps) {
  return (
    <GamePageFrame currentKey="threads" currentColor="#876338" background="#b08a52">
      <Stack spacing={2.5}>{children}</Stack>
    </GamePageFrame>
  )
}
