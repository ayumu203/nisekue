import { Alert, Avatar, Box, Button, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { homePanelColors, innerSurfaceSx, menuButtonSx, panelPrimaryButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type { GetQuestStagesResponse, ListQuestRoomsResponse } from '@/schema/quest'
import { font } from '@/theme/tokens'

function formatCooldownRemainingMessage(seconds: number, template: string) {
  const floorSeconds = Math.max(0, seconds)
  const minutes = Math.floor(floorSeconds / 60)
  const secondPart = floorSeconds % 60
  return template.replace('{minutes}', String(minutes)).replace('{seconds}', String(secondPart).padStart(2, '0'))
}

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
    minRequiredLevel: string
    allowedPlayersOnly: string
    joinDisabledReasons: Record<string, string>
    cooldownRemaining: string
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
    room.maxPartyMemberCount != null
      ? `${room.participantCount} / ${room.maxPartyMemberCount}人`
      : `${room.participantCount}人`
  const roomModeLabel = room.mode === 'Solo' ? 'ソロ' : 'マルチ'
  const displayedMinimumLevel = room.minRequiredLevel ?? stage?.minimumEntryLevel ?? null
  const cooldownRemainingText =
    room.joinDisabledReason === 'CooldownActive' && room.cooldownRemainingSeconds != null
      ? formatCooldownRemainingMessage(room.cooldownRemainingSeconds, locale.cooldownRemaining)
      : null

  return (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        position: 'relative',
        overflow: 'hidden',
        borderRadius: 3,
        p: 1.5,
        color: '#f4f7ff',
        backgroundColor: '#243654',
        borderColor: 'rgba(152, 192, 255, 0.3)',
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          backgroundImage: `linear-gradient(135deg, rgba(18, 29, 48, 0.8), rgba(32, 49, 80, 0.68)), url(${battlefieldImageSrc})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          opacity: 0.95,
        },
      }}
    >
      <Stack spacing={1.5} sx={{ position: 'relative', zIndex: 1 }}>
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
              background: 'radial-gradient(circle at 30% 30%, #eef4ff 0%, #9cbbe6 100%)',
              border: '2px solid #a9c3eb',
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
                fontSize: { xs: font.lg, sm: font.xl },
                fontWeight: 800,
                color: '#ffffff',
                lineHeight: 1.3,
                wordBreak: 'break-word',
              }}
              title={stage?.name ?? room.stageName ?? String(room.stageId)}
            >
              {stage?.name ?? room.stageName ?? String(room.stageId)}
            </Typography>
            <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
              {`${locale.recommendedLevel} ${stage?.recommendedLevel ?? '-'}`}
            </Typography>
            {displayedMinimumLevel != null ? (
              <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.76)' }}>
                {`${locale.minRequiredLevel} ${displayedMinimumLevel}`}
              </Typography>
            ) : null}
            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <Chip
                size="small"
                label={roomModeLabel}
                sx={{
                  fontWeight: 700,
                  bgcolor: room.mode === 'Solo' ? 'rgba(122, 180, 255, 0.2)' : 'rgba(158, 216, 206, 0.2)',
                  color: room.mode === 'Solo' ? '#ddebff' : '#d6f5ef',
                  border: '1px solid',
                  borderColor: room.mode === 'Solo' ? 'rgba(122, 180, 255, 0.38)' : 'rgba(158, 216, 206, 0.38)',
                }}
              />
              <Chip
                size="small"
                label={participantSummary}
                variant="outlined"
                sx={{
                  fontWeight: 700,
                  color: '#edf5ff',
                  bgcolor: 'rgba(255,255,255,0.08)',
                  borderColor: 'rgba(222, 236, 255, 0.34)',
                }}
              />
              {room.hasAllowedPlayerRestriction ? (
                <Chip
                  size="small"
                  label={locale.allowedPlayersOnly}
                  sx={{
                    fontWeight: 700,
                    color: '#fce8b2',
                    bgcolor: 'rgba(241, 194, 80, 0.16)',
                    border: '1px solid rgba(241, 194, 80, 0.3)',
                  }}
                />
              ) : null}
            </Stack>
          </Stack>
        </Stack>

        <Stack direction="row" spacing={1.5} alignItems="flex-end" justifyContent="space-between">
          <Button
            variant="contained"
            onClick={() => void onJoinRoom(room.roomId)}
            disabled={isJoining || !room.isJoinable}
            sx={{
              ...menuButtonSx,
              ...panelPrimaryButtonSx,
              width: 'auto',
              minWidth: 104,
              px: 2,
            }}
          >
            {isJoining ? locale.joiningRoom : locale.joinRoom}
          </Button>

          <Stack spacing={0.5} alignItems="center" sx={{ flexShrink: 0, minWidth: 72 }}>
            <Avatar
              src={ownerImageSrc ?? undefined}
              alt={room.ownerDisplayName ?? 'owner'}
              sx={{ width: 56, height: 56, bgcolor: '#d8e8ff', color: '#274163', fontWeight: 700 }}
            >
              {(room.ownerDisplayName ?? '?').slice(0, 1)}
            </Avatar>
            <Typography
              variant="caption"
              fontSize={font.sm}
              sx={{ maxWidth: 72, textAlign: 'center', color: 'rgba(222, 236, 255, 0.76)' }}
              noWrap
              title={room.ownerDisplayName ?? '---'}
            >
              {room.ownerDisplayName ?? '---'}
            </Typography>
            <Typography variant="caption" sx={{ color: 'rgba(222, 236, 255, 0.64)', fontSize: font.sm }}>
              {locale.ownerBadge}
            </Typography>
          </Stack>
        </Stack>
        {!room.isJoinable && room.joinDisabledReason ? (
          <Stack spacing={0.25}>
            <Typography variant="body2" sx={{ color: '#ffd7d7' }}>
              {locale.joinDisabledReasons[room.joinDisabledReason] ?? room.joinDisabledReason}
            </Typography>
            {cooldownRemainingText ? (
              <Typography variant="body2" sx={{ color: '#ffd7d7' }}>
                {cooldownRemainingText}
              </Typography>
            ) : null}
          </Stack>
        ) : null}
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
  // 白い箱（GamePanel）の中に置くので、外側の枠は付けない
  return (
    <Stack spacing={2}>
      {isLoading ? (
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={18} />
          <Typography variant="body2" sx={{ color: homePanelColors.label }}>
            {locale.latestRoomsLoading}
          </Typography>
        </Stack>
      ) : error ? (
        <Alert severity="warning">{error.message}</Alert>
      ) : rooms.length === 0 ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
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
  )
}
