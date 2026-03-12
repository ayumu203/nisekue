import { useMemo, useState } from 'react'
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
import { innerSurfaceSx } from '@/constants/styles'
import type { GetPlayerResponse } from '@/schema/player'
import type { QuestRoomDetailResponse, QuestRunDetailResponse } from '@/schema/quest'
import locale from '../../../locale/quest/Quest.json'

type QuestRunPanelProps = {
  run: QuestRunDetailResponse | undefined
  room: QuestRoomDetailResponse | undefined
  player: GetPlayerResponse | undefined
  isLoading: boolean
  error: string | null
  isConnected: boolean
  connectionError: string | null
  currentUserId: string | null
  onSubmitCommand: (
    input: {
      participantId: string
      turnNo: number
      actionKind: 'NormalAttack' | 'UseMove' | 'Guard' | 'Wait' | 'LeaveQuest' | 'Escape'
      moveId?: number | null
      targetRow?: 'Front' | 'Middle' | 'Back' | null
      targetColumn?: 'Left' | 'Right' | null
    },
  ) => Promise<void>
  onRequestManualControl: (participantId: string) => Promise<void>
  onApproveManualControl: (participantId: string) => Promise<void>
  onSendChat: (participantId: string, message: string) => Promise<void>
}

const actionKinds = ['NormalAttack', 'UseMove', 'Guard', 'Wait', 'LeaveQuest', 'Escape'] as const
const rowOptions = ['Front', 'Middle', 'Back'] as const
const columnOptions = ['Left', 'Right'] as const

function formatRunStatus(status: 'InProgress' | 'Succeeded' | 'Failed' | 'Aborted') {
  return locale.runStatus[status]
}

