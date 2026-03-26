import { Box, Chip, IconButton, Paper, Stack, SvgIcon, Tooltip, Typography } from '@mui/material'
import locale from '../../../locale/quest/QuestRoom.json'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'
import type { BattleColumn, BattleRow, QuestRoomDetailResponse } from '@/schema/quest'

type QuestRoomFormationPreviewProps = {
  currentRoom: QuestRoomDetailResponse
  selfParticipantId: string | null
  positionDrafts: Record<string, { row: BattleRow; column: BattleColumn }>
  onOpenPositionEditor: () => void
}

const rowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const columnOrder: BattleColumn[] = ['Left', 'Right']
const positionSlotHeight = 260

function GearIcon() {
  return (
    <SvgIcon fontSize="small" viewBox="0 0 24 24">
      <path
        d="M19.14 12.94c.04-.31.06-.63.06-.94s-.02-.63-.06-.94l2.03-1.58a.5.5 0 0 0 .12-.64l-1.92-3.32a.5.5 0 0 0-.6-.22l-2.39.96a7.03 7.03 0 0 0-1.63-.94l-.36-2.54a.5.5 0 0 0-.49-.42H10.1a.5.5 0 0 0-.49.42l-.36 2.54c-.58.23-1.12.55-1.63.94l-2.39-.96a.5.5 0 0 0-.6.22L2.71 8.84a.5.5 0 0 0 .12.64l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94l-2.03 1.58a.5.5 0 0 0-.12.64l1.92 3.32a.5.5 0 0 0 .6.22l2.39-.96c.5.39 1.05.71 1.63.94l.36 2.54a.5.5 0 0 0 .49.42h3.8a.5.5 0 0 0 .49-.42l.36-2.54c.58-.23 1.12-.55 1.63-.94l2.39.96a.5.5 0 0 0 .6-.22l1.92-3.32a.5.5 0 0 0-.12-.64l-2.03-1.58ZM12 15.5A3.5 3.5 0 1 1 12 8.5a3.5 3.5 0 0 1 0 7Z"
        fill="currentColor"
      />
    </SvgIcon>
  )
}

