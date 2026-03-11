import { Alert, Box, Button, MenuItem, Paper, Select, Stack, Typography } from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { resolvePublicAssetPath } from '@/lib/assets'
import { innerSurfaceSx, softGreenButtonSx } from '@/constants/styles'
import type { GetPlayerResponse, PlayerMoveSlot } from '@/schema/player'
import type { TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

type TrainingMovePlanFormProps = {
  enemy: TrainingEnemy
  player: GetPlayerResponse
  moveIds: Array<number | null>
  isActionDisabled: boolean
  lockRemainingSeconds: number
  onChangeMoveId: (turnIndex: number, moveId: number | null) => void
  showEnemyHeader?: boolean
  submitLabel?: string
  showSubmitButton?: boolean
  onSubmit: () => Promise<void> | void
}

type PlannedTurn = {
  moveId: number | null
  moveName: string
  mpCost: number
  warning: string | null
}

function getAvailableMoves(player: GetPlayerResponse): PlayerMoveSlot[] {
  return player.moveSlots.filter((slot) => slot.moveId !== null)
}

function resolveTargetTypeLabel(targetType: PlayerMoveSlot['targetType']): string {
  switch (targetType) {
    case 'Enemy':
      return locale.targetTypeEnemy
    case 'Ally':
      return locale.targetTypeAlly
    case 'Self':
      return locale.targetTypeSelf
    default:
      return locale.targetTypeEnemy
  }
}

function buildPlannedTurns(player: GetPlayerResponse, moveIds: Array<number | null>): PlannedTurn[] {
  const moveById = new Map(getAvailableMoves(player).map((slot) => [slot.moveId!, slot]))
  let remainingMp = player.status.maxMp

  return moveIds.map((moveId) => {
    if (moveId === null) {
      return {
        moveId: null,
        moveName: locale.normalAttack,
        mpCost: 0,
        warning: null,
      }
    }

    const move = moveById.get(moveId)
    const mpCost = move?.mpCost ?? 0
    const canUse = remainingMp >= mpCost

    if (canUse) {
      remainingMp -= mpCost
    }

    return {
      moveId,
      moveName: move?.moveName ?? String(moveId),
      mpCost,
      warning: canUse ? null : locale.waitWarning,
    }
  })
}

export default function TrainingMovePlanForm({
  enemy,
  player,
  moveIds,
  isActionDisabled,
  lockRemainingSeconds,
  onChangeMoveId,
  showEnemyHeader = true,
  submitLabel,
  showSubmitButton = true,
  onSubmit,
}: TrainingMovePlanFormProps) {
  const availableMoves = getAvailableMoves(player)
  const plannedTurns = buildPlannedTurns(player, moveIds)
  const totalMp = plannedTurns.reduce((sum, turn) => sum + turn.mpCost, 0)
  const rematchInSeconds = locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: 2.5 }}>
      <Stack spacing={2}>
        {showEnemyHeader ? (
          <Stack spacing={1.25}>
            <Box
              component="img"
              src={resolvePublicAssetPath(enemy.imagePath)}
              alt={enemy.name}
              sx={{
                width: '100%',
                height: 180,
                objectFit: 'contain',
                borderRadius: 2,
                backgroundColor: 'action.hover',
                p: 1,
                boxSizing: 'border-box',
              }}
            />
            <Typography variant="subtitle1" fontWeight={700} textAlign="center">
              {enemy.name}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {locale.plannedMpSummary
                .replace('{{currentMp}}', String(player.status.maxMp))
                .replace('{{totalMp}}', String(totalMp))}
            </Typography>
          </Stack>
        ) : (
          <Typography variant="body2" color="text.secondary">
            {locale.plannedMpSummary
              .replace('{{currentMp}}', String(player.status.maxMp))
              .replace('{{totalMp}}', String(totalMp))}
          </Typography>
        )}

        {plannedTurns.map((turn, index) => (
          <Stack key={`training-turn-${index}`} spacing={0.75}>
            <Typography variant="subtitle2" fontWeight={700}>
              {locale.turnLabel.replace('{{turn}}', String(index + 1))}
            </Typography>
            <Select
              size="small"
              value={turn.moveId === null ? 'normal-attack' : String(turn.moveId)}
              inputProps={{
                'aria-label': `${locale.turnLabel.replace('{{turn}}', String(index + 1))} ${locale.moveLabel}`,
              }}
              onChange={(event: SelectChangeEvent<string>) =>
                onChangeMoveId(index, event.target.value === 'normal-attack' ? null : Number(event.target.value))
              }
            >
              <MenuItem value="normal-attack">{locale.normalAttack}</MenuItem>
              {availableMoves.map((move) => (
                <MenuItem key={move.moveId} value={String(move.moveId)}>
                  {move.moveName} / {locale.mpCost.replace('{{cost}}', String(move.mpCost ?? 0))} /{' '}
                  {resolveTargetTypeLabel(move.targetType)}
                </MenuItem>
              ))}
            </Select>
            {turn.warning ? <Alert severity="warning">{turn.warning}</Alert> : null}
          </Stack>
        ))}

        {showSubmitButton ? (
          <Button variant="contained" disabled={isActionDisabled} onClick={onSubmit} sx={softGreenButtonSx}>
            {isActionDisabled ? rematchInSeconds : (submitLabel ?? locale.startTraining)}
          </Button>
        ) : null}
      </Stack>
    </Paper>
  )
}
