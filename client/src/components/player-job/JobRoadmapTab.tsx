import { Alert, Box, Button, Snackbar, useMediaQuery, useTheme } from '@mui/material'
import ArrowBackIcon from '@mui/icons-material/ArrowBack'
import { useState } from 'react'
import useSWR from 'swr'
import { getJobRoadmapList, getJobRoadmap, unlockJobRoadmap } from '@/api/player'
import { useAuth } from '@/contexts/useAuth'
import type { JobRoadmapListEntry } from '@/schema/player'
import { innerSurfaceSx } from '@/constants/styles'
import JobRoadmapList from './JobRoadmapList'
import JobRoadmapTree from './JobRoadmapTree'
import JobRoadmapWrap from './JobRoadmapWrap'
import UnlockRoadmapDialog from './UnlockRoadmapDialog'
import locale from '../../../locale/player-job/JobChange.json'

export default function JobRoadmapTab() {
  const { session } = useAuth()
  const accessToken = session?.access_token ?? null
  const [selectedJobId, setSelectedJobId] = useState<number | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('md'))
  const [mobileView, setMobileView] = useState<'list' | 'tree'>('list')

  const {
    data: roadmapList,
    error: listError,
    isLoading: isListLoading,
    mutate: mutateList,
  } = useSWR(accessToken ? ['job-roadmap-list'] : null, () => getJobRoadmapList(accessToken!))

  const {
    data: roadmapTree,
    error: treeError,
    isLoading: isTreeLoading,
    mutate: mutateTree,
  } = useSWR(
    accessToken && selectedJobId ? ['job-roadmap', selectedJobId] : null,
    () => getJobRoadmap(selectedJobId!, accessToken!),
    { revalidateOnFocus: false },
  )

  const selectedEntry: JobRoadmapListEntry | undefined = roadmapList?.find((e) => e.jobId === selectedJobId)

  function handleSelect(jobId: number) {
    setSelectedJobId(jobId)
    if (isMobile) setMobileView('tree')
  }

  async function handleUnlockClick() {
    if (!accessToken || !selectedJobId) return
    setDialogOpen(true)
  }

  async function handleUnlockConfirm() {
    if (!accessToken || !selectedJobId) return
    setDialogOpen(false)
    setSubmitError(null)

    try {
      const result = await unlockJobRoadmap({ jobId: selectedJobId }, accessToken)
      await mutateList()
      await mutateTree()
      setSuccessMessage(
        locale.roadmapUnlockSuccess.replace('{{jobName}}', result.jobName),
      )
    } catch (error) {
      setSubmitError(
        error instanceof Error ? error.message : locale.roadmapUnlockFailed,
      )
    }
  }

  const targetGoldCost = selectedEntry?.goldCostToUnlock ?? 0

  return (
    <Box>
      <JobRoadmapWrap>
      {submitError && (
        <Alert severity="error" sx={{ borderRadius: 1.5 }}>
          {submitError}
        </Alert>
      )}

{isMobile ? (
        <Box
          sx={{
            border: '2px solid #d2c08b',
            borderRadius: 2,
            overflow: 'hidden',
            ...innerSurfaceSx,
          }}
        >
          {mobileView === 'list' ? (
            <Box sx={{ overflowY: 'auto', maxHeight: 'calc(100vh - 300px)' }}>
              <JobRoadmapList
                entries={roadmapList ?? []}
                isLoading={isListLoading}
                error={listError?.message ?? null}
                selectedJobId={selectedJobId}
                onSelect={handleSelect}
              />
            </Box>
          ) : (
            <>
              <Box sx={{ px: 1, pt: 0.75, pb: 0 }}>
                <Button
                  size="small"
                  startIcon={<ArrowBackIcon />}
                  onClick={() => setMobileView('list')}
                  sx={{
                    color: '#5a3a10',
                    fontWeight: 900,
                    fontSize: '0.7rem',
                    minWidth: 0,
                    px: 1,
                    py: 0.25,
                    bgcolor: 'rgba(90,58,16,0.08)',
                    borderRadius: 1,
                    '&:hover': { bgcolor: 'rgba(90,58,16,0.15)' },
                  }}
                >
                  {locale.roadmapBackToList}
                </Button>
              </Box>
              <Box sx={{ overflowY: 'auto', maxHeight: 'calc(100vh - 300px)' }}>
                <JobRoadmapTree
                  roadmap={roadmapTree ?? null}
                  isLoading={isTreeLoading && !roadmapTree}
                  error={treeError?.message ?? null}
                  canUnlockTarget={selectedEntry?.canUnlock ?? false}
                  onUnlockTarget={handleUnlockClick}
                />
              </Box>
            </>
          )}
        </Box>
      ) : (
        <Box
          sx={{
            display: 'flex',
            border: '2px solid #d2c08b',
            borderRadius: 2,
            overflow: 'hidden',
            ...innerSurfaceSx,
          }}
        >
          <Box
            sx={{
              width: 260,
              minWidth: 260,
              borderRight: '2px solid #d2c08b',
              overflowY: 'auto',
              maxHeight: 'calc(100vh - 340px)',
            }}
          >
            <JobRoadmapList
              entries={roadmapList ?? []}
              isLoading={isListLoading}
              error={listError?.message ?? null}
              selectedJobId={selectedJobId}
              onSelect={setSelectedJobId}
            />
          </Box>

          <Box
            sx={{
              flex: 1,
              overflowY: 'auto',
              maxHeight: 'calc(100vh - 340px)',
            }}
          >
            <JobRoadmapTree
              roadmap={roadmapTree ?? null}
              isLoading={isTreeLoading && !roadmapTree}
              error={treeError?.message ?? null}
              canUnlockTarget={selectedEntry?.canUnlock ?? false}
              onUnlockTarget={handleUnlockClick}
            />
          </Box>
        </Box>
      )}
      </JobRoadmapWrap>

      <UnlockRoadmapDialog
        open={dialogOpen}
        jobName={selectedEntry?.jobName ?? ''}
        goldCost={targetGoldCost}
        remainingGold={0}
        onClose={() => setDialogOpen(false)}
        onConfirm={handleUnlockConfirm}
      />
      <Snackbar
        open={successMessage !== null}
        autoHideDuration={3000}
        onClose={() => setSuccessMessage(null)}
        message={successMessage}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      />
    </Box>
  )
}
