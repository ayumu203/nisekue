import { Alert, Box, Button, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useEffect, useRef, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import useSWR from 'swr'
import { getPets } from '@/api/pet'
import { getPlayer } from '@/api/player'
import {
  abortPetBattle,
  assignPetBattleSlot,
  getPetBattleRun,
  getPetBattleStats,
  getPetBattleStatus,
  matchPetBattle,
  removePetBattleSlot,
  startPetBattle,
  submitPetBattleCommand,
} from '@/api/petBattle'
import PetBattleLobbySection from '@/components/petbattle/PetBattleLobbySection'
import PetBattleResultPanel from '@/components/petbattle/PetBattleResultPanel'
import PetBattleRunSection from '@/components/petbattle/PetBattleRunSection'
import { cyberColors, cyberDangerButtonSx, cyberPanelSx } from '@/components/petbattle/petBattleStyles'
import {
  getReachableOpponents,
  getTargetCandidates,
  needsTargetSelection,
  sortMembersByPosition,
  type PetBattleCommandDraft,
} from '@/components/petbattle/petBattleTargets'
import { useAuth } from '@/contexts/useAuth'
import locale from '../../locale/pet/PetBattle.json'
import type { BattleColumn, BattleRow, PetBattleRoom, PetBattleRun } from '@/schema/petBattle'

export default function PetBattle() {
  const { session, isLoading } = useAuth()
  const navigate = useNavigate()
  const [matchedRoom, setMatchedRoom] = useState<PetBattleRoom | null>(null)
  const [startedRun, setStartedRun] = useState<PetBattleRun | null>(null)
  const [hasRecovered, setHasRecovered] = useState(false)
  const [isMatching, setIsMatching] = useState(false)
  const [isAssigning, setIsAssigning] = useState(false)
  const [isStarting, setIsStarting] = useState(false)
  const [isSubmitting, setIsSubmitting] = useState(false)
  const [isAborting, setIsAborting] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [drafts, setDrafts] = useState<Record<string, PetBattleCommandDraft>>({})
  const hasAttemptedRecoveryRef = useRef(false)
  const lastDraftKeyRef = useRef<string | null>(null)
  const startRatingRef = useRef<number | null>(null)

  useEffect(() => {
    hasAttemptedRecoveryRef.current = false
    setHasRecovered(false)
    setMatchedRoom(null)
    setStartedRun(null)
  }, [session?.user.id])

  useEffect(() => {
    if (!session?.access_token || hasAttemptedRecoveryRef.current) {
      return
    }

    hasAttemptedRecoveryRef.current = true

    const recover = async () => {
      try {
        const status = await getPetBattleStatus(session.access_token)
        if (status.run != null) {
          setStartedRun(status.run)
        } else if (status.room != null && status.room.status === 'WaitingForStart') {
          setMatchedRoom(status.room)
        }
      } catch (recoveryError) {
        setError(recoveryError instanceof Error ? recoveryError.message : locale.loadFailed)
      } finally {
        setHasRecovered(true)
      }
    }

    void recover()
  }, [session?.access_token])

  const statsSWRKey = session?.user.id ? (['pet-battle-stats', session.user.id] as const) : null
  const { data: stats, mutate: mutateStats } = useSWR(statsSWRKey, async () => {
    if (!session?.access_token) {
      return null
    }

    return getPetBattleStats(session.access_token)
  })

  const playerSWRKey = session?.user.id ? (['pet-battle-player', session.user.id] as const) : null
  const { data: player } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      return null
    }

    return getPlayer(session.access_token)
  })

  const runSWRKey = session?.access_token && startedRun?.runId ? (['pet-battle-run', startedRun.runId] as const) : null
  const {
    data: liveRun,
    error: runError,
    mutate: mutateRun,
  } = useSWR(
    runSWRKey,
    async () => {
      if (!session?.access_token || !startedRun?.runId) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getPetBattleRun(startedRun.runId, session.access_token)
    },
    {
      refreshInterval: (run) => (run?.status === 'InProgress' ? 2000 : 0),
    },
  )

  const currentRun = liveRun ?? startedRun
  const currentRoom = currentRun == null ? matchedRoom : null

  const petsSWRKey =
    session?.user.id && currentRoom != null && currentRun == null
      ? (['pet-battle-pets', session.user.id] as const)
      : null
  const { data: petsResponse } = useSWR(petsSWRKey, async () => {
    if (!session?.access_token) {
      return null
    }

    return getPets(session.access_token)
  })

  // ターン更新時は待機中ペットのドラフトを引き継ぎつつ、無効なターゲットだけ補正する
  useEffect(() => {
    if (currentRun == null || currentRun.status !== 'InProgress') {
      return
    }

    const draftKey = `${currentRun.runId}:${currentRun.currentTurnNo}`
    if (lastDraftKeyRef.current === draftKey) {
      return
    }

    lastDraftKeyRef.current = draftKey

    setDrafts((currentDrafts) => {
      const nextDrafts: Record<string, PetBattleCommandDraft> = {}

      for (const member of currentRun.ownerMembers) {
        if (!currentRun.waitingParticipantIds.includes(member.participantId)) {
          continue
        }

        const firstTarget = getReachableOpponents(member, currentRun.opponentMembers)[0] ?? null
        const baseDraft: PetBattleCommandDraft = currentDrafts[member.participantId] ?? {
          actionKind: 'NormalAttack',
          moveId: '',
          targetRow: firstTarget?.startRow ?? '',
          targetColumn: firstTarget?.startColumn ?? '',
        }

        const selectedMove =
          baseDraft.moveId === '' ? null : (member.moves.find((move) => move.moveId === baseDraft.moveId) ?? null)
        const sanitizedDraft: PetBattleCommandDraft =
          baseDraft.actionKind === 'UseMove' && (selectedMove == null || selectedMove.mpCost > member.currentMp)
            ? {
                ...baseDraft,
                moveId: '',
                targetRow: '',
                targetColumn: '',
              }
            : baseDraft
        const sanitizedMove = sanitizedDraft.moveId === '' ? null : selectedMove

        if (!needsTargetSelection(sanitizedDraft, sanitizedMove)) {
          nextDrafts[member.participantId] = sanitizedDraft
          continue
        }

        const candidates = getTargetCandidates(
          member,
          sanitizedDraft,
          currentRun.ownerMembers,
          currentRun.opponentMembers,
        )
        const hasCurrentTarget = candidates.some(
          (candidate) =>
            candidate.startRow === sanitizedDraft.targetRow && candidate.startColumn === sanitizedDraft.targetColumn,
        )

        nextDrafts[member.participantId] = hasCurrentTarget
          ? sanitizedDraft
          : {
              ...sanitizedDraft,
              targetRow: candidates[0]?.startRow ?? '',
              targetColumn: candidates[0]?.startColumn ?? '',
            }
      }

      return nextDrafts
    })
  }, [currentRun])

  const isRunFinished = currentRun != null && currentRun.status !== 'InProgress'
  useEffect(() => {
    if (isRunFinished) {
      void mutateStats()
    }
  }, [isRunFinished, mutateStats])

  useEffect(() => {
    if (currentRun?.status === 'InProgress' && stats != null && startRatingRef.current == null) {
      startRatingRef.current = stats.rating
    }

    if (currentRun == null) {
      startRatingRef.current = null
    }
  }, [currentRun, stats])

  function handleDraftChange(participantId: string, draft: PetBattleCommandDraft): void {
    let nextDraft = draft
    const member = currentRun?.ownerMembers.find((m) => m.participantId === participantId) ?? null

    if (member != null && currentRun != null) {
      const move = draft.moveId === '' ? null : (member.moves.find((m) => m.moveId === draft.moveId) ?? null)

      if (draft.actionKind === 'UseMove' && move?.targetType === 'Self') {
        nextDraft = { ...draft, targetRow: member.startRow, targetColumn: member.startColumn }
      } else if (needsTargetSelection(draft, move) && (draft.targetRow === '' || draft.targetColumn === '')) {
        const firstCandidate = getTargetCandidates(
          member,
          draft,
          currentRun.ownerMembers,
          currentRun.opponentMembers,
        )[0]
        if (firstCandidate != null) {
          nextDraft = { ...draft, targetRow: firstCandidate.startRow, targetColumn: firstCandidate.startColumn }
        }
      }
    }

    setDrafts((current) => ({ ...current, [participantId]: nextDraft }))
  }

  async function handleMatch(): Promise<void> {
    if (!session?.access_token) {
      setError(locale.sessionInfoMissing)
      return
    }

    setIsMatching(true)
    setError(null)

    try {
      const room = await matchPetBattle(session.access_token)
      setMatchedRoom(room)
    } catch (matchError) {
      setError(matchError instanceof Error ? matchError.message : locale.matchFailed)
    } finally {
      setIsMatching(false)
    }
  }

  async function handleAssignSlot(petId: string, row: BattleRow, column: BattleColumn): Promise<void> {
    if (!session?.access_token || currentRoom == null) {
      return
    }

    setIsAssigning(true)
    setError(null)

    try {
      const room = await assignPetBattleSlot(currentRoom.roomId, { petId, row, column }, session.access_token)
      setMatchedRoom(room)
    } catch (assignError) {
      setError(assignError instanceof Error ? assignError.message : locale.assignFailed)
    } finally {
      setIsAssigning(false)
    }
  }

  async function handleRemoveSlot(petId: string): Promise<void> {
    if (!session?.access_token || currentRoom == null) {
      return
    }

    setIsAssigning(true)
    setError(null)

    try {
      const room = await removePetBattleSlot(currentRoom.roomId, petId, session.access_token)
      setMatchedRoom(room)
    } catch (removeError) {
      setError(removeError instanceof Error ? removeError.message : locale.assignFailed)
    } finally {
      setIsAssigning(false)
    }
  }

  async function handleStartBattle(): Promise<void> {
    if (!session?.access_token || currentRoom == null) {
      return
    }

    setIsStarting(true)
    setError(null)

    try {
      const run = await startPetBattle(currentRoom.roomId, session.access_token)
      setStartedRun(run)
      setMatchedRoom(null)
    } catch (startError) {
      setError(startError instanceof Error ? startError.message : locale.startFailed)
    } finally {
      setIsStarting(false)
    }
  }

  async function handleConfirmCommands(): Promise<void> {
    if (!session?.access_token || currentRun == null) {
      return
    }

    const waitingMembers = sortMembersByPosition(currentRun.ownerMembers).filter((member) =>
      currentRun.waitingParticipantIds.includes(member.participantId),
    )

    for (const member of waitingMembers) {
      const draft = drafts[member.participantId]
      if (draft == null) {
        setError(locale.commandIncomplete)
        return
      }

      const move = draft.moveId === '' ? null : (member.moves.find((m) => m.moveId === draft.moveId) ?? null)
      if (draft.actionKind === 'UseMove' && move == null) {
        setError(locale.commandIncomplete)
        return
      }

      if (draft.actionKind === 'UseMove' && move != null && move.mpCost > member.currentMp) {
        setError(locale.commandIncomplete)
        return
      }

      if (needsTargetSelection(draft, move) && (draft.targetRow === '' || draft.targetColumn === '')) {
        setError(locale.commandIncomplete)
        return
      }
    }

    setIsSubmitting(true)
    setError(null)

    try {
      let latestRun = currentRun
      for (const member of waitingMembers) {
        if (latestRun.submittedParticipantIds.includes(member.participantId)) {
          continue
        }

        const draft = drafts[member.participantId]!
        const response = await submitPetBattleCommand(
          currentRun.runId,
          {
            participantId: member.participantId,
            turnNo: currentRun.currentTurnNo,
            actionKind: draft.actionKind,
            moveId: draft.actionKind === 'UseMove' && draft.moveId !== '' ? draft.moveId : null,
            targetRow: draft.targetRow === '' ? null : draft.targetRow,
            targetColumn: draft.targetColumn === '' ? null : draft.targetColumn,
          },
          session.access_token,
        )
        latestRun = response.run
      }

      setStartedRun(latestRun)
      await mutateRun(latestRun, { revalidate: false })
    } catch (submitError) {
      setError(submitError instanceof Error ? submitError.message : locale.commandFailed)
      await mutateRun()
    } finally {
      setIsSubmitting(false)
    }
  }

  async function handleAbort(): Promise<void> {
    if (!session?.access_token || currentRun == null) {
      return
    }

    if (!window.confirm(locale.surrenderConfirm)) {
      return
    }

    setIsAborting(true)
    setError(null)

    try {
      const run = await abortPetBattle(currentRun.runId, session.access_token)
      setStartedRun(run)
      await mutateRun(run, { revalidate: false })
      await mutateStats()
    } catch (abortError) {
      setError(abortError instanceof Error ? abortError.message : locale.surrenderFailed)
    } finally {
      setIsAborting(false)
    }
  }

  const showLoading = isLoading || (session != null && !hasRecovered)
  const showEntry = !showLoading && currentRoom == null && currentRun == null
  const showLobby = !showLoading && currentRoom != null && currentRun == null
  const showRun = !showLoading && currentRun != null && currentRun.status === 'InProgress'
  const showResult = !showLoading && currentRun != null && currentRun.status !== 'InProgress'

  useEffect(() => {
    if (!showEntry || isMatching || error != null || !session?.access_token) {
      return
    }

    void handleMatch()
  }, [showEntry, isMatching, error, session?.access_token])

  return (
    <Box sx={{ minHeight: '100vh', backgroundColor: 'var(--app-bg-color)' }}>
      <Container maxWidth="md" sx={{ px: { xs: 1, sm: 3 }, py: { xs: 1.5, sm: 4 } }}>
        <Box
          sx={{
            backgroundColor: cyberColors.bg,
            borderRadius: 3,
            border: '1px solid rgba(255, 255, 255, 0.08)',
            px: { xs: 1, sm: 2 },
            py: { xs: 1.5, sm: 2 },
          }}
        >
          <Stack spacing={2}>
            <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.5, sm: 2 } }}>
              <Stack spacing={0}>
                <Stack direction="row" spacing={1} alignItems="flex-start" justifyContent="space-between">
                  <Stack spacing={0.25}>
                    <Typography
                      variant="overline"
                      sx={{ color: cyberColors.accentDim, letterSpacing: '0.22em', lineHeight: 1.2 }}
                    >
                      {locale.subtitle}
                    </Typography>
                    <Typography
                      variant="h4"
                      sx={{
                        color: cyberColors.accent,
                        fontWeight: 900,
                        letterSpacing: '0.06em',
                        textShadow: `0 0 14px ${cyberColors.accentDim}`,
                        fontSize: { xs: '1.65rem', sm: '2rem' },
                      }}
                    >
                      {locale.title}
                    </Typography>
                  </Stack>
                  {showRun ? (
                    <Button
                      onClick={() => void handleAbort()}
                      disabled={isAborting || isSubmitting}
                      sx={cyberDangerButtonSx}
                    >
                      {locale.surrender}
                    </Button>
                  ) : null}
                </Stack>
              </Stack>
            </Paper>

            {error ? (
              <Alert severity="error" onClose={() => setError(null)}>
                {error}
              </Alert>
            ) : null}
            {runError instanceof Error ? <Alert severity="warning">{runError.message}</Alert> : null}

            {showLoading ? (
              <Stack direction="row" spacing={1} alignItems="center" justifyContent="center" sx={{ py: 6 }}>
                <CircularProgress size={20} sx={{ color: cyberColors.accent }} />
                <Typography sx={{ color: cyberColors.text }}>
                  {isLoading ? locale.authLoading : locale.statusLoading}
                </Typography>
              </Stack>
            ) : null}

            {showEntry ? (
              <Stack direction="row" spacing={1} alignItems="center" justifyContent="center" sx={{ py: 6 }}>
                <CircularProgress size={20} sx={{ color: cyberColors.accent }} />
                <Typography sx={{ color: cyberColors.text }}>{locale.finding}</Typography>
              </Stack>
            ) : null}

            {showLobby && currentRoom != null ? (
              <PetBattleLobbySection
                room={currentRoom}
                pets={petsResponse?.pets ?? []}
                isAssigning={isAssigning}
                isStarting={isStarting}
                onAssignSlot={handleAssignSlot}
                onRemoveSlot={handleRemoveSlot}
                onStartBattle={handleStartBattle}
              />
            ) : null}

            {showRun && currentRun != null ? (
              <PetBattleRunSection
                run={currentRun}
                drafts={drafts}
                isSubmitting={isSubmitting}
                onDraftChange={handleDraftChange}
                onConfirmCommands={handleConfirmCommands}
              />
            ) : null}

            {showResult && currentRun != null ? (
              <PetBattleResultPanel
                run={currentRun}
                ownerImagePath={player?.imagePath ?? null}
                ownerName={player?.userName ?? null}
                stats={stats ?? null}
                ratingChange={
                  stats != null && startRatingRef.current != null ? stats.rating - startRatingRef.current : null
                }
                onBackToPets={() => navigate('/pets')}
              />
            ) : null}
          </Stack>
        </Box>
      </Container>
    </Box>
  )
}
