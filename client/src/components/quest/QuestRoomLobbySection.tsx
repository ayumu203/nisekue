import {
  Box,
  Button,
  Chip,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import CancelIcon from '@mui/icons-material/Cancel'
import PlayArrowIcon from '@mui/icons-material/PlayArrow'
import type { ReactNode } from 'react'
import QuestRoomFormationPreview from './QuestRoomFormationPreview'
import {
  greenOutlinedInputSx,
  homePanelColors,
  menuButtonSx,
  panelCancelButtonSx,
  panelPrimaryButtonSx,
} from '@/constants/styles'
import locale from '../../../locale/quest/QuestRoom.json'
import type { BattleColumn, BattleRow, QuestRoomDetailResponse } from '@/schema/quest'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'

type QuestRoomLobbySectionProps = {
  currentRoom: QuestRoomDetailResponse | null
  selfParticipantId: string | null
  positionDrafts: Record<string, { row: BattleRow; column: BattleColumn }>
  isLoading: boolean
  isStarting: boolean
  isCancellingRoom: boolean
  isSavingFormation: boolean
  onPositionDraftChange: (participantId: string, nextPosition: { row: BattleRow; column: BattleColumn }) => void
  onStartQuest: () => void | Promise<void>
  onCancelRoom: () => void | Promise<void>
  // 隊列の確認と配置変更（START・CANCEL）をどの箱に置くかは画面側で決める
  renderLayout: (parts: { info: ReactNode; editor: ReactNode; editorTitle: string }) => ReactNode
}

// 列・位置の選択欄。上に縮んで出る「列」「位置」の文字を 13px（ホームの最小の文字）にする
const positionSelectSx = {
  ...greenOutlinedInputSx,
  '& .MuiInputLabel-shrink': {
    transform: 'translate(14px, -9px) scale(0.8125)',
  },
} as const

export default function QuestRoomLobbySection({
  currentRoom,
  selfParticipantId,
  positionDrafts,
  isLoading,
  isStarting,
  isCancellingRoom,
  isSavingFormation,
  onPositionDraftChange,
  onStartQuest,
  onCancelRoom,
  renderLayout,
}: QuestRoomLobbySectionProps) {
  const isOwner =
    currentRoom != null && selfParticipantId != null
      ? currentRoom.participants.some(
          (participant) => participant.participantId === selfParticipantId && participant.isOwner,
        )
      : false
  const isStartDisabled =
    currentRoom == null ||
    isStarting ||
    isCancellingRoom ||
    !currentRoom.canStart ||
    currentRoom.status !== 'Recruiting'

  function handleRowChange(
    participantId: string,
    draft: { row: BattleRow; column: BattleColumn },
    event: SelectChangeEvent<BattleRow>,
  ) {
    onPositionDraftChange(participantId, {
      row: event.target.value as BattleRow,
      column: draft.column,
    })
  }

  function handleColumnChange(
    participantId: string,
    draft: { row: BattleRow; column: BattleColumn },
    event: SelectChangeEvent<BattleColumn>,
  ) {
    onPositionDraftChange(participantId, {
      row: draft.row,
      column: event.target.value as BattleColumn,
    })
  }

  const editorTitle = isOwner ? locale.ownerPositionTitle : locale.waitingForOwnerTitle

  if (currentRoom == null || isLoading) {
    const message =
      currentRoom == null ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          {locale.createdRoomEmpty}
        </Typography>
      ) : (
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={18} />
          <Typography variant="body2" sx={{ color: homePanelColors.label }}>
            {locale.roomLoading}
          </Typography>
        </Stack>
      )
    return <>{renderLayout({ info: message, editor: null, editorTitle })}</>
  }

  const isRecruiting = currentRoom.status === 'Recruiting'

  // 参加制限・募集中の表示はルーム作成の画面で済んでいるので出さず、隊列だけを出す
  const info = (
    <Stack spacing={2}>
      <QuestRoomFormationPreview currentRoom={currentRoom} positionDrafts={positionDrafts} />

      {!isRecruiting ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          {locale.roomClosedMessage}
        </Typography>
      ) : null}
    </Stack>
  )

  const editor = (
    <Stack spacing={1.5}>
      {!isOwner && locale.waitingForOwnerSubtitle ? (
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          {locale.waitingForOwnerSubtitle}
        </Typography>
      ) : null}
      {currentRoom.participants.map((participant) => {
        const draft = positionDrafts[participant.participantId] ?? participant.position
        const jobImageSrc = participant.job?.code ? resolveJobAssetPath(participant.job.code) : null

        return (
          // 箱にはせず、点線で区切った一覧にする
          <Box
            key={participant.participantId}
            sx={{
              py: 1.5,
              color: homePanelColors.text,
              borderBottom: `1px dotted ${homePanelColors.divider}`,
              '&:first-of-type': { pt: 0 },
            }}
          >
            <Stack direction="row" spacing={{ xs: 1, sm: 1.5 }} alignItems="flex-start">
              <Box
                sx={{
                  width: { xs: 64, sm: 100 },
                  minWidth: { xs: 64, sm: 100 },
                  aspectRatio: '4 / 5',
                  borderRadius: 2,
                  backgroundColor: '#efe4c8',
                  overflow: 'hidden',
                  display: 'grid',
                  placeItems: 'center',
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
                ) : (
                  <Typography variant="caption" sx={{ color: homePanelColors.label }}>
                    {locale.noImage}
                  </Typography>
                )}
              </Box>

              <Stack spacing={1} sx={{ flex: 1, minWidth: 0 }}>
                <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap" alignItems="center">
                  <Typography variant="subtitle1" fontWeight={800} sx={{ color: homePanelColors.text }}>
                    {participant.displayName}
                  </Typography>
                  {participant.isOwner ? (
                    <Chip
                      size="small"
                      label={locale.ownerBadge}
                      sx={{ color: '#3a2400', backgroundColor: '#ffcc00', fontWeight: 800 }}
                    />
                  ) : null}
                </Stack>

                <Stack direction="row" spacing={1.5} alignItems="center" sx={{ flexWrap: 'nowrap', minWidth: 0 }}>
                  {participant.level != null ? (
                    <Typography
                      variant="body2"
                      sx={{ whiteSpace: 'nowrap', flexShrink: 0, color: homePanelColors.text, fontSize: '1rem' }}
                    >
                      {`${locale.levelLabel}.${participant.level}`}
                    </Typography>
                  ) : null}
                </Stack>

                {isOwner ? (
                  <Stack direction="row" spacing={{ xs: 0.75, sm: 1.25 }}>
                    <FormControl fullWidth size="small" sx={positionSelectSx}>
                      <InputLabel id={`participant-row-${participant.participantId}`}>{locale.positionRow}</InputLabel>
                      <Select
                        labelId={`participant-row-${participant.participantId}`}
                        value={draft.row}
                        label={locale.positionRow}
                        disabled={isSavingFormation || !isRecruiting}
                        onChange={(event) => handleRowChange(participant.participantId, draft, event)}
                      >
                        <MenuItem value="Front">{locale.rows.Front}</MenuItem>
                        <MenuItem value="Middle">{locale.rows.Middle}</MenuItem>
                        <MenuItem value="Back">{locale.rows.Back}</MenuItem>
                      </Select>
                    </FormControl>

                    <FormControl fullWidth size="small" sx={positionSelectSx}>
                      <InputLabel id={`participant-column-${participant.participantId}`}>
                        {locale.positionColumn}
                      </InputLabel>
                      <Select
                        labelId={`participant-column-${participant.participantId}`}
                        value={draft.column}
                        label={locale.positionColumn}
                        disabled={isSavingFormation || !isRecruiting}
                        onChange={(event) => handleColumnChange(participant.participantId, draft, event)}
                      >
                        <MenuItem value="Left">{locale.columns.Left}</MenuItem>
                        <MenuItem value="Right">{locale.columns.Right}</MenuItem>
                      </Select>
                    </FormControl>
                  </Stack>
                ) : (
                  <Typography variant="body2" sx={{ color: homePanelColors.label }}>
                    {`${locale.positionRow}: ${locale.rows[participant.position.row]} / ${locale.positionColumn}: ${locale.columns[participant.position.column]}`}
                  </Typography>
                )}
              </Stack>
              {participant.job?.displayName ? (
                // ジョブは右端に大きめのアイコンと名前で出す
                <Stack
                  alignItems="center"
                  spacing={0.5}
                  sx={{ width: { xs: 52, sm: 88 }, flexShrink: 0, alignSelf: 'center' }}
                >
                  {jobImageSrc ? (
                    <Box
                      component="img"
                      src={jobImageSrc}
                      alt=""
                      sx={{
                        width: { xs: 40, sm: 64 },
                        height: { xs: 40, sm: 64 },
                        objectFit: 'contain',
                        display: 'block',
                      }}
                    />
                  ) : null}
                  <Typography
                    sx={{
                      maxWidth: '100%',
                      whiteSpace: 'nowrap',
                      overflow: 'hidden',
                      textOverflow: 'ellipsis',
                      color: homePanelColors.text,
                      fontSize: '0.875rem',
                      fontWeight: 700,
                    }}
                  >
                    {participant.job.displayName}
                  </Typography>
                </Stack>
              ) : null}
            </Stack>
          </Box>
        )
      })}
      {isOwner ? (
        <Stack direction="row" spacing={1.5} sx={{ pt: 0.5 }}>
          <Button
            variant="contained"
            startIcon={<PlayArrowIcon sx={{ fontSize: 20 }} />}
            onClick={() => void onStartQuest()}
            disabled={isStartDisabled || isSavingFormation}
            sx={{ ...menuButtonSx, ...panelPrimaryButtonSx, flex: 1, minHeight: 52 }}
          >
            {isStarting ? locale.startingQuest : locale.startQuest}
          </Button>
          <Button
            variant="outlined"
            startIcon={<CancelIcon sx={{ fontSize: 20 }} />}
            onClick={() => void onCancelRoom()}
            disabled={isStarting || isCancellingRoom || !isRecruiting}
            sx={{ ...menuButtonSx, ...panelCancelButtonSx, flex: 1, minHeight: 52 }}
          >
            {isCancellingRoom ? locale.cancellingRoom : locale.cancelRoom}
          </Button>
        </Stack>
      ) : null}
    </Stack>
  )

  return <>{renderLayout({ info, editor, editorTitle })}</>
}
