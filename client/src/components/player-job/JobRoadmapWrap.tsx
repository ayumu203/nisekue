import { Paper, Stack, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import locale from '../../../locale/player-job/JobChange.json'

interface JobRoadmapWrapProps {
  children: ReactNode
}

export default function JobRoadmapWrap({ children }: JobRoadmapWrapProps) {
  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 3,
        p: { xs: 2, sm: 2.5 },
        backgroundColor: '#44644a',
        borderColor: '#b8ab7a',
      }}
    >
      <Stack spacing={2}>
        <Stack spacing={0.25}>
          <Typography
            variant="overline"
            sx={{ letterSpacing: '0.16em', color: 'rgba(243, 238, 220, 0.72)' }}
          >
            ROADMAP
          </Typography>
          <Typography variant="h4" fontWeight={900} lineHeight={1.1} color="#fff8ea">
            {locale.roadmapTitle}
          </Typography>
        </Stack>
        {children}
      </Stack>
    </Paper>
  )
}
