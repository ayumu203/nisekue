import { Alert, Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { innerSurfaceSx } from '@/constants/styles'

type DebugSectionProps = {
  title: string
  description?: string
  message?: { severity: 'success' | 'error'; text: string } | null
  children: ReactNode
}

/** デバッグ画面の 1 機能分の枠 */
export default function DebugSection({ title, description, message, children }: DebugSectionProps) {
  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
      <Stack spacing={1.5}>
        <Typography variant="h6" component="h2" sx={{ fontWeight: 700 }}>
          {title}
        </Typography>
        {description && <Typography variant="body2">{description}</Typography>}
        {children}
        {message && <Alert severity={message.severity}>{message.text}</Alert>}
      </Stack>
    </Paper>
  )
}
