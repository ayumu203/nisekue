import { useMemo, useState } from 'react'
import {
  Alert,
  Button,
  Chip,
  MenuItem,
  Paper,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import { innerSurfaceSx } from '@/constants/styles'
import type { QuestRoomDetailResponse } from '@/schema/quest'
import locale from '../../../locale/quest/Quest.json'

type QuestRoomDetailProps = {
  room: QuestRoomDetailResponse | undefined
  currentUserId: string | null
  isLoading: boolean
  error: string | null
  onJoin: () => Promise<void>
  onStart: () => Promise<void>
  onUpdatePosition: (participantId: string, row: 'Front' | 'Middle' | 'Back', column: 'Left' | 'Right') => Promise<void>
}

const rowOptions = ['Front', 'Middle', 'Back'] as const
const columnOptions = ['Left', 'Right'] as const

function formatRoomStatus(status: 'Recruiting' | 'Closed') {
  return status === 'Recruiting' ? locale.statusRecruiting : locale.statusClosed
}

export default function QuestRoomDetail({
  room,
  currentUserId,
  isLoading,
  error,
  onJoin,
  onStart,
  onUpdatePosition,
}: QuestRoomDetailProps) {
  const [positionDrafts, setPositionDrafts] = useState<Record<string, { row: 'Front' | 'Middle' | 'Back'; column: 'Left' | 'Right' }>>({})

  const currentParticipant = useMemo(
    () => room?.participants.find((participant) => participant.playerId === currentUserId) ?? null,
    [currentUserId, room],
  )

  const canJoin = Boolean(room?.status === 'Recruiting' && currentUserId && !currentParticipant)
  const isOwner = room?.ownerPlayerId === currentUserId

  const pendingParticipants = room?.participants.filter((participant) => participant.status !== 'Left') ?? []

  const resolveDraft = (participantId: string, row: 'Front' | 'Middle' | 'Back', column: 'Left' | 'Right') =>
    positionDrafts[participantId] ?? { row, column }

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
      <Stack spacing={2}>
        <Typography variant="h5">{locale.roomSelected}</Typography>
        {error ? <Alert severity="warning">{error}</Alert> : null}
        {isLoading ? <Typography variant="body2">{locale.roomDetailLoading}</Typography> : null}
        {!isLoading && !room ? <Alert severity="info">{locale.noRooms}</Alert> : null}
        {room ? (
          <>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip label={`${locale.stage}: ${room.stageId}`} />
              <Chip label={formatRoomStatus(room.status)} />
              <Chip label={`${locale.roomVersion}: ${room.version}`} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {locale.createdAt}: {new Date(room.createdAt).toLocaleString()}
            </Typography>
            {currentParticipant ? (
              <Alert severity="success">{locale.alreadyJoinedRoom}</Alert>
            ) : (
              <Alert severity="info">{locale.notJoinedRoom}</Alert>
            )}
            <Stack spacing={1.5}>
              {pendingParticipants.map((participant) => {
                const draft = resolveDraft(participant.participantId, participant.position.row, participant.position.column)
                const canEdit = isOwner || participant.playerId === currentUserId

                return (
                  <Paper key={participant.participantId} variant="outlined" sx={{ borderRadius: 2, p: 1.5 }}>
                    <Stack spacing={1.25}>
                      <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                        <Typography fontWeight={700}>{participant.displayName}</Typography>
                        <Chip
                          size="small"
                          label={participant.isOwner ? locale.ownerLabel : participant.status}
                          color={participant.isOwner ? 'primary' : 'default'}
                        />
                      </Stack>
                      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                        <TextField
                          select
                          size="small"
                          label={locale.positionRow}
                          value={draft.row}
                          disabled={!canEdit}
                          onChange={(event) =>
                            setPositionDrafts((current) => ({
                              ...current,
                              [participant.participantId]: {
                                row: event.target.value as 'Front' | 'Middle' | 'Back',
                                column: draft.column,
                              },
                            }))
                          }
                        >
                          {rowOptions.map((row) => (
                            <MenuItem key={row} value={row}>
                              {locale.rows[row]}
                            </MenuItem>
                          ))}
                        </TextField>
                        <TextField
                          select
                          size="small"
                          label={locale.positionColumn}
                          value={draft.column}
                          disabled={!canEdit}
                          onChange={(event) =>
                            setPositionDrafts((current) => ({
                              ...current,
                              [participant.participantId]: {
                                row: draft.row,
                                column: event.target.value as 'Left' | 'Right',
                              },
                            }))
                          }
                        >
                          {columnOptions.map((column) => (
                            <MenuItem key={column} value={column}>
                              {locale.columns[column]}
                            </MenuItem>
                          ))}
                        </TextField>
                        <Button
                          variant="outlined"
                          disabled={!canEdit}
                          onClick={() => void onUpdatePosition(participant.participantId, draft.row, draft.column)}
                        >
                          {locale.savePosition}
                        </Button>
                      </Stack>
                    </Stack>
                  </Paper>
                )
              })}
            </Stack>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
              <Button variant="contained" disabled={!canJoin} onClick={() => void onJoin()}>
                {locale.joinRoom}
              </Button>
              <Button variant="contained" disabled={!isOwner || !room.canStart} onClick={() => void onStart()}>
                {locale.startQuest}
              </Button>
            </Stack>
          </>
        ) : null}
      </Stack>
    </Paper>
  )
}
