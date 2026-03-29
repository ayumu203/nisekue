import { Alert, Box, Button, CircularProgress, Paper, Stack, TextField, Typography } from '@mui/material'
import { innerSurfaceSx, menuButtonSx, softGreenButtonSx } from '@/constants/styles'
import { useState } from 'react'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/quest/QuestRoom.json'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import QuestAllowedPlayersOverlay from './QuestAllowedPlayersOverlay'
import QuestStageModeCard from './QuestStageModeCard'

type QuestRoomCreateSectionProps = {
  activeStages: GetQuestStagesResponse
  selectedStageId: number | ''
  mode: CreateQuestRoomRequest['mode']
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
  onMinRequiredLevelChange: (value: string) => void
  onToggleAllowedPlayer: (playerId: string) => void
  onCreateRoom: () => void | Promise<void>
}

export default function QuestRoomCreateSection({
  activeStages,
  selectedStageId,
  mode,
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
  onMinRequiredLevelChange,
  onToggleAllowedPlayer,
  onCreateRoom,
}: QuestRoomCreateSectionProps) {
  const [isAllowedPlayersOverlayOpen, setIsAllowedPlayersOverlayOpen] = useState(false)
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

            <Stack spacing={1.5}>
              <TextField
                label={locale.labels.minRequiredLevel}
                type="number"
                value={minRequiredLevelInput}
                onChange={(event) => onMinRequiredLevelChange(event.target.value)}
                inputProps={{ min: 1 }}
                placeholder={locale.noRestriction}
                fullWidth
                sx={{
                  '& .MuiInputLabel-root': {
                    color: 'rgba(240, 247, 255, 0.82)',
                  },
                  '& .MuiInputBase-input': {
                    color: '#ffffff',
                  },
                  '& .MuiOutlinedInput-root': {
                    '& fieldset': {
                      borderColor: 'rgba(152, 192, 255, 0.34)',
                    },
                    '&:hover fieldset': {
                      borderColor: 'rgba(186, 214, 255, 0.6)',
                    },
                    '&.Mui-focused fieldset': {
                      borderColor: '#b9d3ff',
                    },
                  },
                }}
              />

              <Stack spacing={1}>
                <Typography variant="subtitle2" sx={{ color: '#ffffff', fontWeight: 800 }}>
                  {locale.labels.allowedPlayers}
                </Typography>
                <Typography variant="body2" sx={{ color: 'rgba(240, 247, 255, 0.9)' }}>
                  {locale.allowedPlayersHint}
                </Typography>
                <Typography variant="body2" sx={{ color: '#f3f8ff', fontWeight: 600 }}>
                  {allowedPlayerIds.length === 0
                    ? locale.noRestriction
                    : locale.selectedAllowedPlayersCount.replace('{{count}}', String(allowedPlayerIds.length))}
                </Typography>
                <Button
                  variant="outlined"
                  onClick={() => setIsAllowedPlayersOverlayOpen(true)}
                  sx={{
                    ...menuButtonSx,
                    color: '#eef4ff',
                    borderColor: 'rgba(152, 192, 255, 0.34)',
                  }}
                >
                  {locale.limitAllowedPlayers}
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

            <QuestAllowedPlayersOverlay
              open={isAllowedPlayersOverlayOpen}
              selectablePlayers={selectablePlayers}
              allowedPlayerIds={allowedPlayerIds}
              isLoading={isPlayerCandidatesLoading}
              error={playerCandidatesError}
              onClose={() => setIsAllowedPlayersOverlayOpen(false)}
              onToggleAllowedPlayer={onToggleAllowedPlayer}
            />
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
