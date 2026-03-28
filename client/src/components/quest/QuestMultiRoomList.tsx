import { Alert, Avatar, Box, Button, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx, menuButtonSx, softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type { GetQuestStagesResponse, ListQuestRoomsResponse } from '@/schema/quest'

type QuestMultiRoomListProps = {
  rooms: ListQuestRoomsResponse
  stages: GetQuestStagesResponse
  isLoading: boolean
  error: Error | null
  isJoiningRoomId: string | null
  locale: {
    latestRoomsLoading: string
    latestRoomsEmpty: string
    joinRoom: string
    joiningRoom: string
    recommendedLevel: string
    ownerBadge: string
    noImage?: string
  }
  onJoinRoom: (roomId: string) => Promise<void>
}

type QuestMultiRoomCardProps = {
  room: ListQuestRoomsResponse[number]
  stages: GetQuestStagesResponse
  isJoining: boolean
  locale: QuestMultiRoomListProps['locale']
  onJoinRoom: (roomId: string) => Promise<void>
}

function QuestMultiRoomCard({ room, stages, isJoining, locale, onJoinRoom }: QuestMultiRoomCardProps) {
  const stage = stages.find((item) => item.stageId === room.stageId)
  const ownerImageSrc = resolveCharacterAssetPath(room.ownerImagePath)
  const battlefieldImageSrc = resolvePublicAssetPath(stage?.battlefieldImagePath ?? 'image/quest/dummy-battlefield.svg')
  const participantSummary =
    room.maxPartyMemberCount != null ? `${room.participantCount} / ${room.maxPartyMemberCount}人` : `${room.participantCount}人`
  const roomModeLabel = room.mode === 'Solo' ? 'ソロ' : 'マルチ'

  return (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        position: 'relative',
        overflow: 'hidden',
        borderRadius: 3,
        p: 1.5,
        backgroundColor: '#fffdf8',
        borderColor: '#e7d9b6',
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          backgroundImage: `linear-gradient(135deg, rgba(255, 250, 240, 0.94), rgba(255, 247, 232, 0.8)), url(${battlefieldImageSrc})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          opacity: 0.95,
        },
      }}
    >
      <Stack spacing={1.25} sx={{ position: 'relative', zIndex: 1 }}>
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
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
            <Typography
              variant="h6"
              sx={{
                fontSize: { xs: '1.15rem', sm: '1.35rem' },
                fontWeight: 800,
                lineHeight: 1.3,
                wordBreak: 'break-word',
              }}
              title={stage?.name ?? room.stageName ?? String(room.stageId)}
            >
              {stage?.name ?? room.stageName ?? String(room.stageId)}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {`${locale.recommendedLevel} ${stage?.recommendedLevel ?? '-'}`}
            </Typography>
            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <Chip
                size="small"
                label={roomModeLabel}
                sx={{
                  fontWeight: 700,
                  bgcolor: room.mode === 'Solo' ? '#eef3ff' : '#fff0f3',
                  color: room.mode === 'Solo' ? '#3A66D6' : '#C9485B',
                }}
              />
              <Chip
                size="small"
                label={participantSummary}
                variant="outlined"
                sx={{
                  fontWeight: 700,
                  bgcolor: 'rgba(255,255,255,0.72)',
                }}
              />
            </Stack>
          </Stack>
        </Stack>

        <Stack direction="row" spacing={1.25} alignItems="flex-end" justifyContent="space-between">
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
              alt={room.ownerDisplayName ?? 'owner'}
              sx={{ width: 56, height: 56, bgcolor: '#efe4cf', color: '#6a5320', fontWeight: 700 }}
            >
              {(room.ownerDisplayName ?? '?').slice(0, 1)}
            </Avatar>
            <Typography
              variant="caption"
              color="text.secondary"
              sx={{ maxWidth: 72, textAlign: 'center' }}
              noWrap
              title={room.ownerDisplayName ?? '---'}
            >
              {room.ownerDisplayName ?? '---'}
            </Typography>
            <Typography variant="caption" color="text.secondary">
              {locale.ownerBadge}
            </Typography>
          </Stack>
        </Stack>
      </Stack>
    </Paper>
  )
}

export default function QuestMultiRoomList({
  rooms,
  stages,
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
