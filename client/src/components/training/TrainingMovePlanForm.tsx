import { Alert, Box, Button, FormControl, MenuItem, Paper, Select, Stack, Typography } from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { resolvePublicAssetPath } from '@/lib/assets'
import { innerSurfaceSx } from '@/constants/styles'
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
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: 2.5,
        width: '100%',
        minWidth: 0,
        boxSizing: 'border-box',
        color: '#f5f0df',
        backgroundColor: '#261819',
        borderColor: 'rgba(214, 146, 112, 0.34)',
      }}
    >
      <Stack spacing={2} sx={{ width: '100%', minWidth: 0 }}>
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
                backgroundColor: '#1d1112',
                border: '1px solid rgba(214, 146, 112, 0.22)',
                p: 1.1,
                boxSizing: 'border-box',
              }}
            />
            <Typography variant="subtitle1" fontWeight={800} textAlign="center" sx={{ color: '#fff7dd' }}>
              {enemy.name}
            </Typography>
            <Typography variant="body2" sx={{ color: 'rgba(245, 240, 223, 0.72)' }}>
              {locale.plannedMpSummary
                .replace('{{currentMp}}', String(player.status.maxMp))
                .replace('{{totalMp}}', String(totalMp))}
            </Typography>
          </Stack>
        ) : (
          <Typography variant="body2" sx={{ color: 'rgba(245, 240, 223, 0.72)' }}>
            {locale.plannedMpSummary
              .replace('{{currentMp}}', String(player.status.maxMp))
              .replace('{{totalMp}}', String(totalMp))}
          </Typography>
        )}

        {plannedTurns.map((turn, index) => (
          <Stack key={`training-turn-${index}`} spacing={0.75} sx={{ width: '100%', minWidth: 0 }}>
            <Typography variant="subtitle2" fontWeight={800} sx={{ color: '#fff0c8' }}>
              {locale.turnLabel.replace('{{turn}}', String(index + 1))}
            </Typography>
            <FormControl fullWidth sx={{ minWidth: 0, maxWidth: '100%' }}>
              <Select
                size="small"
                value={turn.moveId === null ? 'normal-attack' : String(turn.moveId)}
                renderValue={(value) => (
                  <Stack
                    component="span"
                    spacing={0.2}
                    sx={{
                      display: 'block',
                      minWidth: 0,
                      maxWidth: '100%',
                      overflow: 'hidden',
                      py: 0.25,
                    }}
                  >
                    {turn.moveId === null ? (
                      <Typography
                        component="span"
                        sx={{
                          display: 'block',
                          overflow: 'hidden',
                          textOverflow: 'ellipsis',
                          whiteSpace: 'nowrap',
                          color: '#fff7dd',
                        }}
                      >
                        {locale.normalAttack}
                      </Typography>
                    ) : (
                      <>
                        <Typography
                          component="span"
                          sx={{
                            display: 'block',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            lineHeight: 1.25,
                            color: '#fff7dd',
                          }}
                        >
                          {turn.moveName}
                        </Typography>
                        <Typography
                          component="span"
                          variant="caption"
                          sx={{
                            color: 'rgba(245, 240, 223, 0.66)',
                            display: 'block',
                            overflow: 'hidden',
                            textOverflow: 'ellipsis',
                            whiteSpace: 'nowrap',
                            lineHeight: 1.2,
                          }}
                        >
                          {locale.mpCost.replace('{{cost}}', String(turn.mpCost))} /{' '}
                          {resolveTargetTypeLabel(
                            availableMoves.find((move) => String(move.moveId) === String(value))?.targetType ?? 'Enemy',
                          )}
                        </Typography>
                      </>
                    )}
                  </Stack>
                )}
                inputProps={{
                  'aria-label': `${locale.turnLabel.replace('{{turn}}', String(index + 1))} ${locale.moveLabel}`,
                }}
                onChange={(event: SelectChangeEvent<string>) =>
                  onChangeMoveId(index, event.target.value === 'normal-attack' ? null : Number(event.target.value))
                }
                sx={{
                  width: '100%',
                  minWidth: 0,
                  maxWidth: '100%',
                  boxSizing: 'border-box',
                  color: '#fff7dd',
                  backgroundColor: '#382526',
                  '& .MuiOutlinedInput-notchedOutline': {
                    borderColor: 'rgba(214, 146, 112, 0.34)',
                  },
                  '&:hover .MuiOutlinedInput-notchedOutline': {
                    borderColor: 'rgba(214, 146, 112, 0.58)',
                  },
                  '&.Mui-focused .MuiOutlinedInput-notchedOutline': {
                    borderColor: '#c96a52',
                  },
                  '& .MuiSvgIcon-root': {
                    color: 'rgba(245, 240, 223, 0.82)',
                  },
                  '& .MuiSelect-select': {
                    minWidth: 0,
                    maxWidth: '100%',
                    overflow: 'hidden',
                    whiteSpace: 'normal',
                    pr: 4,
                    py: 1.1,
                  },
                }}
              >
                <MenuItem value="normal-attack" sx={{ color: '#1f2326' }}>
                  {locale.normalAttack}
                </MenuItem>
                {availableMoves.map((move) => (
                  <MenuItem
                    key={move.moveId}
                    value={String(move.moveId)}
                    sx={{
                      color: '#1f2326',
                      whiteSpace: 'normal',
                      overflowWrap: 'anywhere',
                    }}
                  >
                      <Stack spacing={0.2} sx={{ minWidth: 0 }}>
                        <Typography sx={{ overflowWrap: 'anywhere', lineHeight: 1.25 }}>{move.moveName}</Typography>
                      <Typography variant="caption" color="text.secondary">
                        {locale.mpCost.replace('{{cost}}', String(move.mpCost ?? 0))} /{' '}
                        {resolveTargetTypeLabel(move.targetType)}
                      </Typography>
                    </Stack>
                  </MenuItem>
                ))}
              </Select>
            </FormControl>
            {turn.warning ? (
              <Alert
                severity="warning"
                sx={{
                  color: '#38240f',
                  backgroundColor: '#f0d49d',
                  '& .MuiAlert-icon': {
                    color: '#8c5311',
                  },
                }}
              >
                {turn.warning}
              </Alert>
            ) : null}
          </Stack>
        ))}

        {showSubmitButton ? (
          <Button
            variant="contained"
            disabled={isActionDisabled}
            onClick={onSubmit}
            sx={{
              borderRadius: 999,
              py: 1.1,
              fontWeight: 800,
              color: '#fff5ef',
              backgroundColor: '#b65f49',
              boxShadow: 'none',
              '&:hover': {
                backgroundColor: '#c96a52',
                boxShadow: 'none',
              },
              '&.Mui-disabled': {
                color: 'rgba(255, 238, 229, 0.58)',
                backgroundColor: 'rgba(182, 95, 73, 0.24)',
              },
            }}
          >
            {isActionDisabled ? rematchInSeconds : (submitLabel ?? locale.startTraining)}
          </Button>
        ) : null}
      </Stack>
    </Paper>
  )
}
