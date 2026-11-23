import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  IconButton,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  SvgIcon,
  Tooltip,
  Typography,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import CancelIcon from '@mui/icons-material/Cancel'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import { useRef, useState, type ReactNode } from 'react'
import QuestRestrictionsModal from './QuestRestrictionsModal'
import QuestRoomFormationPreview from './QuestRoomFormationPreview'
import {
  greenOutlinedInputSx,
  homePanelColors,
  menuButtonSx,
  mutedRedButtonSx,
  panelOutlinedButtonSx,
  panelPrimaryButtonSx,
} from '@/constants/styles'
import locale from '../../../locale/quest/QuestRoom.json'
import type { BattleColumn, BattleRow, QuestRoomDetailResponse } from '@/schema/quest'
import type { PlayerSummary } from '@/schema/player'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'

function GearIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <path d="M19.14 12.94c.04-.31.06-.63.06-.94s-.02-.63-.06-.94l2.03-1.58a.5.5 0 0 0 .12-.64l-1.92-3.32a.5.5 0 0 0-.6-.22l-2.39.96a7.03 7.03 0 0 0-1.63-.94l-.36-2.54a.5.5 0 0 0-.49-.42H10.1a.5.5 0 0 0-.49.42l-.36 2.54c-.58.23-1.12.55-1.63.94l-2.39-.96a.5.5 0 0 0-.6.22L2.71 8.84a.5.5 0 0 0 .12.64l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94l-2.03 1.58a.5.5 0 0 0-.12.64l1.92 3.32a.5.5 0 0 0 .6.22l2.39-.96c.5.39 1.05.71 1.63.94l.36 2.54a.5.5 0 0 0 .49.42h3.8a.5.5 0 0 0 .49-.42l.36-2.54c.58-.23 1.12-.55 1.63-.94l2.39.96a.5.5 0 0 0 .6-.22l1.92-3.32a.5.5 0 0 0-.12-.64l-2.03-1.58ZM12 15.5A3.5 3.5 0 1 1 12 8.5a3.5 3.5 0 0 1 0 7Z" />
    </SvgIcon>
  )
}

function PanelIcon() {
  return (
    <SvgIcon viewBox="0 0 24 24" fontSize="small">
      <path d="M4 5.5A1.5 1.5 0 0 1 5.5 4h13A1.5 1.5 0 0 1 20 5.5v13a1.5 1.5 0 0 1-1.5 1.5h-13A1.5 1.5 0 0 1 4 18.5Zm1.5-.5a.5.5 0 0 0-.5.5v3h14v-3a.5.5 0 0 0-.5-.5Zm13.5 4.5H5v9a.5.5 0 0 0 .5.5h13a.5.5 0 0 0 .5-.5ZM7 6.5a.75.75 0 1 1 0 1.5.75.75 0 0 1 0-1.5m3 0a.75.75 0 1 1 0 1.5.75.75 0 0 1 0-1.5m-3 5h10v1H7zm0 3h6v1H7z" />
    </SvgIcon>
  )
}

type QuestRoomLobbySectionProps = {
  currentRoom: QuestRoomDetailResponse | null
  selfParticipantId: string | null
  currentPlayerLevel: number | null
  selectablePlayers: PlayerSummary[]
  minRequiredLevelInput: string
  allowedPlayerIds: string[]
  positionDrafts: Record<string, { row: BattleRow; column: BattleColumn }>
  isLoading: boolean
  isStarting: boolean
  isCancellingRoom: boolean
  isUpdatingRestrictions: boolean
  isPlayerCandidatesLoading: boolean
  playerCandidatesError: Error | null
  isSavingFormation: boolean
  onRestrictionsChange: (value: string, nextAllowedPlayerIds: string[]) => void
  onUpdateRestrictions: (options?: {
    minRequiredLevelInput?: string
    allowedPlayerIds?: string[]
  }) => void | Promise<void>
  onPositionDraftChange: (participantId: string, nextPosition: { row: BattleRow; column: BattleColumn }) => void
  onSaveFormation: () => void | Promise<void>
  onStartQuest: () => void | Promise<void>
  onCancelRoom: () => void | Promise<void>
  // ルームの情報（START・配置の確認）と配置変更をどの箱に置くかは画面側で決める
  renderLayout: (parts: { info: ReactNode; editor: ReactNode; editorTitle: string }) => ReactNode
}

const iconButtonSx = {
  border: '1px solid',
  borderColor: homePanelColors.divider,
  backgroundColor: '#ffffff',
  color: '#5a3a10',
} as const

