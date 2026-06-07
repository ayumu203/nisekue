import { Box, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import type { JobRoadmapNode } from '@/schema/player'
import JobRoadmapNodeComponent from './JobRoadmapNodeComponent'
import locale from '../../../locale/player-job/JobChange.json'

interface JobRoadmapTreeProps {
  roadmap: JobRoadmapNode | null
  isLoading: boolean
  error: string | null
  canUnlockTarget: boolean
  onUnlockTarget: () => void
}

export default function JobRoadmapTree({
  roadmap,
  isLoading,
  error,
  canUnlockTarget,
  onUnlockTarget,
}: JobRoadmapTreeProps) {
  if (isLoading) {
    return (
      <Stack direction="row" spacing={1} alignItems="center" sx={{ p: 3 }}>
        <CircularProgress size={16} sx={{ color: '#8f6b2f' }} />
        <Typography variant="body2" color="#6a4b35">
          {locale.loadingPlayer}
        </Typography>
      </Stack>
    )
  }

  if (error) {
    return (
      <Box sx={{ p: 2 }}>
        <Paper
          variant="outlined"
          sx={{
            p: 2,
            borderColor: '#d47a70',
            bgcolor: '#fff5f4',
            borderRadius: 1.5,
          }}
        >
          <Typography variant="body2" color="#9f2f24">
            {error}
          </Typography>
        </Paper>
      </Box>
    )
  }

  if (!roadmap) {
    return (
      <Box
        sx={{
          p: 4,
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 1,
        }}
      >
        <Typography variant="body1" sx={{ color: '#b0a070', fontWeight: 700 }}>
          ←
        </Typography>
        <Typography variant="body2" color="#8a7a5a" textAlign="center">
          {locale.roadmapSelectHint}
        </Typography>
      </Box>
    )
  }

  return (
    <Box sx={{ p: 2 }}>
      <JobRoadmapNodeComponent node={roadmap} canUnlockTarget={canUnlockTarget} onUnlockTarget={onUnlockTarget} />
    </Box>
  )
}
