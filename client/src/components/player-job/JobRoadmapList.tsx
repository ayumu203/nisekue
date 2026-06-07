import {
  Accordion,
  AccordionDetails,
  AccordionSummary,
  Box,
  Chip,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import LockIcon from '@mui/icons-material/Lock'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import type { JobRoadmapListEntry } from '@/schema/player'
import { resolveJobAssetPath } from '@/lib/assets'
import locale from '../../../locale/player-job/JobChange.json'

interface JobRoadmapListProps {
  entries: JobRoadmapListEntry[]
  isLoading: boolean
  error: string | null
  selectedJobId: number | null
  onSelect: (jobId: number) => void
}

export default function JobRoadmapList({ entries, isLoading, error, selectedJobId, onSelect }: JobRoadmapListProps) {
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
      <Typography variant="body2" color="error" sx={{ p: 2 }}>
        {error}
      </Typography>
    )
  }

  const groupedByRank = new Map<number, JobRoadmapListEntry[]>()
  for (const entry of entries) {
    const list = groupedByRank.get(entry.rank) ?? []
    list.push(entry)
    groupedByRank.set(entry.rank, list)
  }

  const sortedRanks = [...groupedByRank.keys()].sort((a, b) => a - b)

  return (
    <Stack spacing={1} sx={{ p: 1 }}>
      {sortedRanks.map((rank) => {
        const jobs = groupedByRank.get(rank)!
        const goldCost = jobs[0]?.goldCostToUnlock ?? 0

        return (
          <Accordion key={rank} defaultExpanded disableGutters>
            <AccordionSummary expandIcon={<ExpandMoreIcon />}>
              <Stack direction="row" spacing={1} alignItems="center">
                <Typography variant="subtitle2" fontWeight={700}>
                  Rank {rank}
                </Typography>
                <Typography variant="caption" color="text.secondary">
                  {locale.roadmapRankCostHeader.replace('{{gold}}', goldCost.toLocaleString())}
                </Typography>
              </Stack>
            </AccordionSummary>
            <AccordionDetails sx={{ p: 0.5 }}>
              <Stack spacing={0.5}>
                {jobs.map((job) => {
                  const isSelected = job.jobId === selectedJobId
                  const imageSrc = resolveJobAssetPath(job.jobCode)

                  return (
                    <Paper
                      key={job.jobId}
                      variant="outlined"
                      onClick={() => onSelect(job.jobId)}
                      sx={{
                        p: 1,
                        cursor: 'pointer',
                        borderColor: isSelected ? '#4a6c51' : '#d2c08b',
                        bgcolor: isSelected ? 'rgba(74, 108, 81, 0.08)' : 'rgba(255, 249, 232, 0.92)',
                        '&:hover': {
                          bgcolor: isSelected
                            ? 'rgba(74, 108, 81, 0.12)'
                            : 'rgba(255, 249, 232, 0.98)',
                        },
                      }}
                    >
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Box
                          sx={{
                            width: 32,
                            height: 32,
                            borderRadius: 1.5,
                            border: '1.5px solid',
                            borderColor: job.isUnlocked ? '#4a6c51' : '#b0a581',
                            bgcolor: job.isUnlocked ? '#e8f0e3' : '#f4f0e3',
                            display: 'grid',
                            placeItems: 'center',
                            opacity: job.isUnlocked ? 1 : 0.55,
                            flexShrink: 0,
                          }}
                        >
                          {imageSrc ? (
                            <Box
                              component="img"
                              src={imageSrc}
                              alt={job.jobName}
                              sx={{ width: 24, height: 24, objectFit: 'contain' }}
                            />
                          ) : (
                            <Typography variant="caption" fontWeight={700}>
                              {job.jobName.charAt(0)}
                            </Typography>
                          )}
                        </Box>
                        <Typography variant="body2" fontWeight={600} sx={{ flex: 1 }}>
                          {job.jobName}
                        </Typography>
                        <Stack direction="row" spacing={0.5} alignItems="center">
                          {job.isUnlocked ? (
                            <Chip
                              icon={<CheckCircleIcon sx={{ fontSize: 14 }} />}
                              label={locale.roadmapUnlocked}
                              size="small"
                              sx={{
                                fontSize: '0.65rem',
                                height: 22,
                                bgcolor: 'rgba(74, 108, 81, 0.12)',
                                color: '#324c36',
                              }}
                            />
                          ) : (
                            <LockIcon sx={{ color: '#b0a581', fontSize: 16 }} />
                          )}
                        </Stack>
                      </Stack>
                    </Paper>
                  )
                })}
              </Stack>
            </AccordionDetails>
          </Accordion>
        )
      })}
    </Stack>
  )
}
