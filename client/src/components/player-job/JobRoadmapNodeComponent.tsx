import { Box, Chip, Stack, Typography } from '@mui/material'
import LockIcon from '@mui/icons-material/Lock'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import type { JobRoadmapNode } from '@/schema/player'
import { resolveJobAssetPath } from '@/lib/assets'
import { softGoldButtonSx } from '@/constants/styles'
import { Button } from '@mui/material'
import { JobChangeIllustration } from '@/components/items/ItemIllustrations'
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
      <Box sx={{ ml: depth > 0 ? 3 : 0, position: 'relative' }}>
        {depth > 0 && (
          <Box
            sx={{
              position: 'absolute',
              left: -16,
              top: 0,
              bottom: 0,
              width: 2,
              bgcolor: '#cbb783',
            }}
          />
        )}
        {depth > 0 && (
          <Box
            sx={{
              position: 'absolute',
              left: -16,
              top: 18,
              width: 14,
              height: 2,
              bgcolor: '#cbb783',
            }}
          />
        )}

        <Box
          sx={{
            border: '2px solid',
            borderColor: isTarget ? (node.isUnlocked ? '#4a6c51' : '#8f6b2f') : node.isUnlocked ? '#a0c4a3' : '#c8b888',
            borderRadius: 1.5,
            p: 1,
            bgcolor: isTarget ? (node.isUnlocked ? '#eaf5eb' : '#fffbe6') : node.isUnlocked ? '#f0f8f1' : '#faf6e8',
            mb: 0.5,
          }}
        >
          <Stack direction="row" spacing={1.5} alignItems="center">
            <Box
              sx={{
                width: isTarget ? 110 : 90,
                height: isTarget ? 110 : 90,
                borderRadius: 1.5,
                border: '2px solid',
                borderColor: node.isUnlocked ? '#4a6c51' : '#b0a070',
                bgcolor: node.isUnlocked ? '#d8edd9' : '#ede8d4',
                display: 'grid',
                placeItems: 'center',
                opacity: node.isUnlocked ? 1 : 0.65,
                flexShrink: 0,
              }}
            >
              {imageSrc ? (
                <Box
                  component="img"
                  src={imageSrc}
                  alt={node.jobName}
                  sx={{ width: isTarget ? 85 : 70, height: isTarget ? 85 : 70, objectFit: 'contain' }}
                />
              ) : (
                <Typography variant="caption" fontWeight={700} color="#4f4638">
                  {node.jobName?.charAt(0)}
                </Typography>
              )}
            </Box>

            <Stack spacing={0} minWidth={0} flex={1}>
              <Typography
                variant={isTarget ? 'body1' : 'body2'}
                fontWeight={700}
                noWrap
                color={node.isUnlocked ? '#2a4a2e' : '#6a5a3a'}
                sx={{ fontSize: { xs: isTarget ? '0.8rem' : '0.75rem', md: isTarget ? '1rem' : '0.875rem' } }}
              >
                {node.jobName}
              </Typography>
              <Typography variant="caption" color="#8a7a5a" noWrap sx={{ fontSize: { xs: '0.62rem', md: '0.75rem' } }}>
                {locale.roadmapRankLabel.replace('{{rank}}', String(node.rank))}
              </Typography>
            </Stack>

            {node.isUnlocked ? (
              <Stack direction="row" spacing={0.5} alignItems="center" sx={{ flexShrink: 0 }}>
                <CheckCircleIcon sx={{ color: '#4a6c51', fontSize: 18 }} />
              </Stack>
            ) : (
              <Stack direction="row" spacing={0.5} alignItems="center" sx={{ flexShrink: 0 }}>
                <LockIcon sx={{ color: '#b0a070', fontSize: 16 }} />
                {isTarget && node.goldCostToUnlock != null && (
                  <Button
                    size="small"
                    variant="contained"
                    disabled={!canUnlockTarget}
                    onClick={onUnlockTarget}
                    sx={{
                      ...softGoldButtonSx['&&'],
                      fontSize: '0.7rem',
                      px: 1.5,
                      py: 0.5,
                      minWidth: 0,
                      whiteSpace: 'nowrap',
                      borderRadius: 1,
                      border: '2px solid',
                      fontWeight: 900,
                      ...(canUnlockTarget
                        ? {}
                        : {
                            backgroundColor: '#ead9a7',
                            borderColor: '#b59a69',
                            color: '#927b56',
                          }),
                    }}
                  >
                    {locale.roadmapUnlockButton.replace('{{gold}}', node.goldCostToUnlock.toLocaleString())}
                  </Button>
                )}
              </Stack>
            )}
          </Stack>

          {!node.isUnlocked && !canUnlockTarget && isTarget && (
            <Typography
              variant="caption"
              color="#b0a070"
              noWrap
              sx={{ display: 'block', mt: 0.5, pl: 0.5, fontSize: { xs: '0.62rem', md: '0.75rem' } }}
            >
              {locale.roadmapPrerequisiteLocked}
            </Typography>
          )}
        </Box>

        {node.requirements.length > 0 && (
          <Box sx={{ ml: 1.5 }}>
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
        )}
      </Box>
    )
  }

  return (
    <Box sx={{ ml: depth > 0 ? 3 : 0, position: 'relative' }}>
      {depth > 0 && (
        <Box
          sx={{
            position: 'absolute',
            left: -16,
            top: 0,
            bottom: 0,
            width: 2,
            bgcolor: '#cbb783',
          }}
        />
      )}
      {depth > 0 && (
        <Box
          sx={{
            position: 'absolute',
            left: -16,
            top: 18,
            width: 14,
            height: 2,
            bgcolor: '#cbb783',
          }}
        />
      )}

      <Box
        sx={{
          border: '2px dashed #c8b080',
          borderRadius: 1.5,
          p: 1,
          bgcolor: '#fdf8ec',
          mb: 0.5,
        }}
      >
        <Stack direction="row" spacing={1.5} alignItems="flex-start">
          <Box
            sx={{
              width: 36,
              height: 36,
              borderRadius: 1,
              border: '2px dashed #c8b080',
              bgcolor: '#f5f0de',
              display: 'grid',
              placeItems: 'center',
              flexShrink: 0,
              mt: 0.25,
            }}
          >
            <JobChangeIllustration sx={{ fontSize: 24, color: '#4f7148' }} />
          </Box>
          <Stack spacing={0.5} minWidth={0} flex={1}>
            <Typography
              variant="body2"
              fontWeight={700}
              color="#5a4a28"
              noWrap
              sx={{ fontSize: { xs: '0.75rem', md: '0.875rem' } }}
            >
              {node.itemName}
            </Typography>
            {node.stages && node.stages.length > 0 && (
              <Stack spacing={0.25}>
                <Typography
                  variant="caption"
                  color="#8a7a5a"
                  fontWeight={700}
                  sx={{ fontSize: { xs: '0.62rem', md: '0.75rem' } }}
                >
                  {locale.roadmapItemStage}
                </Typography>
                <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                  {node.stages.map((stage) => (
                    <Chip
                      key={stage.id}
                      label={stage.name}
                      size="small"
                      sx={{
                        fontSize: '0.65rem',
                        height: 20,
                        bgcolor: '#f2e4b8',
                        border: '1px solid #c8a84a',
                        color: '#5a3a10',
                        fontWeight: 700,
                      }}
                    />
                  ))}
                </Stack>
              </Stack>
            )}
            {node.requiredMasterJobs && node.requiredMasterJobs.length > 0 && (
              <Stack spacing={0.25}>
                <Typography
                  variant="caption"
                  color="#8a7a5a"
                  fontWeight={700}
                  sx={{ fontSize: { xs: '0.62rem', md: '0.75rem' } }}
                >
                  {locale.roadmapItemRequiredJobs}
                </Typography>
                <Stack direction="row" spacing={0.5} flexWrap="wrap" useFlexGap>
                  {node.requiredMasterJobs.map((job) => (
                    <Chip
                      key={job.id}
                      label={job.name}
                      size="small"
                      sx={{
                        fontSize: '0.65rem',
                        height: 20,
                        bgcolor: '#ddeedd',
                        border: '1px solid #7aaa7a',
                        color: '#2a4a2e',
                        fontWeight: 700,
                      }}
                    />
                  ))}
                </Stack>
              </Stack>
            )}
          </Stack>
        </Stack>
      </Box>

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
