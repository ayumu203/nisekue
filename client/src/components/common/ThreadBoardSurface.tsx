import { Paper, Stack } from '@mui/material'
import type { ReactNode } from 'react'
import { innerSurfaceSx } from '@/constants/styles'
import { palette } from '@/theme/tokens'

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
        p: { xs: 2, sm: 2.5 },
        color: palette.brown900,
        backgroundColor: palette.brown200,
        borderColor: palette.brown500,
      }}
    >
      <Stack spacing={2}>{children}</Stack>
    </Paper>
  )
}
