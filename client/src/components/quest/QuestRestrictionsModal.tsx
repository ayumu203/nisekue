import { useEffect, useState } from 'react'
import {
  Alert,
  Avatar,
  Button,
  Checkbox,
  CircularProgress,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  FormControlLabel,
  Stack,
  TextField,
  Typography,
} from '@mui/material'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/quest/QuestRoom.json'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'

type QuestRestrictionsModalProps = {
  open: boolean
  title?: string
  minRequiredLevelInput: string
  currentPlayerLevel: number | null
  allowedPlayerIds: string[]
  selectablePlayers: PlayerSummary[]
  isLoading: boolean
  error: Error | null
  disabled?: boolean
  onClose: () => void
  onApply: (nextMinRequiredLevelInput: string, nextAllowedPlayerIds: string[]) => void
}

export default function QuestRestrictionsModal({
  open,
  title,
  minRequiredLevelInput,
  currentPlayerLevel,
  allowedPlayerIds,
  selectablePlayers,
  isLoading,
  error,
  disabled = false,
  onClose,
  onApply,
}: QuestRestrictionsModalProps) {
  const [draftMinRequiredLevelInput, setDraftMinRequiredLevelInput] = useState(minRequiredLevelInput)
  const [draftAllowedPlayerIds, setDraftAllowedPlayerIds] = useState<string[]>(allowedPlayerIds)

  const parsedMinRequiredLevel = Number(draftMinRequiredLevelInput)
  const isMinRequiredLevelTooHigh =
    draftMinRequiredLevelInput.trim() !== '' &&
    Number.isFinite(parsedMinRequiredLevel) &&
    currentPlayerLevel != null &&
    parsedMinRequiredLevel > currentPlayerLevel

  useEffect(() => {
    if (!open) {
      return
    }

    setDraftMinRequiredLevelInput(minRequiredLevelInput)
    setDraftAllowedPlayerIds(allowedPlayerIds)
  }, [open, minRequiredLevelInput, allowedPlayerIds])

  const handleToggleAllowedPlayer = (playerId: string) => {
    setDraftAllowedPlayerIds((current) =>
      current.includes(playerId) ? current.filter((id) => id !== playerId) : [...current, playerId],
    )
  }

  const handleApply = () => {
    if (isMinRequiredLevelTooHigh) {
      return
    }

    onApply(draftMinRequiredLevelInput, draftAllowedPlayerIds)
    onClose()
  }

  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{title ?? locale.restrictionsModalTitle}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={1.5}>
          <TextField
            label={locale.labels.minRequiredLevel}
            type="number"
            value={draftMinRequiredLevelInput}
            onChange={(event) => setDraftMinRequiredLevelInput(event.target.value)}
            inputProps={{
              min: 1,
              ...(currentPlayerLevel != null ? { max: currentPlayerLevel } : {}),
            }}
            placeholder={locale.noRestriction}
            fullWidth
            disabled={disabled}
            error={isMinRequiredLevelTooHigh}
            helperText={
              isMinRequiredLevelTooHigh && currentPlayerLevel != null
                ? locale.minRequiredLevelTooHigh.replace('{{level}}', String(currentPlayerLevel))
                : undefined
            }
          />

          <Typography variant="subtitle2" sx={{ fontWeight: 800 }}>
            {locale.labels.allowedPlayers}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {locale.allowedPlayersHint}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {draftAllowedPlayerIds.length === 0
              ? locale.noRestriction
              : locale.selectedAllowedPlayersCount.replace('{{count}}', String(draftAllowedPlayerIds.length))}
          </Typography>

          {isLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={18} />
              <Typography variant="body2">{locale.playerCandidatesLoading}</Typography>
            </Stack>
          ) : error ? (
            <Alert severity="warning">{error.message}</Alert>
          ) : selectablePlayers.length === 0 ? (
            <Typography variant="body2" color="text.secondary">
              {locale.noSelectablePlayers}
            </Typography>
          ) : (
            <Stack spacing={1}>
              {selectablePlayers.map((candidate) => {
                const displayName = candidate.userName ?? locale.unknownPlayer

                return (
                  <FormControlLabel
                    key={candidate.userId}
                    control={
                      <Checkbox
                        checked={draftAllowedPlayerIds.includes(candidate.userId)}
                        onChange={() => handleToggleAllowedPlayer(candidate.userId)}
                        disabled={disabled}
                      />
                    }
                    label={
                      <Stack direction="row" spacing={1.25} alignItems="center">
                        <Avatar src={resolveCharacterAssetPath(candidate.imagePath) ?? undefined} alt={displayName}>
                          {displayName.slice(0, 1)}
                        </Avatar>
                        <Stack spacing={0.25}>
                          <Typography variant="body2" sx={{ fontWeight: 700 }}>
                            {displayName}
                          </Typography>
                          <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
                            <Typography variant="caption" color="text.secondary">
                              {`${locale.levelLabel} ${candidate.level}`}
                            </Typography>
                            <Stack direction="row" spacing={0.5} alignItems="center">
                              <img
                                src={resolveJobAssetPath(candidate.job.code) ?? undefined}
                                alt={candidate.job.displayName}
                                width={18}
                                height={18}
                              />
                              <Typography variant="caption" color="text.secondary">
                                {candidate.job.displayName}
                              </Typography>
                            </Stack>
                          </Stack>
                        </Stack>
                      </Stack>
                    }
                    sx={{ m: 0, alignItems: 'flex-start' }}
                  />
                )
              })}
            </Stack>
          )}
        </Stack>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{locale.closeOverlay}</Button>
        <Button onClick={handleApply} variant="contained" disabled={disabled || isMinRequiredLevelTooHigh}>
          {locale.applyRestrictions}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