export default function QuestRunPanel({
  run,
  room,
  player,
  isLoading,
  error,
  isConnected,
  connectionError,
  currentUserId,
  onSubmitCommand,
  onRequestManualControl,
  onApproveManualControl,
  onSendChat,
}: QuestRunPanelProps) {
  const [actionKind, setActionKind] = useState<(typeof actionKinds)[number]>('NormalAttack')
  const [moveId, setMoveId] = useState<number | ''>('')
  const [targetRow, setTargetRow] = useState<(typeof rowOptions)[number] | ''>('')
  const [targetColumn, setTargetColumn] = useState<(typeof columnOptions)[number] | ''>('')
  const [chatMessage, setChatMessage] = useState('')

  const currentParticipantId = useMemo(
    () => room?.participants.find((participant) => participant.playerId === currentUserId)?.participantId ?? null,
    [currentUserId, room],
  )

  const currentPartyMember = useMemo(
    () => run?.partyMembers.find((member) => member.participantId === currentParticipantId) ?? null,
    [currentParticipantId, run],
  )

  const availableMoves = player?.moveSlots.filter((slot) => slot.moveId !== null) ?? []
  const pendingManualApprovals = run?.partyMembers.filter((member) => member.manualControlRequestStatus === 'Pending') ?? []
  const isOwner = room?.ownerPlayerId === currentUserId

  const canSubmitCommand =
    !!run &&
    !!currentParticipantId &&
    !!currentPartyMember &&
    !currentPartyMember.isDead &&
    run.turn.waitingParticipantIds.includes(currentParticipantId)

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
      <Stack spacing={2}>
        <Typography variant="h5">{locale.questRunTitle}</Typography>
        {error ? <Alert severity="warning">{error}</Alert> : null}
        {connectionError ? (
          <Alert severity="warning">
            {locale.signalrError}: {connectionError}
          </Alert>
        ) : null}
        <Chip
          label={isConnected ? locale.signalrConnected : locale.signalrDisconnected}
          color={isConnected ? 'success' : 'default'}
          sx={{ alignSelf: 'flex-start' }}
        />
        {isLoading ? <Typography variant="body2">{locale.runLoading}</Typography> : null}
        {!isLoading && !run ? <Alert severity="info">{locale.questRunEmpty}</Alert> : null}
        {run ? (
          <>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
              <Chip label={`${locale.currentRunStatus}: ${formatRunStatus(run.status)}`} />
              <Chip label={`${locale.currentFloor}: ${run.floor.currentFloorNo}`} />
              <Chip label={run.floor.isBossFloor ? locale.bossFloor : locale.normalFloor} />
              <Chip label={`${locale.turnNo}: ${run.turn.currentTurnNo}`} />
            </Stack>
            <Typography variant="body2" color="text.secondary">
              {locale.deadline}: {new Date(run.turn.actionDeadlineAt).toLocaleString()}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {locale.waitingParticipants}: {run.turn.waitingParticipantIds.length}
            </Typography>

            <Stack spacing={1.25}>
              <Typography variant="h6">Party</Typography>
              {run.partyMembers.map((member) => (
                <Paper key={member.participantId} variant="outlined" sx={{ borderRadius: 2, p: 1.5 }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                    <Typography fontWeight={700}>{member.displayName}</Typography>
                    <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
                      <Chip size="small" label={`HP ${member.currentHp}/${member.maxHp ?? '-'}`} />
                      <Chip size="small" label={`MP ${member.currentMp}/${member.maxMp ?? '-'}`} />
                      <Chip
                        size="small"
                        label={member.actionMode === 'Manual' ? locale.labels.manual : locale.labels.autoAttackOnly}
                      />
                      <Chip
                        size="small"
                        label={member.manualControlRequestStatus === 'Pending' ? locale.labels.pending : locale.labels.none}
                      />
                    </Stack>
                  </Stack>
                </Paper>
              ))}
            </Stack>

            <Stack spacing={1.25}>
              <Typography variant="h6">Enemies</Typography>
              {run.enemies.map((enemy) => (
                <Paper key={enemy.enemyInstanceId} variant="outlined" sx={{ borderRadius: 2, p: 1.5 }}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center" flexWrap="wrap" gap={1}>
                    <Typography fontWeight={700}>{enemy.name}</Typography>
                    <Chip size="small" label={`HP ${enemy.currentHp}/${enemy.maxHp ?? '-'}`} />
                  </Stack>
                </Paper>
              ))}
            </Stack>

            <Stack spacing={1.25}>
              <Typography variant="h6">{locale.commandTitle}</Typography>
              <TextField
                select
                label={locale.labels.actionKind}
                value={actionKind}
                onChange={(event) => setActionKind(event.target.value as (typeof actionKinds)[number])}
              >
                {actionKinds.map((kind) => (
                  <MenuItem key={kind} value={kind}>
                    {locale.actionKinds[kind]}
                  </MenuItem>
                ))}
              </TextField>
              <TextField
                select
                label={locale.labels.move}
                value={moveId}
                onChange={(event) => setMoveId(event.target.value === '' ? '' : Number(event.target.value))}
                disabled={actionKind !== 'UseMove'}
              >
                <MenuItem value="">{locale.labels.none}</MenuItem>
                {availableMoves.map((slot) => (
                  <MenuItem key={slot.slot} value={slot.moveId ?? ''}>
                    {slot.moveName ?? `Move ${slot.moveId}`}
                  </MenuItem>
                ))}
              </TextField>
              <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
                <TextField
                  select
                  label={locale.labels.targetRow}
                  value={targetRow}
                  onChange={(event) => setTargetRow(event.target.value as (typeof rowOptions)[number] | '')}
                >
                  <MenuItem value="">{locale.labels.none}</MenuItem>
                  {rowOptions.map((row) => (
                    <MenuItem key={row} value={row}>
                      {locale.rows[row]}
                    </MenuItem>
                  ))}
                </TextField>
                <TextField
                  select
                  label={locale.labels.targetColumn}
                  value={targetColumn}
                  onChange={(event) => setTargetColumn(event.target.value as (typeof columnOptions)[number] | '')}
                >
                  <MenuItem value="">{locale.labels.none}</MenuItem>
                  {columnOptions.map((column) => (
                    <MenuItem key={column} value={column}>
                      {locale.columns[column]}
                    </MenuItem>
                  ))}
                </TextField>
              </Stack>
              <Button
                variant="contained"
                disabled={!canSubmitCommand}
                onClick={() =>
                  void onSubmitCommand({
                    participantId: currentParticipantId!,
                    turnNo: run.turn.currentTurnNo,
                    actionKind,
                    moveId: moveId === '' ? null : moveId,
                    targetRow: targetRow === '' ? null : targetRow,
                    targetColumn: targetColumn === '' ? null : targetColumn,
                  })
                }
              >
                {locale.submitCommand}
              </Button>
              <Button
                variant="outlined"
                color="error"
                disabled={!canSubmitCommand}
                onClick={() =>
                  void onSubmitCommand({
                    participantId: currentParticipantId!,
                    turnNo: run.turn.currentTurnNo,
                    actionKind: 'LeaveQuest',
                  })
                }
              >
                {locale.leaveQuest}
              </Button>
              {currentPartyMember?.actionMode === 'AutoAttackOnly' &&
              currentPartyMember.manualControlRequestStatus === 'None' &&
              currentParticipantId ? (
                <Button variant="outlined" onClick={() => void onRequestManualControl(currentParticipantId)}>
                  {locale.requestManualControl}
                </Button>
              ) : null}
              {isOwner && pendingManualApprovals.length ? (
                <Stack spacing={1}>
                  {pendingManualApprovals.map((member) => (
                    <Button
                      key={member.participantId}
                      variant="outlined"
                      onClick={() => void onApproveManualControl(member.participantId)}
                    >
                      {member.displayName}: {locale.approveManualControl}
                    </Button>
                  ))}
                </Stack>
              ) : null}
            </Stack>

            <Stack spacing={1.25}>
              <Typography variant="h6">{locale.chatTitle}</Typography>
              <TextField
                multiline
                minRows={2}
                value={chatMessage}
                placeholder={locale.chatPlaceholder}
                onChange={(event) => setChatMessage(event.target.value)}
              />
              <Button
                variant="outlined"
                disabled={!currentParticipantId || !chatMessage.trim()}
                onClick={async () => {
                  if (!currentParticipantId) {
                    return
                  }

                  await onSendChat(currentParticipantId, chatMessage.trim())
                  setChatMessage('')
                }}
              >
                {locale.sendChat}
              </Button>
              <Stack spacing={1}>
                {run.chatMessages.map((message, index) => (
                  <Paper key={`${message.sentAt}-${index}`} variant="outlined" sx={{ borderRadius: 2, p: 1.25 }}>
                    <Typography fontWeight={700}>{message.displayName}</Typography>
                    <Typography variant="body2" color="text.secondary">
                      {new Date(message.sentAt).toLocaleString()}
                    </Typography>
                    <Typography variant="body2">{message.message}</Typography>
                  </Paper>
                ))}
              </Stack>
            </Stack>

            {run.lastTurnResults ? (
              <Stack spacing={1.25}>
                <Typography variant="h6">{locale.lastTurnResults}</Typography>
                <Typography variant="body2" color="text.secondary">
                  {new Date(run.lastTurnResults.resolvedAt).toLocaleString()}
                </Typography>
                {run.lastTurnResults.actions.map((action, index) => (
                  <Paper key={`${action.actorDisplayName}-${index}`} variant="outlined" sx={{ borderRadius: 2, p: 1.25 }}>
                    <Stack spacing={0.75}>
                      <Typography fontWeight={700}>
                        {action.actorDisplayName} / {locale.actionKinds[action.actionKind]}
                      </Typography>
                      {action.moveName ? <Typography variant="body2">{action.moveName}</Typography> : null}
                      {action.logs.map((log, logIndex) => (
                        <Typography key={logIndex} variant="body2" color="text.secondary">
                          {log}
                        </Typography>
                      ))}
                    </Stack>
                  </Paper>
                ))}
              </Stack>
            ) : null}
          </>
        ) : null}
      </Stack>
    </Paper>
  )
}
