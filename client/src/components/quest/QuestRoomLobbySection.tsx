import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { useRef } from 'react'
import {
  greenOutlinedInputSx,
  innerSurfaceSx,
  menuButtonSx,
  mutedRedButtonSx,
  softGreenButtonSx,
} from '@/constants/styles'
import locale from '../../../locale/quest/QuestRoom.json'
import type { BattleColumn, BattleRow, QuestRoomDetailResponse } from '@/schema/quest'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'
import QuestRoomFormationPreview from './QuestRoomFormationPreview'

type QuestRoomLobbySectionProps = {
  currentRoom: QuestRoomDetailResponse | null
  stageLabel: string | null
  selfParticipantId: string | null
  positionDrafts: Record<string, { row: BattleRow; column: BattleColumn }>
  isLoading: boolean
  isStarting: boolean
  isCancellingRoom: boolean
  isUpdatingParticipantId: string | null
  onPositionDraftChange: (participantId: string, nextPosition: { row: BattleRow; column: BattleColumn }) => void
  onUpdateParticipantPosition: (participantId: string) => void | Promise<void>
  onStartQuest: () => void | Promise<void>
  onCancelRoom: () => void | Promise<void>
}

export default function QuestRoomLobbySection({
  currentRoom,
  stageLabel,
  selfParticipantId,
  positionDrafts,
  isLoading,
  isStarting,
  isCancellingRoom,
  isUpdatingParticipantId,
  onPositionDraftChange,
  onUpdateParticipantPosition,
  onStartQuest,
  onCancelRoom,
}: QuestRoomLobbySectionProps) {
  const positionEditorRef = useRef<HTMLDivElement | null>(null)
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

  function handleOpenPositionEditor() {
    positionEditorRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
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
            <Stack spacing={0.75} alignItems="flex-start">
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

            <QuestRoomFormationPreview
              currentRoom={currentRoom}
              selfParticipantId={selfParticipantId}
              positionDrafts={positionDrafts}
              onOpenPositionEditor={handleOpenPositionEditor}
            />

            {currentRoom.status !== 'Recruiting' ? (
              <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
                {locale.roomClosedMessage}
              </Typography>
            ) : null}

            {isOwner ? (
              <Stack direction="row" spacing={1.25} useFlexGap flexWrap="wrap">
                <Button
                  variant="contained"
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

            <div ref={positionEditorRef}>
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
