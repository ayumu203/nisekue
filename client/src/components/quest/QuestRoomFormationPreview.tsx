import { Box, Stack, Typography } from '@mui/material'
import { Fragment } from 'react'
import locale from '../../../locale/quest/QuestRoom.json'
import { homePanelColors } from '@/constants/styles'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'
import type { BattleColumn, BattleRow, QuestRoomDetailResponse } from '@/schema/quest'

type QuestRoomFormationPreviewProps = {
  currentRoom: QuestRoomDetailResponse
  positionDrafts: Record<string, { row: BattleRow; column: BattleColumn }>
}

const rowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const columnOrder: BattleColumn[] = ['Left', 'Right']
const rowBackground: Record<BattleRow, string> = {
  Front: '#fff5ea',
  Middle: '#f6fbf3',
  Back: '#eef6fb',
}

// 隊列の確認用の表。前列・中列・後列 × 左・右のマスに、キャラの小さい画像と名前を並べる
export default function QuestRoomFormationPreview({ currentRoom, positionDrafts }: QuestRoomFormationPreviewProps) {
  const playerParticipants = currentRoom.participants
    .filter((participant) => participant.type === 'Player')
    .map((participant) => ({
      ...participant,
      previewPosition: positionDrafts[participant.participantId] ?? participant.position,
    }))

  return (
    <Box sx={{ display: 'grid', gridTemplateColumns: '56px repeat(2, minmax(0, 1fr))', gap: 1 }}>
      <Box />
      {columnOrder.map((column) => (
        <Typography key={column} sx={{ fontSize: '0.8125rem', color: homePanelColors.label, textAlign: 'center' }}>
          {locale.columns[column]}
        </Typography>
      ))}
      {rowOrder.map((row) => (
        <Fragment key={row}>
          <Box
            sx={{
              display: 'grid',
              placeItems: 'center',
              borderRadius: 2,
              backgroundColor: rowBackground[row],
              fontSize: '0.875rem',
              fontWeight: 700,
              color: '#5a4421',
            }}
          >
            {locale.rows[row]}
          </Box>
          {columnOrder.map((column) => {
            const members = playerParticipants.filter(
              (participant) => participant.previewPosition.row === row && participant.previewPosition.column === column,
            )

            return (
              <Box
                key={column}
                sx={{
                  minHeight: 72,
                  borderRadius: 2,
                  border: '1px dashed',
                  borderColor: homePanelColors.divider,
                  backgroundColor: rowBackground[row],
                  px: 1.25,
                  py: 0.75,
                  display: 'flex',
                  flexDirection: 'column',
                  justifyContent: 'center',
                  gap: 0.5,
                }}
              >
                {members.length === 0 ? (
                  <Typography sx={{ fontSize: '0.875rem', color: homePanelColors.label, textAlign: 'center' }}>
                    {locale.emptyPosition}
                  </Typography>
                ) : (
                  members.map((participant) => {
                    const jobImageSrc = participant.job?.code ? resolveJobAssetPath(participant.job.code) : null

                    return (
                      <Stack key={participant.participantId} direction="row" spacing={1.25} alignItems="center">
                        <Box
                          sx={{
                            width: 48,
                            height: 60,
                            flexShrink: 0,
                            borderRadius: 1.5,
                            backgroundColor: '#efe4c8',
                            overflow: 'hidden',
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
                          ) : null}
                        </Box>
                        <Stack spacing={0.25} sx={{ minWidth: 0 }}>
                          <Typography
                            noWrap
                            title={participant.displayName}
                            sx={{ fontSize: '1rem', fontWeight: 700, color: homePanelColors.text }}
                          >
                            {participant.displayName}
                          </Typography>
                          <Stack direction="row" spacing={0.75} alignItems="center">
                            {participant.level != null ? (
                              <Typography sx={{ fontSize: '0.875rem', color: homePanelColors.label }}>
                                {`${locale.levelLabel}.${participant.level}`}
                              </Typography>
                            ) : null}
                            {jobImageSrc ? (
                              <Box
                                component="img"
                                src={jobImageSrc}
                                alt=""
                                sx={{ width: 16, height: 16, objectFit: 'contain' }}
                              />
                            ) : null}
                            {participant.job?.displayName ? (
                              <Typography noWrap sx={{ fontSize: '0.875rem', color: homePanelColors.label }}>
                                {participant.job.displayName}
                              </Typography>
                            ) : null}
                          </Stack>
                        </Stack>
                      </Stack>
                    )
                  })
                )}
              </Box>
            )
          })}
        </Fragment>
      ))}
    </Box>
  )
}
