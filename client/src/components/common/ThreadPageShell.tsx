import { Stack } from '@mui/material'
import type { ReactNode } from 'react'
import GamePageFrame from '@/components/common/GamePageFrame'
import { palette } from '@/theme/tokens'

type ThreadPageShellProps = {
  children: ReactNode
}

export default function ThreadPageShell({ children }: ThreadPageShellProps) {
  return (
    <GamePageFrame currentKey="threads" currentColor={palette.brown500} background={palette.brown500}>
      <Stack spacing={2.5}>{children}</Stack>
    </GamePageFrame>
  )
}
