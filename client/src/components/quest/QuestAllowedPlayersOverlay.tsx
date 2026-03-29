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
  Typography,
} from '@mui/material'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/quest/QuestRoom.json'
import { resolveCharacterAssetPath, resolveJobAssetPath } from '@/lib/assets'

type QuestAllowedPlayersOverlayProps = {
  open: boolean
  selectablePlayers: PlayerSummary[]
  allowedPlayerIds: string[]
  isLoading: boolean
  error: Error | null
  disabled?: boolean
  onClose: () => void
  onToggleAllowedPlayer: (playerId: string) => void
}

export default function QuestAllowedPlayersOverlay({
  open,
  selectablePlayers,
  allowedPlayerIds,
  isLoading,
  error,
  disabled = false,
  onClose,
  onToggleAllowedPlayer,
}: QuestAllowedPlayersOverlayProps) {
  return (
    <Dialog open={open} onClose={onClose} fullWidth maxWidth="sm">
      <DialogTitle>{locale.allowedPlayersOverlayTitle}</DialogTitle>
      <DialogContent dividers>
        <Stack spacing={1.5}>
          <Typography variant="body2" color="text.secondary">
            {locale.allowedPlayersHint}
          </Typography>
          <Typography variant="body2" color="text.secondary">
            {locale.selectedAllowedPlayersCount.replace('{{count}}', String(allowedPlayerIds.length))}
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
                        checked={allowedPlayerIds.includes(candidate.userId)}
                        onChange={() => onToggleAllowedPlayer(candidate.userId)}
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
      </DialogActions>
    </Dialog>
  )
}
