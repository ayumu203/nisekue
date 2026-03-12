import { Chip, LinearProgress, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx, playerHpBarSx } from '@/constants/styles'
import type { QuestRunDetailResponse } from '@/schema/quest'

type QuestBattleStatusPanelProps = {
  run: QuestRunDetailResponse
  selfParticipantId: string | null
  locale: {
    battleStatusTitle: string
    battleStatusSubtitle: string
    runStatusLabel: string
    runStatus: Record<'InProgress' | 'Succeeded' | 'Failed' | 'Aborted', string>
    waitingParticipantsLabel: string
    partyMembersLabel: string
    enemiesLabel: string
    labels: {
      submitted: string
      pending: string
      manual: string
      autoAttackOnly: string
    }
    positionRow: string
    positionColumn: string
    rows: Record<'Front' | 'Middle' | 'Back', string>
    columns: Record<'Left' | 'Right', string>
    currentHpLabel: string
    currentMpLabel: string
    maxHpLabel: string
    maxMpLabel: string
    waitingForInputLabel: string
    yes: string
    no: string
    deadLabel: string
    actionModeLabel: string
    activeEffectsLabel: string
    noActiveEffects: string
    pendingCommandTitle: string
    pendingCommandEmpty: string
    actionKinds: Record<'NormalAttack' | 'UseMove' | 'Guard' | 'Wait' | 'LeaveQuest' | 'Escape', string>
  }
}

function getHpRate(currentHp: number, maxHp: number | null | undefined): number {
  if (!maxHp || maxHp <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (currentHp / maxHp) * 100))
}

export default function QuestBattleStatusPanel({ run, selfParticipantId, locale }: QuestBattleStatusPanelProps) {
  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
      <Stack spacing={2}>
        <div>
          <Typography variant="h5">{locale.battleStatusTitle}</Typography>
          <Typography variant="body2" color="text.secondary">
            {locale.battleStatusSubtitle}
          </Typography>
        </div>

        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          <Chip color="primary" label={`${locale.runStatusLabel}: ${locale.runStatus[run.status]}`} />
          <Chip label={`${locale.waitingParticipantsLabel}: ${run.turn.waitingParticipantIds.length}`} />
          <Chip label={`${locale.partyMembersLabel}: ${run.partyMembers.length}`} />
          <Chip label={`${locale.enemiesLabel}: ${run.enemies.length}`} />
        </Stack>

        <Stack spacing={1.5}>
          <Typography variant="subtitle1">{locale.partyMembersLabel}</Typography>
          {run.partyMembers.map((member) => {
            const isWaiting = run.turn.waitingParticipantIds.includes(member.participantId)
            const pendingCommand = run.pendingCommands.find(
              (command) => command.participantId === member.participantId && command.turnNo === run.turn.currentTurnNo,
            )

            return (
              <Paper
                key={member.participantId}
                variant="outlined"
                sx={{
                  borderRadius: 2,
                  p: 1.5,
                  backgroundColor: member.participantId === selfParticipantId ? '#fffaf0' : '#fffdf8',
                }}
              >
                <Stack spacing={1}>
                  <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
                    <Typography variant="subtitle2">{member.displayName}</Typography>
                    {member.participantId === selfParticipantId ? <Chip size="small" label="You" /> : null}
                    <Chip size="small" color={isWaiting ? 'warning' : 'success'} label={isWaiting ? locale.labels.pending : locale.labels.submitted} />
                    <Chip
                      size="small"
                      variant="outlined"
                      label={member.actionMode === 'Manual' ? locale.labels.manual : locale.labels.autoAttackOnly}
                    />
                    {member.isDead ? <Chip size="small" color="error" label={locale.deadLabel} /> : null}
                  </Stack>

                  <Typography variant="body2" color="text.secondary">
                    {`${locale.positionRow}: ${locale.rows[member.position.row]} / ${locale.positionColumn}: ${locale.columns[member.position.column]}`}
                  </Typography>

                  <Stack spacing={0.75}>
                    <Typography variant="body2">{`${locale.currentHpLabel}: ${member.currentHp} / ${member.maxHp ?? '-'}`}</Typography>
                    <LinearProgress variant="determinate" value={getHpRate(member.currentHp, member.maxHp)} sx={playerHpBarSx} />
                    <Typography variant="body2" color="text.secondary">{`${locale.currentMpLabel}: ${member.currentMp} / ${member.maxMp ?? '-'}`}</Typography>
                  </Stack>

                  <Typography variant="body2" color="text.secondary">
                    {`${locale.waitingForInputLabel}: ${isWaiting ? locale.yes : locale.no}`}
                  </Typography>

                  <Typography variant="body2" color="text.secondary">
                    {`${locale.activeEffectsLabel}: ${member.activeEffects.length > 0 ? member.activeEffects.map((effect) => effect.displayName).join(', ') : locale.noActiveEffects}`}
                  </Typography>

                  <Typography variant="body2" color="text.secondary">
                    {`${locale.pendingCommandTitle}: ${pendingCommand ? locale.actionKinds[pendingCommand.actionKind] : locale.pendingCommandEmpty}`}
                  </Typography>
                </Stack>
              </Paper>
            )
          })}
        </Stack>

        <Stack spacing={1.5}>
          <Typography variant="subtitle1">{locale.enemiesLabel}</Typography>
          {run.enemies.map((enemy) => (
            <Paper
              key={enemy.enemyInstanceId}
              variant="outlined"
              sx={{
                borderRadius: 2,
                p: 1.5,
                backgroundColor: '#fffdf8',
              }}
            >
              <Stack spacing={1}>
                <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
                  <Typography variant="subtitle2">{enemy.name}</Typography>
                  {enemy.isDead ? <Chip size="small" color="error" label={locale.deadLabel} /> : null}
                </Stack>

                <Typography variant="body2" color="text.secondary">
                  {`${locale.positionRow}: ${locale.rows[enemy.position.row]} / ${locale.positionColumn}: ${locale.columns[enemy.position.column]}`}
                </Typography>

                <Stack spacing={0.75}>
                  <Typography variant="body2">{`${locale.currentHpLabel}: ${enemy.currentHp} / ${enemy.maxHp ?? '-'}`}</Typography>
                  <LinearProgress variant="determinate" value={getHpRate(enemy.currentHp, enemy.maxHp)} sx={playerHpBarSx} />
                  <Typography variant="body2" color="text.secondary">{`${locale.currentMpLabel}: ${enemy.currentMp} / ${enemy.maxMp ?? '-'}`}</Typography>
                </Stack>

                <Typography variant="body2" color="text.secondary">
                  {`${locale.activeEffectsLabel}: ${enemy.activeEffects.length > 0 ? enemy.activeEffects.map((effect) => effect.displayName).join(', ') : locale.noActiveEffects}`}
                </Typography>
              </Stack>
            </Paper>
          ))}
        </Stack>
      </Stack>
    </Paper>
  )
}
