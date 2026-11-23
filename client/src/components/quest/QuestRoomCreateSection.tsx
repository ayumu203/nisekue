import { Alert, Box, Button, CircularProgress, Pagination, Stack, Typography } from '@mui/material'
import { useMemo, useState, type ReactNode } from 'react'
import { homePanelColors, menuButtonSx, panelOutlinedButtonSx, panelPrimaryButtonSx } from '@/constants/styles'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/quest/QuestRoom.json'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import QuestRestrictionsModal from './QuestRestrictionsModal'
import QuestStageModeCard from './QuestStageModeCard'

type QuestRoomCreateSectionProps = {
  activeStages: GetQuestStagesResponse
  selectedStageId: number | ''
  mode: CreateQuestRoomRequest['mode']
  currentPlayerLevel: number | null
  currentPlayerImagePath?: string | null
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
  // 1 ページに出すステージの数と、横に並べる列の数（PC は 2 列で多めに出す）
  stagePageSize?: number
  stageColumns?: number
  // ステージ一覧と、参加制限・募集開始をどの箱に置くかは画面側で決める
  renderLayout: (parts: { stages: ReactNode; form: ReactNode }) => ReactNode
}

function PanelRow({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={{
        display: 'flex',
        // 見出しと内容を離しすぎないよう、内容は見出しのすぐ右に置く
        alignItems: 'baseline',
        gap: 2,
        py: 1.25,
        borderBottom: `1px dotted ${homePanelColors.divider}`,
      }}
    >
      <Typography variant="body2" sx={{ color: homePanelColors.label, whiteSpace: 'nowrap', minWidth: 120 }}>
        {label}
      </Typography>
      <Typography sx={{ color: homePanelColors.text, fontWeight: 700, fontSize: '1rem' }}>{value}</Typography>
    </Box>
  )
}

export default function QuestRoomCreateSection({
  activeStages,
  selectedStageId,
  mode,
  currentPlayerLevel,
  currentPlayerImagePath,
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
  stagePageSize = 3,
  stageColumns = 1,
  renderLayout,
}: QuestRoomCreateSectionProps) {
  const [isRestrictionsModalOpen, setIsRestrictionsModalOpen] = useState(false)
  const [stagePage, setStagePage] = useState(1)
  const handleStageModeSelect = (stageId: number, nextMode: CreateQuestRoomRequest['mode']) => {
    onStageChange(stageId)
    onModeChange(nextMode)
  }

  const stagePageCount = Math.max(1, Math.ceil(activeStages.length / stagePageSize))
  const visibleStagePage = Math.min(stagePage, stagePageCount)
  const pagedStages = useMemo(() => {
    const start = (visibleStagePage - 1) * stagePageSize
    return activeStages.slice(start, start + stagePageSize)
  }, [activeStages, visibleStagePage, stagePageSize])

  const stages = isLoading ? (
    <Stack direction="row" spacing={1} alignItems="center">
      <CircularProgress size={18} />
      <Typography variant="body2" sx={{ color: homePanelColors.label }}>
        {locale.roomPageLoading}
      </Typography>
    </Stack>
  ) : activeStages.length === 0 ? (
    <Alert severity="info">{locale.stagesEmpty}</Alert>
  ) : (
    <Stack spacing={2}>
      <Box sx={{ display: 'grid', gap: 2, gridTemplateColumns: `repeat(${stageColumns}, minmax(0, 1fr))` }}>
        {pagedStages.map((stage) => (
          <QuestStageModeCard
            key={stage.stageId}
            stage={stage}
            playerImagePath={currentPlayerImagePath}
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
            sx={{
              '& .MuiPaginationItem-root': {
                color: '#5a3a10',
                fontWeight: 700,
                transition: 'none',
              },
              '& .MuiTouchRipple-root': {
                display: 'none',
              },
              '& .MuiPaginationItem-root.Mui-selected': {
                color: '#ffffff',
                backgroundColor: homePanelColors.band,
              },
              '& .MuiPaginationItem-root.Mui-selected:hover': {
                backgroundColor: homePanelColors.bandSelected,
              },
            }}
          />
        </Stack>
      ) : null}
    </Stack>
  )

  const form = (
    <Stack spacing={2}>
      <Box>
        <PanelRow
          label={locale.labels.minRequiredLevel}
          value={minRequiredLevelInput.trim() === '' ? locale.noRestriction : minRequiredLevelInput}
        />
        <PanelRow
          label={locale.labels.allowedPlayers}
          value={
            allowedPlayerIds.length === 0
              ? locale.noRestriction
              : locale.selectedAllowedPlayersCount.replace('{{count}}', String(allowedPlayerIds.length))
          }
        />
      </Box>
      <Button
        variant="outlined"
        onClick={() => setIsRestrictionsModalOpen(true)}
        disabled={isLoading}
        sx={{ ...menuButtonSx, ...panelOutlinedButtonSx }}
      >
        {locale.openRestrictionsModal}
      </Button>
      <Button
        variant="contained"
        onClick={() => void onCreateRoom()}
        disabled={isCreateDisabled}
        sx={{ ...menuButtonSx, ...panelPrimaryButtonSx, minHeight: 56, fontSize: '1.15rem' }}
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
  )

  return <>{renderLayout({ stages, form })}</>
}
