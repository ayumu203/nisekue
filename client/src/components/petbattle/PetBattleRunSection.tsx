import {
  Box,
  Button,
  Chip,
  MenuItem,
  Paper,
  Select,
  Stack,
  ToggleButton,
  ToggleButtonGroup,
  Typography,
} from '@mui/material'
import { useEffect, useState } from 'react'
import PetBattleMemberCard from '@/components/petbattle/PetBattleMemberCard'
import PetBattleTurnLog from '@/components/petbattle/PetBattleTurnLog'
import {
  cyberButtonSx,
  cyberColors,
  cyberDangerButtonSx,
  cyberPanelSx,
  cyberSelectSx,
} from '@/components/petbattle/petBattleStyles'
import {
  getTargetCandidates,
  needsTargetSelection,
  sortMembersByPosition,
  type PetBattleCommandDraft,
} from '@/components/petbattle/petBattleTargets'
import locale from '../../../locale/pet/PetBattle.json'
import type { BattleColumn, BattleRow, PetBattleActionKind, PetBattleMember, PetBattleRun } from '@/schema/petBattle'

type PetBattleRunSectionProps = {
  run: PetBattleRun
  drafts: Record<string, PetBattleCommandDraft>
  isSubmitting: boolean
  isAborting: boolean
  onDraftChange: (participantId: string, draft: PetBattleCommandDraft) => void
  onConfirmCommands: () => void | Promise<void>
  onAbort: () => void | Promise<void>
}

const ownerRowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const opponentRowOrder: BattleRow[] = ['Back', 'Middle', 'Front']
const columnOrder: BattleColumn[] = ['Left', 'Right']

function FieldGrid({
  members,
  rowOrderForSide,
  targetedKeys,
  submittedIds,
}: {
  members: PetBattleMember[]
  rowOrderForSide: BattleRow[]
  targetedKeys: Set<string>
  submittedIds: Set<string>
}) {
  const memberByPosition = new Map(members.map((member) => [`${member.startRow}:${member.startColumn}`, member]))

  return (
    <Stack spacing={0.75}>
      {rowOrderForSide.map((row) => (
        <Stack key={row} direction="row" spacing={0.75} alignItems="stretch">
          <Box
            sx={{
              width: 36,
              display: 'grid',
              placeItems: 'center',
              borderRadius: 1.5,
              border: `1px solid ${cyberColors.panelBorder}`,
            }}
          >
            <Typography variant="caption" sx={{ color: cyberColors.accentDim, fontSize: '0.6rem', fontWeight: 700 }}>
              {locale.rows[row]}
            </Typography>
          </Box>

          {columnOrder.map((column) => {
            const member = memberByPosition.get(`${row}:${column}`)

            return (
              <Box key={column} sx={{ flex: 1, minWidth: 0 }}>
                {member != null ? (
                  <PetBattleMemberCard
                    member={member}
                    isTargeted={targetedKeys.has(`${row}:${column}`)}
                    isSubmitted={submittedIds.has(member.participantId)}
                  />
                ) : (
                  <Box
                    sx={{
                      height: '100%',
                      minHeight: 56,
                      borderRadius: 2,
                      border: `1px dashed ${cyberColors.accentFaint}`,
                    }}
                  />
                )}
              </Box>
            )
          })}
        </Stack>
      ))}
    </Stack>
  )
}

