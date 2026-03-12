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
import type { GetQuestStagesResponse, ListQuestRoomsResponse } from '@/schema/quest'
import { innerSurfaceSx } from '@/constants/styles'
import locale from '../../../locale/quest/Quest.json'

type QuestRoomListProps = {
  stages: GetQuestStagesResponse | undefined
  rooms: ListQuestRoomsResponse | undefined
  isStagesLoading: boolean
  isRoomsLoading: boolean
  roomsError: string | null
  selectedRoomId: string | null
  createStageId: number | ''
  createMode: 'Solo' | 'Multi'
  onChangeStageId: (stageId: number) => void
  onChangeMode: (mode: 'Solo' | 'Multi') => void
  onCreateRoom: () => Promise<void>
  onSelectRoom: (roomId: string) => void
  onRefresh: () => Promise<unknown>
}

function formatRoomMode(mode: 'Solo' | 'Multi') {
  return mode === 'Solo' ? locale.modeSolo : locale.modeMulti
}

function formatRoomStatus(status: 'Recruiting' | 'Closed') {
  return status === 'Recruiting' ? locale.statusRecruiting : locale.statusClosed
}

export default function QuestRoomList({
  stages,
  rooms,
  isStagesLoading,
  isRoomsLoading,
  roomsError,
  selectedRoomId,
  createStageId,
  createMode,
  onChangeStageId,
  onChangeMode,
  onCreateRoom,
  onSelectRoom,
  onRefresh,
}: QuestRoomListProps) {
  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
        <Stack spacing={2}>
          <Typography variant="h5">{locale.title}</Typography>
          <Typography variant="body2" color="text.secondary">
            {locale.subtitle}
          </Typography>
          <TextField
            select
            label={locale.labels.stageSelect}
            value={createStageId}
            onChange={(event) => onChangeStageId(Number(event.target.value))}
            disabled={isStagesLoading || !stages?.length}
          >
            {stages?.length ? (
              stages.map((stage) => (
                <MenuItem key={stage.stageId} value={stage.stageId}>
                  {stage.name} / {locale.recommendedLevel} {stage.recommendedLevel}
                </MenuItem>
              ))
            ) : (
              <MenuItem value="" disabled>
                {locale.stagesEmpty}
              </MenuItem>
            )}
          </TextField>
          <TextField
            select
            label={locale.labels.modeSelect}
            value={createMode}
            onChange={(event) => onChangeMode(event.target.value as 'Solo' | 'Multi')}
          >
            <MenuItem value="Solo">{locale.modeSolo}</MenuItem>
            <MenuItem value="Multi">{locale.modeMulti}</MenuItem>
          </TextField>
          <Button variant="contained" onClick={() => void onCreateRoom()} disabled={!createStageId}>
            {locale.createRoom}
          </Button>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
        <Stack spacing={2}>
          <Stack direction="row" justifyContent="space-between" alignItems="center">
            <Typography variant="h6">{locale.participants}</Typography>
            <Button variant="outlined" onClick={() => void onRefresh()}>
              {locale.refreshRooms}
            </Button>
          </Stack>
          {roomsError ? <Alert severity="warning">{roomsError}</Alert> : null}
          {isRoomsLoading ? <Typography variant="body2">{locale.roomsLoading}</Typography> : null}
          {!isRoomsLoading && !rooms?.length ? <Alert severity="info">{locale.noRooms}</Alert> : null}
          <Stack spacing={1.5}>
            {rooms?.map((room) => (
              <Paper
                key={room.roomId}
                variant="outlined"
                sx={{
                  borderRadius: 2,
                  p: 1.5,
                  borderColor: room.roomId === selectedRoomId ? '#c8b894' : '#e7d9b6',
                  backgroundColor: room.roomId === selectedRoomId ? '#fff2d6' : '#fffaf0',
                }}
              >
                <Stack spacing={1}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                    <Typography fontWeight={700}>{room.stageName ?? `${locale.stage} #${room.stageId}`}</Typography>
                    <Chip size="small" label={formatRoomStatus(room.status)} />
                  </Stack>
                  <Typography variant="body2" color="text.secondary">
                    {locale.ownerLabel}: {room.ownerDisplayName ?? room.ownerPlayerId}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {formatRoomMode(room.mode)} / {room.participantCount}
                    {room.maxPartyMemberCount ? ` / ${room.maxPartyMemberCount}` : ''}
                  </Typography>
                  <Button variant="outlined" onClick={() => onSelectRoom(room.roomId)}>
                    {room.roomId === selectedRoomId ? locale.roomSelected : '詳細を見る'}
                  </Button>
                </Stack>
              </Paper>
            ))}
          </Stack>
        </Stack>
      </Paper>
    </Stack>
  )
}
