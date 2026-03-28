import { Alert, Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx, menuButtonSx, softGreenButtonSx } from '@/constants/styles'
import locale from '../../../locale/quest/QuestRoom.json'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import QuestStageModeCard from './QuestStageModeCard'

type QuestRoomCreateSectionProps = {
  activeStages: GetQuestStagesResponse
  selectedStageId: number | ''
  mode: CreateQuestRoomRequest['mode']
  isLoading: boolean
  isSubmitting: boolean
  isCreateDisabled: boolean
  onStageChange: (stageId: number | '') => void
  onModeChange: (mode: CreateQuestRoomRequest['mode']) => void
  onCreateRoom: () => void | Promise<void>
}

export default function QuestRoomCreateSection({
  activeStages,
  selectedStageId,
  mode,
  isLoading,
  isSubmitting,
  isCreateDisabled,
  onStageChange,
  onModeChange,
  onCreateRoom,
}: QuestRoomCreateSectionProps) {
  const handleStageModeSelect = (stageId: number, nextMode: CreateQuestRoomRequest['mode']) => {
    onStageChange(stageId)
    onModeChange(nextMode)
  }

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
              {activeStages.map((stage) => (
                <QuestStageModeCard
                  key={stage.stageId}
                  stage={stage}
                  isSelected={selectedStageId === stage.stageId}
                  selectedMode={mode}
                  onSelect={handleStageModeSelect}
                />
              ))}
            </Box>

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
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
