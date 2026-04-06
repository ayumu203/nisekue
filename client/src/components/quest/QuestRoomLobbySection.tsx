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
import { useRef, useState } from 'react'
import {
  greenOutlinedInputSx,
  innerSurfaceSx,
  menuButtonSx,
  mutedRedButtonSx,
  softGreenButtonSx,
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

import QuestRestrictionsModal from './QuestRestrictionsModal'
import QuestRoomFormationPreview from './QuestRoomFormationPreview'

type QuestRoomLobbySectionProps = {
  currentRoom: QuestRoomDetailResponse | null
  stageLabel: string | null
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
  isUpdatingParticipantId: string | null
  onRestrictionsChange: (value: string, nextAllowedPlayerIds: string[]) => void
  onUpdateRestrictions: (options?: {
    minRequiredLevelInput?: string
    allowedPlayerIds?: string[]
  }) => void | Promise<void>
  onPositionDraftChange: (participantId: string, nextPosition: { row: BattleRow; column: BattleColumn }) => void
  onUpdateParticipantPosition: (participantId: string) => void | Promise<void>
  onStartQuest: () => void | Promise<void>
  onCancelRoom: () => void | Promise<void>
}

export default function QuestRoomLobbySection({
  currentRoom,
  stageLabel,
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
  isUpdatingParticipantId,
  onRestrictionsChange,
  onUpdateRestrictions,
  onPositionDraftChange,
  onUpdateParticipantPosition,
  onStartQuest,
  onCancelRoom,
}: QuestRoomLobbySectionProps) {
  const [isRestrictionsModalOpen, setIsRestrictionsModalOpen] = useState(false)
  const formationEditorRef = useRef<HTMLDivElement | null>(null)
  const isOwner =
    currentRoom != null && selfParticipantId != null
      ? currentRoom.participants.some(
          (participant) => participant.participantId === selfParticipantId && participant.isOwner,
        )
      : false

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

  return (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: { xs: 1.5, sm: 2.5 },
        color: '#eef4ff',
        backgroundColor: '#1d2d4a',
        borderColor: 'rgba(152, 192, 255, 0.34)',
      }}
    >
      <Stack spacing={2}>
        {currentRoom == null ? (
          <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
            {locale.createdRoomEmpty}
          </Typography>
        ) : isLoading ? (
          <Stack direction="row" spacing={1} alignItems="center">
            <CircularProgress size={18} />
            <Typography variant="body2">{locale.roomLoading}</Typography>
          </Stack>
        ) : (
          <Stack spacing={1.5}>
            <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start', gap: 2 }}>
              <Stack spacing={0.75} alignItems="flex-start" sx={{ flex: 1, minWidth: 0 }}>
                <Typography variant="h5" fontWeight={900} sx={{ color: '#ffffff' }}>
                  {stageLabel ?? `${locale.stage} ${currentRoom.stageId}`}
                </Typography>
                <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
                  <Chip
                    size="small"
                    label={locale.roomStatus[currentRoom.status]}
                    sx={{
                      fontWeight: 800,
                      color: currentRoom.status === 'Recruiting' ? '#173a34' : '#4d1e2a',
                      backgroundColor: currentRoom.status === 'Recruiting' ? '#9ed8ce' : '#f0b7c0',
                    }}
                  />
                  <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
                    {locale.participantCountLabel.replace('{{count}}', String(currentRoom.participants.length))}
                  </Typography>
                </Stack>
              </Stack>
              {isOwner ? (
                <Stack spacing={1} alignItems="center">
                  <Tooltip title={locale.openRestrictionsModal}>
                    <IconButton
                      onClick={() => setIsRestrictionsModalOpen(true)}
                      disabled={currentRoom.status !== 'Recruiting' || isUpdatingRestrictions}
                      sx={{
                        border: '1px solid',
                        borderColor: 'rgba(152, 192, 255, 0.34)',
                        backgroundColor: '#fffdfa',
                        color: '#1d2d4a',
                        alignSelf: 'flex-start',
                      }}
                      aria-label={locale.openRestrictionsModal}
                    >
                      <GearIcon />
                    </IconButton>
                  </Tooltip>
                  <Tooltip title={locale.jumpToFormationEditor}>
                    <IconButton
                      onClick={handleJumpToFormationEditor}
                      sx={{
                        border: '1px solid',
                        borderColor: 'rgba(152, 192, 255, 0.24)',
                        backgroundColor: 'rgba(255, 253, 250, 0.88)',
                        color: '#1d2d4a',
                        alignSelf: 'flex-start',
                      }}
                      aria-label={locale.jumpToFormationEditor}
                    >
                      <PanelIcon />
                    </IconButton>
                  </Tooltip>
                </Stack>
              ) : null}
            </Box>
            <Stack spacing={0.5}>
              <Typography variant="body2" sx={{ color: '#f3f8ff', fontWeight: 600 }}>
                {currentRoom.restrictions.minRequiredLevel == null
                  ? `${locale.labels.minRequiredLevel}: ${locale.noRestriction}`
                  : `${locale.labels.minRequiredLevel}: ${currentRoom.restrictions.minRequiredLevel}`}
              </Typography>
              <Typography variant="body2" sx={{ color: '#f3f8ff', fontWeight: 600 }}>
                {currentRoom.restrictions.allowedPlayers.length === 0
                  ? `${locale.labels.allowedPlayers}: ${locale.noRestriction}`
                  : `${locale.labels.allowedPlayers}: ${currentRoom.restrictions.allowedPlayers
                      .map((player) => player.displayName ?? player.playerId)
                      .join(', ')}`}
              </Typography>
            </Stack>

            {isOwner ? (
              <Stack direction="row" spacing={1.25} useFlexGap flexWrap="wrap">
                <Button
                  variant="contained"
                  startIcon={<PlayArrowIcon sx={{ fontSize: 20 }} />}
                  onClick={() => void onStartQuest()}
                  disabled={
                    isStarting || isCancellingRoom || !currentRoom.canStart || currentRoom.status !== 'Recruiting'
                  }
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
                  {isStarting ? locale.startingQuest : locale.startQuest}
                </Button>
                <Button
                  variant="outlined"
                  startIcon={<CancelIcon sx={{ fontSize: 20 }} />}
                  onClick={() => void onCancelRoom()}
                  disabled={isStarting || isCancellingRoom || currentRoom.status !== 'Recruiting'}
                  sx={{
                    ...menuButtonSx,
                    ...mutedRedButtonSx,
                    color: '#f7f1f2',
                    borderColor: 'rgba(240, 183, 192, 0.44)',
                    backgroundColor: 'rgba(116, 46, 61, 0.22)',
                  }}
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

            {currentRoom.status !== 'Recruiting' ? (
              <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
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
              disabled={currentRoom.status !== 'Recruiting' || isUpdatingRestrictions}
              onClose={() => setIsRestrictionsModalOpen(false)}
              onApply={handleRestrictionsModalApply}
            />

            <div ref={formationEditorRef}>
              <Typography variant="h6" fontWeight={900} sx={{ color: '#ffffff' }}>
                {isOwner ? locale.ownerPositionTitle : locale.waitingForOwnerTitle}
              </Typography>
              <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
                {isOwner ? locale.ownerPositionSubtitle : locale.waitingForOwnerSubtitle}
              </Typography>
            </div>

            <Stack spacing={1.5}>
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
                      color: '#eef4ff',
                      backgroundColor: isSelf ? '#24395d' : '#223452',
                      borderColor: isSelf ? 'rgba(158, 216, 206, 0.42)' : 'rgba(152, 192, 255, 0.22)',
                    }}
                  >
                    <Stack spacing={1.5}>
                      <Stack direction="row" spacing={1.25} alignItems="flex-start">
                        <Box
                          sx={{
                            width: { xs: 92, sm: 110 },
                            minWidth: { xs: 92, sm: 110 },
                            aspectRatio: '4 / 5',
                            borderRadius: 2,
                            border: '1px solid',
                            borderColor: 'rgba(152, 192, 255, 0.22)',
                            backgroundColor: '#172742',
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
                            <Typography variant="caption" sx={{ color: 'rgba(222, 236, 255, 0.64)' }}>
                              {locale.noImage}
                            </Typography>
                          )}
                        </Box>

                        <Stack spacing={1.5} sx={{ flex: 1, minWidth: 0 }}>
                          <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
                            <Typography variant="subtitle2" fontWeight={800} sx={{ color: '#ffffff' }}>
                              {participant.displayName}
                            </Typography>
                            {participant.isOwner ? (
                              <Chip
                                size="small"
                                label={locale.ownerBadge}
                                sx={{ color: '#173a34', backgroundColor: '#9ed8ce', fontWeight: 700 }}
                              />
                            ) : null}
                            {isSelf ? (
                              <Chip
                                size="small"
                                label={locale.selfBadge}
                                sx={{ color: '#173252', backgroundColor: '#d8e8ff', fontWeight: 700 }}
                              />
                            ) : null}
                          </Stack>

                          <Stack
                            direction="row"
                            spacing={1.5}
                            alignItems="center"
                            sx={{ flexWrap: 'nowrap', minWidth: 0 }}
                          >
                            {participant.level != null ? (
                              <Typography
                                variant="body2"
                                sx={{ whiteSpace: 'nowrap', flexShrink: 0, color: 'rgba(222, 236, 255, 0.76)' }}
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
                                    sx={{
                                      width: 20,
                                      height: 20,
                                      objectFit: 'contain',
                                      display: 'block',
                                      flexShrink: 0,
                                    }}
                                  />
                                ) : null}
                                <Typography
                                  variant="body2"
                                  sx={{
                                    whiteSpace: 'nowrap',
                                    overflow: 'hidden',
                                    textOverflow: 'ellipsis',
                                    color: 'rgba(222, 236, 255, 0.76)',
                                  }}
                                >
                                  {`${locale.jobLabel}: ${participant.job.displayName}`}
                                </Typography>
                              </Stack>
                            ) : null}
                          </Stack>

                          {isOwner ? (
                            <Stack direction="row" spacing={1.25}>
                              <FormControl
                                fullWidth
                                sx={{
                                  ...greenOutlinedInputSx,
                                  '& .MuiInputLabel-root': {
                                    color: 'rgba(222, 236, 255, 0.68)',
                                  },
                                  '& .MuiOutlinedInput-root': {
                                    color: '#eef4ff',
                                    backgroundColor: '#172742',
                                    '& fieldset': {
                                      borderColor: 'rgba(152, 192, 255, 0.28)',
                                    },
                                    '&:hover fieldset': {
                                      borderColor: 'rgba(152, 192, 255, 0.46)',
                                    },
                                    '&.Mui-focused fieldset': {
                                      borderColor: '#7ab4ff',
                                    },
                                  },
                                  '& .MuiSvgIcon-root': {
                                    color: 'rgba(222, 236, 255, 0.76)',
                                  },
                                }}
                              >
                                <InputLabel id={`participant-row-${participant.participantId}`}>
                                  {locale.positionRow}
                                </InputLabel>
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

                              <FormControl
                                fullWidth
                                sx={{
                                  ...greenOutlinedInputSx,
                                  '& .MuiInputLabel-root': {
                                    color: 'rgba(222, 236, 255, 0.68)',
                                  },
                                  '& .MuiOutlinedInput-root': {
                                    color: '#eef4ff',
                                    backgroundColor: '#172742',
                                    '& fieldset': {
                                      borderColor: 'rgba(152, 192, 255, 0.28)',
                                    },
                                    '&:hover fieldset': {
                                      borderColor: 'rgba(152, 192, 255, 0.46)',
                                    },
                                    '&.Mui-focused fieldset': {
                                      borderColor: '#7ab4ff',
                                    },
                                  },
                                  '& .MuiSvgIcon-root': {
                                    color: 'rgba(222, 236, 255, 0.76)',
                                  },
                                }}
                              >
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
                            <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
                              {`${locale.positionRow}: ${locale.rows[participant.position.row]} / ${locale.positionColumn}: ${locale.columns[participant.position.column]}`}
                            </Typography>
                          )}

                          {isOwner ? (
                            <Button
                              variant="contained"
                              onClick={() => void onUpdateParticipantPosition(participant.participantId)}
                              disabled={
                                isUpdatingParticipantId === participant.participantId ||
                                currentRoom.status !== 'Recruiting'
                              }
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
                              {isUpdatingParticipantId === participant.participantId
                                ? locale.updatingPosition
                                : locale.savePosition}
                            </Button>
                          ) : null}
                        </Stack>
                      </Stack>
                    </Stack>
                  </Paper>
                )
              })}
            </Stack>
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
