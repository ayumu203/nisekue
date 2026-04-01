import { Box, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { deepGreen, framedPanelSx } from './ItemsConstants'

export function ControlFrame({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        p: { xs: 1.5, sm: 2 },
      }}
    >
      {children}
    </Box>
  )
}

export function PageFrame({ children }: { children: ReactNode }) {
  return (
    <Box
      sx={{
        ...framedPanelSx,
        p: { xs: 1.5, sm: 2 },
        background: deepGreen,
      }}
    >
      <Stack spacing={2}>{children}</Stack>
    </Box>
  )
}

export function SectionFrame({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <Box
      sx={{
        px: { xs: 0.25, sm: 0.5 },
        py: 0.25,
      }}
    >
      <Stack spacing={2}>
        <Stack
          direction={{ xs: 'column', sm: 'row' }}
          justifyContent="space-between"
          spacing={1}
          alignItems={{ sm: 'center' }}
        >
          <Typography variant="h5" fontWeight={900} color="#ffffff">
            {title}
          </Typography>
          {subtitle ? (
            <Typography variant="body2" color="rgba(255,255,255,0.76)">
              {subtitle}
            </Typography>
          ) : null}
        </Stack>
        {children}
      </Stack>
    </Box>
  )
}
