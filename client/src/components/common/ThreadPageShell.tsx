import { Container, Paper, Stack } from '@mui/material'
import type { ReactNode } from 'react'
import { outerPagePaperSx } from '@/constants/styles'

type ThreadPageShellProps = {
  children: ReactNode
}

export default function ThreadPageShell({ children }: ThreadPageShellProps) {
  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper
        elevation={2}
        sx={{
          ...outerPagePaperSx,
          backgroundColor: '#b08a52',
          borderColor: '#876338',
        }}
      >
        <Stack spacing={2.5}>{children}</Stack>
      </Paper>
    </Container>
  )
}
