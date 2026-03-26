import { Alert, Avatar, Box, Button, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import useSWR from 'swr'
import { getPlayerById } from '@/api/player'
import { innerSurfaceSx, menuButtonSx, softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type { GetQuestStagesResponse, ListQuestRoomsResponse } from '@/schema/quest'

type QuestMultiRoomListProps = {
  rooms: ListQuestRoomsResponse
  stages: GetQuestStagesResponse
  accessToken: string | null | undefined
  isLoading: boolean
  error: Error | null
  isJoiningRoomId: string | null
  locale: {
    latestRoomsLoading: string
    latestRoomsEmpty: string
    joinRoom: string
    joiningRoom: string
    recommendedLevel: string
    noImage?: string
  }
  onJoinRoom: (roomId: string) => Promise<void>
}

type QuestMultiRoomCardProps = {
  room: ListQuestRoomsResponse[number]
  stages: GetQuestStagesResponse
  accessToken: string | null | undefined
  isJoining: boolean
  locale: QuestMultiRoomListProps['locale']
  onJoinRoom: (roomId: string) => Promise<void>
}

function QuestMultiRoomCard({
  room,
  stages,
  accessToken,
  isJoining,
  locale,
  onJoinRoom,
}: QuestMultiRoomCardProps) {
  const stage = stages.find((item) => item.stageId === room.stageId)
  const ownerSWRKey = accessToken ? (['quest-room-owner', room.ownerPlayerId] as const) : null
  const { data: owner } = useSWR(ownerSWRKey, async () => getPlayerById(room.ownerPlayerId, accessToken!))

  const ownerImageSrc = resolveCharacterAssetPath(owner?.imagePath)

  return (
    <Paper
      variant="outlined"
      sx={{
        borderRadius: 3,
        p: 1.5,
        backgroundColor: '#fffdf8',
      }}
    >
      <Stack direction="row" spacing={1.5} alignItems="center">
        <Box
          sx={{
            width: 64,
            height: 64,
            borderRadius: '50%',
            overflow: 'hidden',
            flexShrink: 0,
            display: 'grid',
            placeItems: 'center',
            background: 'radial-gradient(circle at 30% 30%, #fff6de 0%, #f0d5a0 100%)',
            border: '2px solid #e2bf7b',
          }}
        >
          {stage?.previewEnemyImagePath ? (
            <Box
              component="img"
              src={resolvePublicAssetPath(stage.previewEnemyImagePath)}
              alt={stage.name}
              sx={{ width: '100%', height: '100%', objectFit: 'cover' }}
            />
          ) : (
            <Typography variant="caption" color="text.secondary">
              {locale.noImage ?? 'No Image'}
            </Typography>
          )}
        </Box>

        <Stack spacing={0.5} sx={{ flex: 1, minWidth: 0 }}>
          <Typography variant="h6" noWrap title={stage?.name ?? room.stageName ?? String(room.stageId)}>
            {stage?.name ?? room.stageName ?? String(room.stageId)}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {`${locale.recommendedLevel} ${stage?.recommendedLevel ?? '-'}`}
          </Typography>
        </Stack>

        <Button
          variant="contained"
          onClick={() => void onJoinRoom(room.roomId)}
          disabled={isJoining}
          sx={{ ...menuButtonSx, ...softGreenButtonSx, width: 'auto', minWidth: 104, px: 2 }}
        >
          {isJoining ? locale.joiningRoom : locale.joinRoom}
        </Button>

        <Stack spacing={0.5} alignItems="center" sx={{ flexShrink: 0, minWidth: 72 }}>
          <Avatar
            src={ownerImageSrc ?? undefined}
            alt={owner?.userName ?? room.ownerDisplayName ?? 'owner'}
            sx={{ width: 56, height: 56, bgcolor: '#efe4cf', color: '#6a5320', fontWeight: 700 }}
          >
            {(owner?.userName ?? room.ownerDisplayName ?? '?').slice(0, 1)}
          </Avatar>
          <Typography
            variant="caption"
            color="text.secondary"
            sx={{ maxWidth: 72, textAlign: 'center' }}
            noWrap
            title={owner?.userName ?? room.ownerDisplayName ?? '---'}
          >
            {owner?.userName ?? room.ownerDisplayName ?? '---'}
          </Typography>
        </Stack>
      </Stack>
    </Paper>
  )
}

export default function QuestMultiRoomList({
  rooms,
  stages,
  accessToken,
  isLoading,
  error,
  isJoiningRoomId,
  locale,
  onJoinRoom,
}: QuestMultiRoomListProps) {
  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
      <Stack spacing={2}>
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
              <QuestMultiRoomCard
                key={room.roomId}
                room={room}
                stages={stages}
                accessToken={accessToken}
                isJoining={isJoiningRoomId === room.roomId}
                locale={locale}
                onJoinRoom={onJoinRoom}
              />
            ))}
          </Stack>
        )}
      </Stack>
    </Paper>
  )
}
