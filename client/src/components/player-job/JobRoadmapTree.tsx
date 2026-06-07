import { Alert, Box, CircularProgress, Stack, Typography } from '@mui/material'
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
      <Stack direction="row" spacing={1} alignItems="center" sx={{ p: 2 }}>
        <CircularProgress size={16} />
        <Typography variant="body2">{locale.loadingPlayer}</Typography>
      </Stack>
    )
  }

  if (error) {
    return (
      <Alert severity="warning" sx={{ borderRadius: 2 }}>
        {error}
      </Alert>
    )
  }

  if (!roadmap) {
    return (
      <Box sx={{ p: 3, textAlign: 'center' }}>
        <Typography variant="body2" color="text.secondary">
          {locale.roadmapSelectHint}
        </Typography>
      </Box>
    )
  }

  return (
    <Box sx={{ p: 2 }}>
      <JobRoadmapNodeComponent
        node={roadmap}
        canUnlockTarget={canUnlockTarget}
        onUnlockTarget={onUnlockTarget}
      />
    </Box>
  )
}
