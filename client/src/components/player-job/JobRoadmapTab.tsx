import { Alert, Box, Snackbar, Typography } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { getJobRoadmapList, getJobRoadmap, unlockJobRoadmap } from '@/api/player'
import { useAuth } from '@/contexts/useAuth'
import type { JobRoadmapListEntry } from '@/schema/player'
import JobRoadmapList from './JobRoadmapList'
import JobRoadmapTree from './JobRoadmapTree'
import UnlockRoadmapDialog from './UnlockRoadmapDialog'
import locale from '../../../locale/player-job/JobChange.json'

export default function JobRoadmapTab() {
  const { session } = useAuth()
  const accessToken = session?.access_token ?? null
  const [selectedJobId, setSelectedJobId] = useState<number | null>(null)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

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
    <Box sx={{ display: 'flex', height: '100%' }}>
      <Box
        sx={{
          width: 280,
          minWidth: 280,
          borderRight: '1px solid',
          borderColor: '#d2c08b',
          overflowY: 'auto',
          maxHeight: 'calc(100vh - 300px)',
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
      <Box sx={{ flex: 1, overflowY: 'auto', maxHeight: 'calc(100vh - 300px)' }}>
        {submitError && (
          <Alert severity="error" sx={{ mx: 2, mt: 2, borderRadius: 2 }}>
            {submitError}
          </Alert>
        )}
        {selectedEntry && !selectedEntry.isUnlocked ? (
          <Alert severity="info" sx={{ mx: 2, mt: 2, borderRadius: 2 }}>
            <Typography variant="body2">
              {locale.roadmapUnlockButton.replace('{{gold}}', targetGoldCost.toLocaleString())}
            </Typography>
          </Alert>
        ) : null}
        <JobRoadmapTree
          roadmap={roadmapTree ?? null}
          isLoading={isTreeLoading && !roadmapTree}
          error={treeError?.message ?? null}
          canUnlockTarget={selectedEntry?.canUnlock ?? false}
          onUnlockTarget={handleUnlockClick}
        />
      </Box>
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
