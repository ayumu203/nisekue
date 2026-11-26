import { Alert, Avatar, Box, Button, Chip, CircularProgress, Paper, Stack, Typography } from '@mui/material'
import { homePanelColors, innerSurfaceSx, menuButtonSx, panelPrimaryButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type { GetQuestStagesResponse, ListQuestRoomsResponse } from '@/schema/quest'
import { font, palette, withAlpha } from '@/theme/tokens'

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
        color: palette.sky,
        backgroundColor: palette.navy,
        borderColor: withAlpha(palette.blue300, 0.25),
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          backgroundImage: `linear-gradient(135deg, ${withAlpha(palette.navy, 0.75)}, ${withAlpha(palette.navy, 0.75)}), url(${battlefieldImageSrc})`,
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
              background: `radial-gradient(circle at 30% 30%, ${palette.sky} 0%, ${palette.blue300} 100%)`,
              border: `2px solid ${palette.blue300}`,
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
                color: palette.white,
                lineHeight: 1.3,
                wordBreak: 'break-word',
              }}
              title={stage?.name ?? room.stageName ?? String(room.stageId)}
            >
              {stage?.name ?? room.stageName ?? String(room.stageId)}
            </Typography>
            <Typography variant="body2" sx={{ color: withAlpha(palette.sky, 0.75) }}>
              {`${locale.recommendedLevel} ${stage?.recommendedLevel ?? '-'}`}
            </Typography>
            {displayedMinimumLevel != null ? (
              <Typography variant="body2" sx={{ color: withAlpha(palette.sky, 0.75) }}>
                {`${locale.minRequiredLevel} ${displayedMinimumLevel}`}
              </Typography>
            ) : null}
            <Stack direction="row" spacing={0.75} flexWrap="wrap" useFlexGap>
              <Chip
                size="small"
                label={roomModeLabel}
                sx={{
                  fontWeight: 700,
                  bgcolor: room.mode === 'Solo' ? withAlpha(palette.blue300, 0.25) : withAlpha(palette.teal400, 0.25),
                  color: room.mode === 'Solo' ? palette.sky : palette.sky,
                  border: '1px solid',
                  borderColor: room.mode === 'Solo' ? withAlpha(palette.blue300, 0.5) : withAlpha(palette.teal400, 0.5),
                }}
              />
              <Chip
                size="small"
                label={participantSummary}
                variant="outlined"
                sx={{
                  fontWeight: 700,
                  color: palette.sky,
                  bgcolor: withAlpha(palette.white, 0.1),
                  borderColor: withAlpha(palette.sky, 0.25),
                }}
              />
              {room.hasAllowedPlayerRestriction ? (
                <Chip
                  size="small"
                  label={locale.allowedPlayersOnly}
                  sx={{
                    fontWeight: 700,
                    color: palette.brown200,
                    bgcolor: withAlpha(palette.gold300, 0.1),
                    border: `1px solid ${withAlpha(palette.gold300, 0.25)}`,
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
              sx={{ width: 56, height: 56, bgcolor: palette.sky, color: palette.teal700, fontWeight: 700 }}
            >
              {(room.ownerDisplayName ?? '?').slice(0, 1)}
            </Avatar>
            <Typography
              variant="caption"
              fontSize={font.sm}
              sx={{ maxWidth: 72, textAlign: 'center', color: withAlpha(palette.sky, 0.75) }}
              noWrap
              title={room.ownerDisplayName ?? '---'}
            >
              {room.ownerDisplayName ?? '---'}
            </Typography>
            <Typography variant="caption" sx={{ color: withAlpha(palette.sky, 0.75), fontSize: font.sm }}>
              {locale.ownerBadge}
            </Typography>
          </Stack>
        </Stack>
        {!room.isJoinable && room.joinDisabledReason ? (
          <Stack spacing={0.25}>
            <Typography variant="body2" sx={{ color: palette.gray300 }}>
              {locale.joinDisabledReasons[room.joinDisabledReason] ?? room.joinDisabledReason}
            </Typography>
            {cooldownRemainingText ? (
              <Typography variant="body2" sx={{ color: palette.gray300 }}>
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
