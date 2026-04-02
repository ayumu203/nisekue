import { Box, Chip, Paper, Stack, Typography } from '@mui/material'
import locale from '../../../locale/quest/QuestRoom.json'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'
import type { BattleColumn, BattleRow, QuestRoomDetailResponse } from '@/schema/quest'

type QuestRoomFormationPreviewProps = {
  currentRoom: QuestRoomDetailResponse
  selfParticipantId: string | null
  positionDrafts: Record<string, { row: BattleRow; column: BattleColumn }>
}

const rowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const columnOrder: BattleColumn[] = ['Left', 'Right']
const positionSlotHeight = 220

export default function QuestRoomFormationPreview({
  currentRoom,
  selfParticipantId,
  positionDrafts,
}: QuestRoomFormationPreviewProps) {
  const playerParticipants = currentRoom.participants
    .filter((participant) => participant.type === 'Player')
    .map((participant) => ({
      ...participant,
      previewPosition: positionDrafts[participant.participantId] ?? participant.position,
    }))

  return (
    <Stack spacing={1}>
      {rowOrder.map((row) => (
        <Paper
          key={row}
          variant="outlined"
          sx={{
            borderRadius: 2.5,
            p: { xs: 1, sm: 1.5 },
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

            <Stack direction="row" spacing={1}>
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
                            const jobImageSrc = participant.job?.code ? resolveJobAssetPath(participant.job.code) : null
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
                                      width: { xs: 112, sm: 140 },
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
  )
}
