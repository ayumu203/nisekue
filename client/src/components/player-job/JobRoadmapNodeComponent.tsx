import {
  Box,
  Button,
  Chip,
  Stack,
  Typography,
} from '@mui/material'
import LockIcon from '@mui/icons-material/Lock'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import type { JobRoadmapNode } from '@/schema/player'
import { resolveJobAssetPath } from '@/lib/assets'
import locale from '../../../locale/player-job/JobChange.json'

interface JobRoadmapNodeComponentProps {
  node: JobRoadmapNode
  canUnlockTarget: boolean
  onUnlockTarget: () => void
  depth?: number
}

export default function JobRoadmapNodeComponent({
  node,
  canUnlockTarget,
  onUnlockTarget,
  depth = 0,
}: JobRoadmapNodeComponentProps) {
  const isTarget = depth === 0

  if (node.type === 'job') {
    const imageSrc = resolveJobAssetPath(node.jobCode)
    return (
      <Box sx={{ ml: depth > 0 ? 2 : 0, position: 'relative' }}>
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ py: 0.5 }}>
          {depth > 0 && (
            <Box
              sx={{
                position: 'absolute',
                left: -10,
                top: -6,
                bottom: 0,
                width: 2,
                bgcolor: '#cbb783',
              }}
            />
          )}
          <Box
            sx={{
              width: 36,
              height: 36,
              borderRadius: 1.5,
              border: '1.5px solid',
              borderColor: node.isUnlocked ? '#4a6c51' : '#b0a581',
              bgcolor: node.isUnlocked ? '#e8f0e3' : '#f4f0e3',
              display: 'grid',
              placeItems: 'center',
              opacity: node.isUnlocked ? 1 : 0.55,
              flexShrink: 0,
            }}
          >
            {imageSrc ? (
              <Box
                component="img"
                src={imageSrc}
                alt={node.jobName}
                sx={{ width: 28, height: 28, objectFit: 'contain' }}
              />
            ) : (
              <Typography variant="caption" fontWeight={700}>
                {node.jobName?.charAt(0)}
              </Typography>
            )}
          </Box>
          <Stack spacing={0} minWidth={0}>
            <Typography variant="body2" fontWeight={700} color={node.isUnlocked ? '#324c36' : '#8a7d5d'}>
              {node.jobName}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {locale.roadmapRankLabel.replace('{{rank}}', String(node.rank))}
            </Typography>
          </Stack>
          {node.isUnlocked ? (
            <CheckCircleIcon sx={{ color: '#4a6c51', fontSize: 18, flexShrink: 0 }} />
          ) : (
            <Stack direction="row" spacing={0.5} alignItems="center" sx={{ ml: 'auto', flexShrink: 0 }}>
              <LockIcon sx={{ color: '#b0a581', fontSize: 16 }} />
              {isTarget && node.goldCostToUnlock != null && (
                <Button
                  size="small"
                  variant="outlined"
                  disabled={!canUnlockTarget}
                  onClick={onUnlockTarget}
                  sx={{
                    fontSize: '0.7rem',
                    px: 1,
                    py: 0.25,
                    minWidth: 0,
                    whiteSpace: 'nowrap',
                    borderColor: '#cbb783',
                    color: '#6c6246',
                    '&.Mui-disabled': {
                      borderColor: '#d5c49a',
                      color: '#b0a581',
                    },
                  }}
                >
                  {locale.roadmapUnlockButton.replace('{{gold}}', node.goldCostToUnlock.toLocaleString())}
                </Button>
              )}
              {!canUnlockTarget && isTarget && (
                <Typography variant="caption" color="#b0a581">
                  {locale.roadmapPrerequisiteLocked}
                </Typography>
              )}
            </Stack>
          )}
        </Stack>
        {node.requirements.map((child, idx) => (
          <JobRoadmapNodeComponent
            key={`${child.type}-${child.type === 'job' ? child.jobId : child.itemId}-${idx}`}
            node={child}
            canUnlockTarget={false}
            onUnlockTarget={() => {}}
            depth={depth + 1}
          />
        ))}
      </Box>
    )
  }

  return (
    <Box sx={{ ml: depth > 0 ? 2 : 0, position: 'relative' }}>
      <Stack direction="row" spacing={1.5} alignItems="center" sx={{ py: 0.5 }}>
        {depth > 0 && (
          <Box
            sx={{
              position: 'absolute',
              left: -10,
              top: -6,
              bottom: 0,
              width: 2,
              bgcolor: '#cbb783',
            }}
          />
        )}
        <Box
          sx={{
            width: 36,
            height: 36,
            borderRadius: 1,
            border: '1.5px dashed',
            borderColor: '#b0a581',
            bgcolor: '#faf6ed',
            display: 'grid',
            placeItems: 'center',
            flexShrink: 0,
          }}
        >
          <Typography variant="caption" fontWeight={700} color="#6c6246">
            I
          </Typography>
        </Box>
        <Stack spacing={0} minWidth={0}>
          <Typography variant="body2" fontWeight={600} color="#5c5440">
            {node.itemName}
          </Typography>
          {node.stages && node.stages.length > 0 && (
            <Stack spacing={0.25}>
              <Typography variant="caption" color="text.secondary" fontWeight={600}>
                {locale.roadmapItemStage}
              </Typography>
              {node.stages.map((stage) => (
                <Chip
                  key={stage.id}
                  label={stage.name}
                  size="small"
                  variant="outlined"
                  sx={{ width: 'fit-content', fontSize: '0.65rem', height: 20 }}
                />
              ))}
            </Stack>
          )}
          {node.requiredMasterJobs && node.requiredMasterJobs.length > 0 && (
            <Stack spacing={0.25} sx={{ mt: 0.5 }}>
              <Typography variant="caption" color="text.secondary" fontWeight={600}>
                {locale.roadmapItemRequiredJobs}
              </Typography>
              {node.requiredMasterJobs.map((job) => (
                <Chip
                  key={job.id}
                  label={job.name}
                  size="small"
                  variant="outlined"
                  sx={{ width: 'fit-content', fontSize: '0.65rem', height: 20 }}
                />
              ))}
            </Stack>
          )}
        </Stack>
      </Stack>
      {node.requirements.map((child, idx) => (
        <JobRoadmapNodeComponent
          key={`${child.type}-${child.type === 'job' ? child.jobId : child.itemId}-${idx}`}
          node={child}
          canUnlockTarget={false}
          onUnlockTarget={() => {}}
          depth={depth + 1}
        />
      ))}
    </Box>
  )
}