export default function QuestRoomLobbySection({
  currentRoom,
  selfParticipantId,
  currentPlayerLevel,
  selectablePlayers,
  minRequiredLevelInput,
  allowedPlayerIds,
  positionDrafts,
  isLoading,
  isStarting,
  isCancellingRoom,
  isUpdatingRestrictions,
  isPlayerCandidatesLoading,
  playerCandidatesError,
  isSavingFormation,
  onRestrictionsChange,
  onUpdateRestrictions,
  onPositionDraftChange,
  onSaveFormation,
  onStartQuest,
  onCancelRoom,
  renderLayout,
}: QuestRoomLobbySectionProps) {
  const [isRestrictionsModalOpen, setIsRestrictionsModalOpen] = useState(false)
  const formationEditorRef = useRef<HTMLDivElement | null>(null)
  const isOwner =
    currentRoom != null && selfParticipantId != null
      ? currentRoom.participants.some(
          (participant) => participant.participantId === selfParticipantId && participant.isOwner,
        )
      : false
  const isStartDisabled =
    currentRoom == null ||
    isStarting ||
    isCancellingRoom ||
    !currentRoom.canStart ||
    currentRoom.status !== 'Recruiting'

  function handleRowChange(
    participantId: string,
    draft: { row: BattleRow; column: BattleColumn },
    event: SelectChangeEvent<BattleRow>,
  ) {
    onPositionDraftChange(participantId, {
      row: event.target.value as BattleRow,
      column: draft.column,
    })
  }

  function handleColumnChange(
    participantId: string,
    draft: { row: BattleRow; column: BattleColumn },
    event: SelectChangeEvent<BattleColumn>,
  ) {
    onPositionDraftChange(participantId, {
      row: draft.row,
      column: event.target.value as BattleColumn,
    })
  }

  const handleRestrictionsModalApply = async (nextMinRequiredLevelInput: string, nextAllowedPlayerIds: string[]) => {
    onRestrictionsChange(nextMinRequiredLevelInput, nextAllowedPlayerIds)
    await onUpdateRestrictions({
      minRequiredLevelInput: nextMinRequiredLevelInput,
      allowedPlayerIds: nextAllowedPlayerIds,
    })
  }

  function handleJumpToFormationEditor(): void {
    formationEditorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  const editorTitle = isOwner ? locale.ownerPositionTitle : locale.waitingForOwnerTitle

  if (currentRoom == null || isLoading) {
    const message =
      currentRoom == null ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          {locale.createdRoomEmpty}
        </Typography>
      ) : (
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={18} />
          <Typography variant="body2" sx={{ color: homePanelColors.label }}>
            {locale.roomLoading}
          </Typography>
        </Stack>
      )
    return <>{renderLayout({ info: message, editor: null, editorTitle })}</>
  }

  const isRecruiting = currentRoom.status === 'Recruiting'

  const info = (
    <Stack spacing={2}>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 2 }}>
        <Stack direction="row" spacing={1.25} useFlexGap flexWrap="wrap" alignItems="center" sx={{ flex: 1 }}>
          <Chip
            size="small"
            label={locale.roomStatus[currentRoom.status]}
            sx={{
              fontWeight: 800,
              color: isRecruiting ? '#3a2400' : '#4d1e2a',
              backgroundColor: isRecruiting ? '#ffcc00' : '#f0b7c0',
            }}
          />
          <Typography variant="body2" sx={{ color: homePanelColors.text, fontWeight: 700 }}>
            {locale.participantCountLabel.replace('{{count}}', String(currentRoom.participants.length))}
          </Typography>
        </Stack>
        {isOwner ? (
          <Stack direction="row" spacing={1}>
            <Tooltip title={locale.openRestrictionsModal}>
              <span>
                <IconButton
                  onClick={() => setIsRestrictionsModalOpen(true)}
                  disabled={!isRecruiting || isUpdatingRestrictions}
                  sx={iconButtonSx}
                  aria-label={locale.openRestrictionsModal}
                >
                  <GearIcon />
                </IconButton>
              </span>
            </Tooltip>
            <Tooltip title={locale.jumpToFormationEditor}>
              <IconButton
                onClick={handleJumpToFormationEditor}
                sx={iconButtonSx}
                aria-label={locale.jumpToFormationEditor}
              >
                <PanelIcon />
              </IconButton>
            </Tooltip>
          </Stack>
        ) : null}
      </Box>

      <Box sx={{ display: 'grid', gridTemplateColumns: { xs: '1fr', md: '1fr 1fr' }, columnGap: 3 }}>
        <InfoRow
          label={locale.labels.minRequiredLevel}
          value={
            currentRoom.restrictions.minRequiredLevel == null
              ? locale.noRestriction
              : String(currentRoom.restrictions.minRequiredLevel)
          }
        />
        <InfoRow
          label={locale.labels.allowedPlayers}
          value={
            currentRoom.restrictions.allowedPlayers.length === 0
              ? locale.noRestriction
              : currentRoom.restrictions.allowedPlayers
                  .map((player) => player.displayName ?? player.playerId)
                  .join(', ')
          }
        />
      </Box>

      {isOwner ? (
        <Stack direction="row" spacing={1.5}>
          <Button
            variant="contained"
            startIcon={<PlayArrowIcon sx={{ fontSize: 20 }} />}
            onClick={() => void onStartQuest()}
            disabled={isStartDisabled || isSavingFormation}
            sx={{ ...menuButtonSx, ...panelPrimaryButtonSx, flex: 2, minHeight: 52, fontSize: '1.1rem' }}
          >
            {isStarting ? locale.startingQuest : locale.startQuest}
          </Button>
          <Button
            variant="outlined"
            startIcon={<CancelIcon sx={{ fontSize: 20 }} />}
            onClick={() => void onCancelRoom()}
            disabled={isStarting || isCancellingRoom || !isRecruiting}
            sx={{ ...menuButtonSx, ...mutedRedButtonSx, flex: 1, minHeight: 52, fontWeight: 800 }}
          >
            {isCancellingRoom ? locale.cancellingRoom : locale.cancelRoom}
          </Button>
        </Stack>
      ) : null}

      <QuestRoomFormationPreview
        currentRoom={currentRoom}
        selfParticipantId={selfParticipantId}
        positionDrafts={positionDrafts}
      />

      {!isRecruiting ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          {locale.roomClosedMessage}
        </Typography>
      ) : null}

      <QuestRestrictionsModal
        open={isRestrictionsModalOpen}
        minRequiredLevelInput={minRequiredLevelInput}
        currentPlayerLevel={currentPlayerLevel}
        selectablePlayers={selectablePlayers}
        allowedPlayerIds={allowedPlayerIds}
        isLoading={isPlayerCandidatesLoading}
        error={playerCandidatesError}
        disabled={!isRecruiting || isUpdatingRestrictions}
        onClose={() => setIsRestrictionsModalOpen(false)}
        onApply={handleRestrictionsModalApply}
      />
    </Stack>
  )

  const editor = (
    <Stack spacing={1.5} ref={formationEditorRef}>
      {!isOwner && locale.waitingForOwnerSubtitle ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          {locale.waitingForOwnerSubtitle}
        </Typography>
      ) : null}
      {currentRoom.participants.map((participant) => {
        const draft = positionDrafts[participant.participantId] ?? participant.position
        const isSelf = participant.participantId === selfParticipantId
        const jobImageSrc = participant.job?.code ? resolveJobAssetPath(participant.job.code) : null

        return (
          <Paper
            key={participant.participantId}
            variant="outlined"
            sx={{
              borderRadius: 2,
              p: 1.5,
              color: homePanelColors.text,
              backgroundColor: isSelf ? '#f3f8ef' : '#fffdf6',
              borderColor: isSelf ? '#b9d9bb' : homePanelColors.divider,
            }}
          >
            <Stack direction="row" spacing={1.5} alignItems="flex-start">
              <Box
                sx={{
                  width: { xs: 92, sm: 100 },
                  minWidth: { xs: 92, sm: 100 },
                  aspectRatio: '4 / 5',
                  borderRadius: 2,
                  backgroundColor: '#efe4c8',
                  overflow: 'hidden',
                  display: 'grid',
                  placeItems: 'center',
                }}
              >
                {participant.imagePath ? (
                  <Box
                    component="img"
                    src={resolveCharacterAssetPath(participant.imagePath) ?? undefined}
                    alt={participant.displayName}
                    sx={{
                      width: '100%',
                      height: '100%',
                      objectFit: 'contain',
                      objectPosition: 'center bottom',
                      display: 'block',
                    }}
                  />
                ) : (
                  <Typography variant="caption" sx={{ color: homePanelColors.label }}>
                    {locale.noImage}
                  </Typography>
                )}
              </Box>

              <Stack spacing={1} sx={{ flex: 1, minWidth: 0 }}>
                <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
                  <Typography variant="subtitle1" fontWeight={800} sx={{ color: homePanelColors.text }}>
                    {participant.displayName}
                  </Typography>
                  {participant.isOwner ? (
                    <Chip
                      size="small"
                      label={locale.ownerBadge}
                      sx={{ color: '#3a2400', backgroundColor: '#ffcc00', fontWeight: 800 }}
                    />
                  ) : null}
                  {isSelf ? (
                    <Chip
                      size="small"
                      label={locale.selfBadge}
                      sx={{ color: '#ffffff', backgroundColor: homePanelColors.band, fontWeight: 800 }}
                    />
                  ) : null}
                </Stack>

                <Stack direction="row" spacing={1.5} alignItems="center" sx={{ flexWrap: 'nowrap', minWidth: 0 }}>
                  {participant.level != null ? (
                    <Typography
                      variant="body2"
                      sx={{ whiteSpace: 'nowrap', flexShrink: 0, color: homePanelColors.label }}
                    >
                      {`${locale.levelLabel}.${participant.level}`}
                    </Typography>
                  ) : null}
                  {participant.job?.displayName ? (
                    <Stack
                      direction="row"
                      spacing={1}
                      alignItems="center"
                      sx={{ flexWrap: 'nowrap', minWidth: 0, overflow: 'hidden' }}
                    >
                      {jobImageSrc ? (
                        <Box
                          component="img"
                          src={jobImageSrc}
                          alt={participant.job.displayName}
                          sx={{ width: 20, height: 20, objectFit: 'contain', display: 'block', flexShrink: 0 }}
                        />
                      ) : null}
                      <Typography
                        variant="body2"
                        sx={{
                          whiteSpace: 'nowrap',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          color: homePanelColors.label,
                        }}
                      >
                        {`${locale.jobLabel}: ${participant.job.displayName}`}
                      </Typography>
                    </Stack>
                  ) : null}
                </Stack>

                {isOwner ? (
                  <Stack direction="row" spacing={1.25}>
                    <FormControl fullWidth size="small" sx={greenOutlinedInputSx}>
                      <InputLabel id={`participant-row-${participant.participantId}`}>{locale.positionRow}</InputLabel>
                      <Select
                        labelId={`participant-row-${participant.participantId}`}
                        value={draft.row}
                        label={locale.positionRow}
                        onChange={(event) => handleRowChange(participant.participantId, draft, event)}
                      >
                        <MenuItem value="Front">{locale.rows.Front}</MenuItem>
                        <MenuItem value="Middle">{locale.rows.Middle}</MenuItem>
                        <MenuItem value="Back">{locale.rows.Back}</MenuItem>
                      </Select>
                    </FormControl>

                    <FormControl fullWidth size="small" sx={greenOutlinedInputSx}>
                      <InputLabel id={`participant-column-${participant.participantId}`}>
                        {locale.positionColumn}
                      </InputLabel>
                      <Select
                        labelId={`participant-column-${participant.participantId}`}
                        value={draft.column}
                        label={locale.positionColumn}
                        onChange={(event) => handleColumnChange(participant.participantId, draft, event)}
                      >
                        <MenuItem value="Left">{locale.columns.Left}</MenuItem>
                        <MenuItem value="Right">{locale.columns.Right}</MenuItem>
                      </Select>
                    </FormControl>
                  </Stack>
                ) : (
                  <Typography variant="body2" sx={{ color: homePanelColors.label }}>
                    {`${locale.positionRow}: ${locale.rows[participant.position.row]} / ${locale.positionColumn}: ${locale.columns[participant.position.column]}`}
                  </Typography>
                )}
              </Stack>
            </Stack>
          </Paper>
        )
      })}
      {isOwner ? (
        <Stack direction="row" spacing={1.5} sx={{ pt: 0.5 }}>
          <Button
            variant="outlined"
            onClick={() => void onSaveFormation()}
            disabled={isSavingFormation || isStarting || isCancellingRoom || !isRecruiting}
            sx={{ ...menuButtonSx, ...panelOutlinedButtonSx, flex: 1, minHeight: 52 }}
          >
            {isSavingFormation ? locale.updatingPosition : locale.confirmFormation}
          </Button>
          <Button
            variant="contained"
            startIcon={<PlayArrowIcon sx={{ fontSize: 20 }} />}
            onClick={() => void onStartQuest()}
            disabled={isStartDisabled || isSavingFormation}
            sx={{ ...menuButtonSx, ...panelPrimaryButtonSx, flex: 1, minHeight: 52, fontSize: '1.1rem' }}
          >
            {isStarting ? locale.startingQuest : locale.startQuest}
          </Button>
        </Stack>
      ) : null}
    </Stack>
  )

  return <>{renderLayout({ info, editor, editorTitle })}</>
}

function InfoRow({ label, value }: { label: string; value: string }) {
  return (
    <Box
      sx={{
        display: 'flex',
        justifyContent: 'space-between',
        gap: 2,
        py: 1.25,
        borderBottom: `1px dotted ${homePanelColors.divider}`,
      }}
    >
      <Typography variant="body2" sx={{ color: homePanelColors.label, whiteSpace: 'nowrap' }}>
        {label}
      </Typography>
      <Typography variant="body2" sx={{ color: homePanelColors.text, fontWeight: 700, textAlign: 'right' }}>
        {value}
      </Typography>
    </Box>
  )
}
