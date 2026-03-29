import { Paper, Stack } from '@mui/material'
import type { ReactNode } from 'react'
import { innerSurfaceSx } from '@/constants/styles'

type ThreadBoardSurfaceProps = {
  children: ReactNode
}

export default function ThreadBoardSurface({ children }: ThreadBoardSurfaceProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: { xs: 1.75, sm: 2.25 },
        color: '#432f22',
        backgroundColor: '#d7c4a8',
        borderColor: '#94704c',
      }}
    >
      <Stack spacing={2}>{children}</Stack>
    </Paper>
  )
}
