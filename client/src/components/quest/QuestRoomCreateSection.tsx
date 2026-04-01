import { Alert, Box, Button, CircularProgress, Pagination, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx, menuButtonSx, softGreenButtonSx } from '@/constants/styles'
import { useMemo, useState } from 'react'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/quest/QuestRoom.json'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import QuestRestrictionsModal from './QuestRestrictionsModal'
import QuestStageModeCard from './QuestStageModeCard'

const STAGE_PAGE_SIZE = 3

type QuestRoomCreateSectionProps = {
  activeStages: GetQuestStagesResponse
  selectedStageId: number | ''
  mode: CreateQuestRoomRequest['mode']
  currentPlayerLevel: number | null
  minRequiredLevelInput: string
  selectablePlayers: PlayerSummary[]
  allowedPlayerIds: string[]
  isLoading: boolean
  isPlayerCandidatesLoading: boolean
  playerCandidatesError: Error | null
  isSubmitting: boolean
  isCreateDisabled: boolean
  onStageChange: (stageId: number | '') => void
  onModeChange: (mode: CreateQuestRoomRequest['mode']) => void
  onRestrictionsChange: (value: string, nextAllowedPlayerIds: string[]) => void
  onCreateRoom: () => void | Promise<void>
}

export default function QuestRoomCreateSection({
  activeStages,
  selectedStageId,
  mode,
  currentPlayerLevel,
  minRequiredLevelInput,
  selectablePlayers,
  allowedPlayerIds,
  isLoading,
  isPlayerCandidatesLoading,
  playerCandidatesError,
  isSubmitting,
  isCreateDisabled,
  onStageChange,
  onModeChange,
  onRestrictionsChange,
  onCreateRoom,
}: QuestRoomCreateSectionProps) {
  const [isRestrictionsModalOpen, setIsRestrictionsModalOpen] = useState(false)
  const [stagePage, setStagePage] = useState(1)
  const handleStageModeSelect = (stageId: number, nextMode: CreateQuestRoomRequest['mode']) => {
    onStageChange(stageId)
    onModeChange(nextMode)
  }

  const stagePageCount = Math.max(1, Math.ceil(activeStages.length / STAGE_PAGE_SIZE))
  const visibleStagePage = Math.min(stagePage, stagePageCount)
  const pagedStages = useMemo(() => {
    const start = (visibleStagePage - 1) * STAGE_PAGE_SIZE
    return activeStages.slice(start, start + STAGE_PAGE_SIZE)
  }, [activeStages, visibleStagePage])

  return (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: { xs: 2, sm: 2.5 },
        color: '#eef4ff',
        backgroundColor: '#1d2d4a',
        borderColor: 'rgba(152, 192, 255, 0.34)',
      }}
    >
      <Stack spacing={2}>
        {isLoading ? (
          <Stack direction="row" spacing={1} alignItems="center">
            <CircularProgress size={18} />
            <Typography variant="body2">{locale.roomPageLoading}</Typography>
          </Stack>
        ) : activeStages.length === 0 ? (
          <Alert severity="info">{locale.stagesEmpty}</Alert>
        ) : (
          <Stack spacing={2}>
            <Box
              sx={{
                display: 'grid',
                gap: 2,
              }}
            >
              {pagedStages.map((stage) => (
                <QuestStageModeCard
                  key={stage.stageId}
                  stage={stage}
                  isSelected={selectedStageId === stage.stageId}
                  selectedMode={mode}
                  onSelect={handleStageModeSelect}
                />
              ))}
            </Box>

            {stagePageCount > 1 ? (
              <Stack alignItems="center">
                <Pagination
                  page={visibleStagePage}
                  count={stagePageCount}
                  onChange={(_, nextPage) => setStagePage(nextPage)}
                  color="primary"
                />
              </Stack>
            ) : null}

            <Stack spacing={1.5}>
              <Stack spacing={1}>
                <Typography variant="body2" sx={{ color: 'rgba(240, 247, 255, 0.9)' }}>
                  {minRequiredLevelInput.trim() === ''
                    ? `${locale.labels.minRequiredLevel}: ${locale.noRestriction}`
                    : `${locale.labels.minRequiredLevel}: ${minRequiredLevelInput}`}
                </Typography>
                <Typography variant="body2" sx={{ color: '#f3f8ff', fontWeight: 600 }}>
                  {allowedPlayerIds.length === 0
                    ? `${locale.labels.allowedPlayers}: ${locale.noRestriction}`
                    : locale.selectedAllowedPlayersCount.replace('{{count}}', String(allowedPlayerIds.length))}
                </Typography>
                <Button
                  variant="outlined"
                  onClick={() => setIsRestrictionsModalOpen(true)}
                  sx={{
                    ...menuButtonSx,
                    color: '#eef4ff',
                    borderColor: 'rgba(152, 192, 255, 0.34)',
                  }}
                >
                  {locale.openRestrictionsModal}
                </Button>
              </Stack>
            </Stack>

            <Button
              variant="contained"
              onClick={() => void onCreateRoom()}
              disabled={isCreateDisabled}
              sx={{
                ...menuButtonSx,
                ...softGreenButtonSx,
                color: '#ffffff',
                backgroundColor: '#4f79b5',
                boxShadow: 'none',
                '&:hover': {
                  backgroundColor: '#5a86c5',
                  boxShadow: 'none',
                },
              }}
            >
              {isSubmitting ? locale.creatingRoom : locale.createRoom}
            </Button>

            <QuestRestrictionsModal
              open={isRestrictionsModalOpen}
              minRequiredLevelInput={minRequiredLevelInput}
              currentPlayerLevel={currentPlayerLevel}
              selectablePlayers={selectablePlayers}
              allowedPlayerIds={allowedPlayerIds}
              isLoading={isPlayerCandidatesLoading}
              error={playerCandidatesError}
              onClose={() => setIsRestrictionsModalOpen(false)}
              onApply={onRestrictionsChange}
            />
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
