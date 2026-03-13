import { Alert, Button, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx, menuButtonSx, softGreenButtonSx } from '@/constants/styles'
import type { ListQuestRoomsResponse } from '@/schema/quest'

type QuestMultiRoomListProps = {
  rooms: ListQuestRoomsResponse
  isLoading: boolean
  error: Error | null
  isJoiningRoomId: string | null
  locale: {
    latestRoomsTitle: string
    latestRoomsSubtitle: string
    latestRoomsLoading: string
    latestRoomsEmpty: string
    joinRoom: string
    joiningRoom: string
    stage: string
    participants: string
    createdAt: string
    roomStatus: Record<'Recruiting' | 'Closed', string>
    modeMultiFixed: string
    roomStatusLabel: string
  }
  onJoinRoom: (roomId: string) => Promise<void>
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('ja-JP', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(new Date(value))
}

export default function QuestMultiRoomList({
  rooms,
  isLoading,
  error,
  isJoiningRoomId,
  locale,
  onJoinRoom,
}: QuestMultiRoomListProps) {
  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
      <Stack spacing={2}>
        <div>
          <Typography variant="h5">{locale.latestRoomsTitle}</Typography>
          <Typography variant="body2" color="text.secondary">
            {locale.latestRoomsSubtitle}
          </Typography>
        </div>

        {isLoading ? (
          <Stack direction="row" spacing={1} alignItems="center">
            <CircularProgress size={18} />
            <Typography variant="body2">{locale.latestRoomsLoading}</Typography>
          </Stack>
        ) : error ? (
          <Alert severity="warning">{error.message}</Alert>
        ) : rooms.length === 0 ? (
          <Typography variant="body2" color="text.secondary">
            {locale.latestRoomsEmpty}
          </Typography>
        ) : (
          <Stack spacing={1.5}>
            {rooms.map((room) => (
              <Paper
                key={room.roomId}
                variant="outlined"
                sx={{
                  borderRadius: 2,
                  p: 1.5,
                  backgroundColor: '#fffdf8',
                }}
              >
                <Stack spacing={1.25}>
                  <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                    <Chip size="small" label={locale.modeMultiFixed} />
                    <Chip size="small" label={`${locale.roomStatusLabel}: ${locale.roomStatus[room.status]}`} />
                  </Stack>
                  <Typography variant="subtitle2">{room.stageName ?? `${locale.stage}: ${room.stageId}`}</Typography>
                  <Typography variant="body2" color="text.secondary">
                    {`${locale.stage}: ${room.stageId}`}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {`${locale.participants}: ${room.participantCount}`}
                  </Typography>
                  <Typography variant="body2" color="text.secondary">
                    {`${locale.createdAt}: ${formatDateTime(room.createdAt)}`}
                  </Typography>
                  <Button
                    variant="contained"
                    onClick={() => void onJoinRoom(room.roomId)}
                    disabled={isJoiningRoomId === room.roomId}
                    sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                  >
                    {isJoiningRoomId === room.roomId ? locale.joiningRoom : locale.joinRoom}
                  </Button>
                </Stack>
              </Paper>
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