export default function PetBattleRunSection({
  run,
  drafts,
  isSubmitting,
  isAborting,
  onDraftChange,
  onConfirmCommands,
  onAbort,
}: PetBattleRunSectionProps) {
  const [nowMs, setNowMs] = useState(() => Date.now())

  useEffect(() => {
    const timer = window.setInterval(() => setNowMs(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [])

  const remainingSeconds = Math.max(0, Math.floor((new Date(run.actionDeadlineAt).getTime() - nowMs) / 1000))
  const submittedIds = new Set(run.submittedParticipantIds)
  const waitingIds = new Set(run.waitingParticipantIds)
  const sortedOwnerMembers = sortMembersByPosition(run.ownerMembers)

  const targetedOpponentKeys = new Set<string>()
  const targetedOwnerKeys = new Set<string>()
  for (const member of sortedOwnerMembers) {
    if (!waitingIds.has(member.participantId)) {
      continue
    }

    const draft = drafts[member.participantId]
    if (draft == null || draft.targetRow === '' || draft.targetColumn === '') {
      continue
    }

    const key = `${draft.targetRow}:${draft.targetColumn}`
    const move = draft.moveId === '' ? null : (member.moves.find((m) => m.moveId === draft.moveId) ?? null)
    if (draft.actionKind === 'NormalAttack' || (draft.actionKind === 'UseMove' && move?.targetType === 'Enemy')) {
      targetedOpponentKeys.add(key)
    } else if (draft.actionKind === 'UseMove' && move != null) {
      targetedOwnerKeys.add(key)
    }
  }

  const hasWaitingPets = run.waitingParticipantIds.length > 0

  return (
    <Stack spacing={2}>
      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.25, sm: 1.5 } }}>
        <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
          <Chip
            label={`${locale.turnLabel} ${run.currentTurnNo}`}
            sx={{ color: cyberColors.accent, backgroundColor: cyberColors.accentFaint, fontWeight: 800 }}
          />
          <Chip
            label={
              remainingSeconds > 0
                ? `${locale.deadlineLabel} ${Math.floor(remainingSeconds / 60)}:${String(remainingSeconds % 60).padStart(2, '0')}`
                : locale.deadlineExpired
            }
            sx={{
              color: remainingSeconds <= 10 ? cyberColors.danger : cyberColors.warn,
              backgroundColor: remainingSeconds <= 10 ? 'rgba(255, 56, 96, 0.12)' : 'rgba(255, 209, 102, 0.12)',
              fontWeight: 700,
            }}
          />
          <Box sx={{ flex: 1 }} />
          <Button onClick={() => void onAbort()} disabled={isAborting || isSubmitting} sx={cyberDangerButtonSx}>
            {locale.surrender}
          </Button>
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.25, sm: 2 } }}>
        <Stack spacing={1.25}>
          <Stack direction="row" spacing={1} alignItems="center">
            <Typography variant="caption" sx={{ color: cyberColors.danger, fontWeight: 800, letterSpacing: '0.12em' }}>
              {locale.opponentSideLabel}
            </Typography>
            <Typography variant="caption" sx={{ color: cyberColors.textDim }}>
              {run.opponentPlayerName ?? locale.unknownPlayer}
            </Typography>
          </Stack>

          <FieldGrid
            members={run.opponentMembers}
            rowOrderForSide={opponentRowOrder}
            targetedKeys={targetedOpponentKeys}
            submittedIds={new Set()}
          />

          <Box
            sx={{
              height: 2,
              borderRadius: 999,
              background: `linear-gradient(90deg, transparent 0%, ${cyberColors.accent} 50%, transparent 100%)`,
              opacity: 0.6,
              my: 0.5,
            }}
          />

          <Typography variant="caption" sx={{ color: cyberColors.accent, fontWeight: 800, letterSpacing: '0.12em' }}>
            {locale.mySideLabel}
          </Typography>

          <FieldGrid
            members={run.ownerMembers}
            rowOrderForSide={ownerRowOrder}
            targetedKeys={targetedOwnerKeys}
            submittedIds={submittedIds}
          />
        </Stack>
      </Paper>

      <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.25, sm: 2 } }}>
        <Stack spacing={1.5}>
          <Typography variant="subtitle2" sx={{ color: cyberColors.accent, fontWeight: 800 }}>
            {locale.commandTitle}
          </Typography>

          {sortedOwnerMembers.map((member) => {
            const isSubmitted = submittedIds.has(member.participantId)
            const isWaiting = waitingIds.has(member.participantId)
            const draft = drafts[member.participantId]

            return (
              <Paper
                key={member.participantId}
                variant="outlined"
                sx={{
                  p: { xs: 1, sm: 1.25 },
                  borderRadius: 2,
                  backgroundColor: cyberColors.panelLight,
                  borderColor: cyberColors.panelBorder,
                  opacity: member.isDead ? 0.5 : 1,
                }}
              >
                <Stack spacing={0.75}>
                  <Stack direction="row" spacing={1} alignItems="center" justifyContent="space-between">
                    <Typography variant="body2" noWrap sx={{ color: cyberColors.text, fontWeight: 800 }}>
                      {member.displayName}
                    </Typography>
                    {member.isDead ? (
                      <Chip
                        size="small"
                        label={locale.deadChip}
                        sx={{ color: cyberColors.danger, backgroundColor: 'rgba(255, 56, 96, 0.14)' }}
                      />
                    ) : isSubmitted ? (
                      <Chip
                        size="small"
                        label={locale.submittedChip}
                        sx={{ color: cyberColors.accent, backgroundColor: cyberColors.accentFaint }}
                      />
                    ) : !isWaiting ? (
                      <Chip
                        size="small"
                        label={locale.cannotActChip}
                        sx={{ color: cyberColors.warn, backgroundColor: 'rgba(255, 209, 102, 0.12)' }}
                      />
                    ) : null}
                  </Stack>

                  {isWaiting && !isSubmitted && draft != null ? (
                    <Stack spacing={0.75}>
                      <ToggleButtonGroup
                        exclusive
                        size="small"
                        value={draft.actionKind}
                        onChange={(_, nextValue: PetBattleActionKind | null) => {
                          if (nextValue == null) {
                            return
                          }

                          onDraftChange(member.participantId, {
                            actionKind: nextValue,
                            moveId: '',
                            targetRow: '',
                            targetColumn: '',
                          })
                        }}
                        sx={{
                          flexWrap: 'wrap',
                          gap: 0.5,
                          '& .MuiToggleButton-root': {
                            color: cyberColors.textDim,
                            border: `1px solid ${cyberColors.panelBorder}`,
                            borderRadius: '8px !important',
                            px: 1.25,
                            py: 0.4,
                            fontSize: '0.74rem',
                            fontWeight: 700,
                          },
                          '& .MuiToggleButton-root.Mui-selected': {
                            color: '#0a0d0c',
                            backgroundColor: cyberColors.accent,
                            '&:hover': { backgroundColor: cyberColors.accent },
                          },
                        }}
                      >
                        <ToggleButton value="NormalAttack">{locale.actionKinds.NormalAttack}</ToggleButton>
                        {member.moves.length > 0 ? (
                          <ToggleButton value="UseMove">{locale.actionKinds.UseMove}</ToggleButton>
                        ) : null}
                        <ToggleButton value="Guard">{locale.actionKinds.Guard}</ToggleButton>
                        <ToggleButton value="Wait">{locale.actionKinds.Wait}</ToggleButton>
                      </ToggleButtonGroup>

                      {draft.actionKind === 'UseMove' ? (
                        <Select
                          displayEmpty
                          size="small"
                          value={draft.moveId === '' ? '' : String(draft.moveId)}
                          onChange={(event) => {
                            const nextValue = String(event.target.value)
                            onDraftChange(member.participantId, {
                              ...draft,
                              moveId: nextValue === '' ? '' : Number(nextValue),
                              targetRow: '',
                              targetColumn: '',
                            })
                          }}
                          sx={cyberSelectSx}
                        >
                          <MenuItem value="">{locale.movePlaceholder}</MenuItem>
                          {member.moves.map((move) => (
                            <MenuItem
                              key={move.moveId}
                              value={String(move.moveId)}
                              disabled={move.mpCost > member.currentMp}
                            >
                              {move.name}（{move.mpCost}
                              {locale.mpCostSuffix}）
                            </MenuItem>
                          ))}
                        </Select>
                      ) : null}

                      {(() => {
                        const move =
                          draft.moveId === '' ? null : (member.moves.find((m) => m.moveId === draft.moveId) ?? null)
                        if (!needsTargetSelection(draft, move)) {
                          return null
                        }

                        const candidates = getTargetCandidates(member, draft, run.ownerMembers, run.opponentMembers)
                        const selectedValue =
                          draft.targetRow !== '' && draft.targetColumn !== ''
                            ? `${draft.targetRow}:${draft.targetColumn}`
                            : ''

                        return (
                          <Stack direction="row" spacing={1} alignItems="center">
                            <Typography variant="caption" sx={{ color: cyberColors.textDim }}>
                              {locale.targetLabel}
                            </Typography>
                            <Select
                              displayEmpty
                              size="small"
                              value={selectedValue}
                              onChange={(event) => {
                                const nextValue = String(event.target.value)
                                if (nextValue === '') {
                                  onDraftChange(member.participantId, { ...draft, targetRow: '', targetColumn: '' })
                                  return
                                }

                                const [row, column] = nextValue.split(':') as [BattleRow, BattleColumn]
                                onDraftChange(member.participantId, { ...draft, targetRow: row, targetColumn: column })
                              }}
                              sx={{ ...cyberSelectSx, flex: 1, minWidth: 0 }}
                            >
                              {candidates.length === 0 ? (
                                <MenuItem value="">{locale.noReachableTarget}</MenuItem>
                              ) : (
                                candidates.map((candidate) => (
                                  <MenuItem
                                    key={candidate.participantId}
                                    value={`${candidate.startRow}:${candidate.startColumn}`}
                                  >
                                    {candidate.displayName}（{locale.rows[candidate.startRow]}・
                                    {locale.columns[candidate.startColumn]}）
                                  </MenuItem>
                                ))
                              )}
                            </Select>
                          </Stack>
                        )
                      })()}
                    </Stack>
                  ) : null}
                </Stack>
              </Paper>
            )
          })}

          {hasWaitingPets ? (
            <Button onClick={() => void onConfirmCommands()} disabled={isSubmitting} sx={cyberButtonSx}>
              {isSubmitting ? locale.submittingCommands : locale.confirmCommands}
            </Button>
          ) : (
            <Typography variant="body2" sx={{ color: cyberColors.textDim, textAlign: 'center' }}>
              {locale.waitingResolution}
            </Typography>
          )}
        </Stack>
      </Paper>

      {run.lastTurnResults != null ? <PetBattleTurnLog results={run.lastTurnResults} /> : null}
    </Stack>
  )
}
