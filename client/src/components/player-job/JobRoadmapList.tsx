import {
  Box,
  CircularProgress,
  Paper,
  Stack,
  Typography,
} from '@mui/material'
import LockIcon from '@mui/icons-material/Lock'
import CheckCircleIcon from '@mui/icons-material/CheckCircle'
import { useState } from 'react'
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
  const [expandedRanks, setExpandedRanks] = useState<Set<number>>(new Set())

  if (isLoading) {
    return (
      <Stack direction="row" spacing={1} alignItems="center" sx={{ p: 2 }}>
        <CircularProgress size={16} sx={{ color: '#8f6b2f' }} />
        <Typography variant="body2" color="#6a4b35">{locale.loadingPlayer}</Typography>
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

  const toggleRank = (rank: number) => {
    setExpandedRanks((prev) => {
      const next = new Set(prev)
      if (next.has(rank)) {
        next.delete(rank)
      } else {
        next.add(rank)
      }
      return next
    })
  }

  return (
    <Stack spacing={0.5} sx={{ p: 1 }}>
      {sortedRanks.map((rank) => {
        const jobs = groupedByRank.get(rank)!
        const goldCost = jobs[0]?.goldCostToUnlock ?? 0
        const isExpanded = !expandedRanks.has(rank)
        const unlockedCount = jobs.filter((j) => j.isUnlocked).length

        return (
          <Box key={rank}>
            <Box
              onClick={() => toggleRank(rank)}
              sx={{
                display: 'flex',
                alignItems: 'center',
                justifyContent: 'space-between',
                px: 1.5,
                py: 1,
                cursor: 'pointer',
                background: 'linear-gradient(90deg, #e8c84a 0%, #f2d27a 100%)',
                border: '2px solid #8f6b2f',
                borderRadius: 1,
                userSelect: 'none',
                '&:hover': {
                  background: 'linear-gradient(90deg, #dbb93c 0%, #e8c462 100%)',
                },
              }}
            >
              <Stack direction="row" spacing={1} alignItems="center" minWidth={0}>
                <Typography
                  variant="subtitle2"
                  fontWeight={900}
                  noWrap
                  sx={{ color: '#4a2e0a', letterSpacing: '0.05em', fontSize: { xs: '0.72rem', md: '0.875rem' } }}
                >
                  ★ Rank {rank}
                </Typography>
                <Typography
                  variant="caption"
                  noWrap
                  sx={{ color: '#6a4b1a', fontWeight: 700, fontSize: { xs: '0.62rem', md: '0.75rem' } }}
                >
                  {unlockedCount}/{jobs.length}
                </Typography>
              </Stack>
              <Stack direction="row" spacing={0.5} alignItems="center" flexShrink={0}>
                <Typography
                  variant="caption"
                  noWrap
                  sx={{ color: '#6a4b1a', fontWeight: 700, fontSize: { xs: '0.62rem', md: '0.75rem' } }}
                >
                  {locale.roadmapRankCostHeader.replace('{{gold}}', goldCost.toLocaleString())}
                </Typography>
                <Typography sx={{ color: '#6a4b1a', fontSize: '0.7rem', fontWeight: 900 }}>
                  {isExpanded ? '▲' : '▼'}
                </Typography>
              </Stack>
            </Box>

            {isExpanded && (
              <Stack spacing={0.5} sx={{ mt: 0.5, mb: 0.5 }}>
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
                        borderWidth: 2,
                        borderStyle: 'solid',
                        borderColor: isSelected ? '#4a6c51' : '#d2c08b',
                        bgcolor: isSelected ? 'rgba(74, 108, 81, 0.12)' : '#fffbf0',
                        borderRadius: 1,
                        transition: 'all 0.1s',
                        '&:hover': {
                          borderColor: isSelected ? '#3f5f46' : '#b09050',
                          bgcolor: isSelected ? 'rgba(74, 108, 81, 0.18)' : '#fff7e0',
                        },
                      }}
                    >
                      <Stack direction="row" spacing={1} alignItems="center">
                        <Box
                          sx={{
                            width: 32,
                            height: 32,
                            borderRadius: 1,
                            border: '2px solid',
                            borderColor: job.isUnlocked ? '#4a6c51' : '#b0a581',
                            bgcolor: job.isUnlocked ? '#d8edd9' : '#ede8d4',
                            display: 'grid',
                            placeItems: 'center',
                            opacity: job.isUnlocked ? 1 : 0.6,
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
                            <Typography variant="caption" fontWeight={700} color="#4f4638">
                              {job.jobName.charAt(0)}
                            </Typography>
                          )}
                        </Box>
                        <Typography
                          variant="body2"
                          fontWeight={700}
                          noWrap
                          sx={{
                            flex: 1,
                            minWidth: 0,
                            color: job.isUnlocked ? '#2a4a2e' : '#7a6a50',
                            fontSize: { xs: '0.75rem', md: '0.875rem' },
                          }}
                        >
                          {job.jobName}
                        </Typography>
                        {job.isUnlocked ? (
                          <Stack direction="row" spacing={0.5} alignItems="center">
                            <CheckCircleIcon sx={{ color: '#4a6c51', fontSize: 16 }} />
                            <Typography variant="caption" sx={{ color: '#4a6c51', fontWeight: 700 }}>
                              {locale.roadmapUnlocked}
                            </Typography>
                          </Stack>
                        ) : (
                          <LockIcon sx={{ color: '#b0a070', fontSize: 16, flexShrink: 0 }} />
                        )}
                      </Stack>
                    </Paper>
                  )
                })}
              </Stack>
            )}
          </Box>
        )
      })}
    </Stack>
  )
}