export default function QuestRoomFormationPreview({
  currentRoom,
  selfParticipantId,
  positionDrafts,
  onOpenPositionEditor,
}: QuestRoomFormationPreviewProps) {
  const playerParticipants = currentRoom.participants
    .filter((participant) => participant.type === 'Player')
    .map((participant) => ({
      ...participant,
      previewPosition: positionDrafts[participant.participantId] ?? participant.position,
    }))

  return (
    <Stack spacing={1.5}>
      <Stack direction="row" justifyContent="flex-end">
        <Tooltip title={locale.openPositionEditor}>
          <IconButton
            size="small"
            onClick={onOpenPositionEditor}
            aria-label={locale.openPositionEditor}
            sx={{
              border: '1px solid',
              borderColor: 'divider',
              backgroundColor: '#fffdfa',
            }}
          >
            <GearIcon />
          </IconButton>
        </Tooltip>
      </Stack>

      <Stack spacing={1.25}>
        {rowOrder.map((row) => (
          <Paper
            key={row}
            variant="outlined"
            sx={{
              borderRadius: 2.5,
              p: { xs: 1.25, sm: 1.5 },
              background:
                row === 'Front'
                  ? 'linear-gradient(180deg, #fff5ea 0%, #fffdf8 100%)'
                  : row === 'Middle'
                    ? 'linear-gradient(180deg, #f6fbf3 0%, #fffdf8 100%)'
                    : 'linear-gradient(180deg, #eef6fb 0%, #fffdf8 100%)',
            }}
          >
            <Stack spacing={1.25}>
              <Typography variant="subtitle2">{locale.rows[row]}</Typography>

              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.25}>
                {columnOrder.map((column) => {
                  const members = playerParticipants.filter(
                    (participant) =>
                      participant.previewPosition.row === row && participant.previewPosition.column === column,
                  )

                  return (
                    <Box key={column} sx={{ flex: 1, minWidth: 0 }}>
                      <Typography variant="caption" color="text.secondary" sx={{ display: 'block', mb: 0.75 }}>
                        {locale.columns[column]}
                      </Typography>

                      <Paper
                        variant="outlined"
                        sx={{
                          borderRadius: 2,
                          minHeight: positionSlotHeight,
                          p: 1,
                          backgroundColor: '#fffdfa',
                          borderStyle: 'dashed',
                        }}
                      >
                        {members.length > 0 ? (
                          <Stack
                            direction="row"
                            spacing={1}
                            useFlexGap
                            flexWrap="wrap"
                            sx={{ minHeight: positionSlotHeight - 16, alignItems: 'stretch' }}
                          >
                            {members.map((participant) => {
                              const jobImageSrc = participant.job?.code
                                ? resolveJobAssetPath(participant.job.code)
                                : null
                              const isSelf = participant.participantId === selfParticipantId

                              return (
                                <Paper
                                  key={participant.participantId}
                                  variant="outlined"
                                  sx={{
                                    width: '100%',
                                    minHeight: positionSlotHeight - 16,
                                    minWidth: 0,
                                    borderRadius: 2,
                                    p: 1,
                                    backgroundColor: isSelf ? '#fff6df' : '#ffffff',
                                  }}
                                >
                                  <Stack spacing={0.75}>
                                    <Stack direction="row" spacing={0.5} useFlexGap flexWrap="wrap">
                                      {participant.level != null ? (
                                        <Chip size="small" label={`${locale.levelLabel}.${participant.level}`} />
                                      ) : null}
                                      {participant.job?.displayName ? (
                                        <Chip
                                          size="small"
                                          label={participant.job.displayName}
                                          icon={
                                            jobImageSrc ? (
                                              <Box
                                                component="img"
                                                src={jobImageSrc}
                                                alt={participant.job.displayName}
                                                sx={{ width: 16, height: 16, objectFit: 'contain' }}
                                              />
                                            ) : undefined
                                          }
                                        />
                                      ) : null}
                                      {isSelf ? <Chip size="small" label={locale.selfBadge} /> : null}
                                    </Stack>

                                    <Box
                                      sx={{
                                        width: 140,
                                        maxWidth: '100%',
                                        aspectRatio: '4 / 5',
                                        borderRadius: 1.5,
                                        backgroundColor: '#f5efe2',
                                        overflow: 'hidden',
                                        display: 'grid',
                                        placeItems: 'center',
                                        alignSelf: 'center',
                                      }}
                                    >
                                      {participant.imagePath ? (
                                        <Box
                                          component="img"
                                          src={resolveCharacterAssetPath(participant.imagePath) ?? undefined}
                                          alt={participant.displayName}
                                          sx={{
                                            width: 'auto',
                                            height: '100%',
                                            maxWidth: '100%',
                                            objectFit: 'contain',
                                            objectPosition: 'center',
                                            display: 'block',
                                            mx: 'auto',
                                          }}
                                        />
                                      ) : (
                                        <Typography variant="caption" color="text.secondary">
                                          {locale.noImage}
                                        </Typography>
                                      )}
                                    </Box>

                                    <Typography variant="body2" noWrap title={participant.displayName}>
                                      {participant.displayName}
                                    </Typography>
                                  </Stack>
                                </Paper>
                              )
                            })}
                          </Stack>
                        ) : (
                          <Box
                            sx={{
                              minHeight: positionSlotHeight - 16,
                              display: 'grid',
                              placeItems: 'center',
                            }}
                          >
                            <Typography variant="body2" color="text.secondary">
                              {locale.emptyPosition}
                            </Typography>
                          </Box>
                        )}
                      </Paper>
                    </Box>
                  )
                })}
              </Stack>
            </Stack>
          </Paper>
        ))}
      </Stack>
    </Stack>
  )
}
