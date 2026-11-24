import { Alert, Box, Button, FormControl, MenuItem, Select, Stack, Typography } from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { resolvePublicAssetPath } from '@/lib/assets'
import { panelPrimaryButtonSx, trainingPanelColors } from '@/constants/styles'
import type { GetPlayerResponse, PlayerMoveSlot } from '@/schema/player'
import type { TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'
import { getTrainingBackgroundPath } from './trainingSelectStyles'

type TrainingMovePlanFormProps = {
  // まだ相手を選んでいないときは null（技の欄と「戦闘」を押せない状態で出す）
  enemy: TrainingEnemy | null
  // 相手の背景を決めるキー（NPC は敵の id、PLAYER はプレイヤーの id）
  enemyBackgroundKey?: string | number
  player: GetPlayerResponse
  moveIds: Array<number | null>
  isActionDisabled: boolean
  lockRemainingSeconds: number
  onChangeMoveId: (turnIndex: number, moveId: number | null) => void
  submitButtonId?: string
  // チュートリアルの矢印で指す技の選択欄（何ターン目か と 付ける id）
  tutorialMoveSelect?: { index: number; id: string }
  onSubmit: () => Promise<void> | void
}

type PlannedTurn = {
  moveId: number | null
  moveName: string
  mpCost: number
  warning: string | null
}

const dottedDivider = `1px dotted ${trainingPanelColors.divider}`

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

// 消費MP の合計。上限を超えたら赤
function MpSummary({ plannedMp, maxMp }: { plannedMp: number; maxMp: number }) {
  return (
    <Typography
      variant="body2"
      fontWeight={700}
      sx={{ color: plannedMp > maxMp ? '#c62828' : trainingPanelColors.label, whiteSpace: 'nowrap' }}
    >
      {locale.plannedMpSummary.replace('{{plannedMp}}', String(plannedMp)).replace('{{maxMp}}', String(maxMp))}
    </Typography>
  )
}

function MoveSlotValue({ moveName, mpCost }: { moveName: string; mpCost: number }) {
  return (
    <Stack component="span" direction="row" spacing={1} alignItems="baseline" sx={{ minWidth: 0 }}>
      <Typography
        component="span"
        sx={{ color: trainingPanelColors.text, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}
      >
        {moveName}
      </Typography>
      {mpCost > 0 ? (
        <Typography component="span" variant="caption" sx={{ color: trainingPanelColors.label, flex: 'none' }}>
          {locale.mpCost.replace('{{cost}}', String(mpCost))}
        </Typography>
      ) : null}
    </Stack>
  )
}

function formatSlotDetail(mpCost: number, targetLabel: string): string {
  return locale.moveSlotDetail.replace('{{cost}}', String(mpCost)).replace('{{target}}', targetLabel)
}

function BattleButton({
  id,
  disabled,
  lockRemainingSeconds,
  onClick,
}: {
  id?: string
  disabled: boolean
  lockRemainingSeconds: number
  onClick: () => void
}) {
  return (
    <Button
      id={id}
      variant="contained"
      fullWidth
      disabled={disabled}
      onClick={onClick}
      sx={{
        ...panelPrimaryButtonSx,
        flex: 'none',
        height: 48,
        fontSize: '1.05rem',
        '&&': {
          ...panelPrimaryButtonSx['&&'],
          backgroundColor: trainingPanelColors.band,
          borderColor: trainingPanelColors.band,
        },
        '&&:hover': {
          ...panelPrimaryButtonSx['&&:hover'],
          backgroundColor: trainingPanelColors.band,
        },
        '&&.Mui-disabled': {
          backgroundColor: trainingPanelColors.disabledButton,
          borderColor: trainingPanelColors.disabledButton,
          color: '#ffffff',
        },
      }}
    >
      {lockRemainingSeconds > 0
        ? locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))
        : locale.startBattle}
    </Button>
  )
}

export default function TrainingMovePlanForm({
  enemy,
  enemyBackgroundKey,
  player,
  moveIds,
  isActionDisabled,
  lockRemainingSeconds,
  onChangeMoveId,
  submitButtonId,
  tutorialMoveSelect,
  onSubmit,
}: TrainingMovePlanFormProps) {
  const availableMoves = getAvailableMoves(player)
  const plannedTurns = buildPlannedTurns(player, moveIds)
  const totalMp = plannedTurns.reduce((sum, turn) => sum + turn.mpCost, 0)
  const maxMp = player.status.maxMp
  const hasEnemy = enemy !== null
  const enemyLevelLabel = enemy ? locale.enemyLevel.replace('{{level}}', String(enemy.level)) : null
  const enemyImageSrc = enemy ? resolvePublicAssetPath(enemy.imagePath) : null

  const battleButton = (
    <BattleButton
      id={submitButtonId}
      disabled={isActionDisabled || !hasEnemy}
      lockRemainingSeconds={lockRemainingSeconds}
      onClick={() => void onSubmit()}
    />
  )

  const turnSlots = (
    <Stack spacing={1.25}>
      {plannedTurns.map((turn, index) => {
        const turnLabel = locale.turnLabel.replace('{{turn}}', String(index + 1))
        return (
          <Box key={`training-turn-${index}`}>
            <Stack direction="row" spacing={1.5} alignItems="center">
              <Typography
                variant="body2"
                fontWeight={700}
                sx={{ color: trainingPanelColors.label, width: 52, flex: 'none', opacity: hasEnemy ? 1 : 0.5 }}
              >
                {turnLabel}
              </Typography>
              <FormControl
                id={tutorialMoveSelect?.index === index ? tutorialMoveSelect.id : undefined}
                fullWidth
                disabled={!hasEnemy}
                sx={{ minWidth: 0, maxWidth: '100%', opacity: hasEnemy ? 1 : 0.5 }}
              >
                <Select
                  value={turn.moveId === null ? 'normal-attack' : String(turn.moveId)}
                  renderValue={() => <MoveSlotValue moveName={turn.moveName} mpCost={turn.mpCost} />}
                  inputProps={{ 'aria-label': `${turnLabel} ${locale.moveLabel}` }}
                  onChange={(event: SelectChangeEvent<string>) =>
                    onChangeMoveId(index, event.target.value === 'normal-attack' ? null : Number(event.target.value))
                  }
                  sx={{
                    borderRadius: '8px',
                    backgroundColor: '#ffffff',
                    '& .MuiOutlinedInput-notchedOutline': { borderColor: '#d9dee7' },
                    '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: '#b3bccb' },
                    '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: trainingPanelColors.band },
                    '& .MuiSelect-select': { py: 1.1, pl: 1.5, display: 'flex', alignItems: 'center' },
                    '& .MuiSvgIcon-root': { color: trainingPanelColors.label },
                  }}
                >
                  <MenuItem value="normal-attack" sx={{ color: trainingPanelColors.text }}>
                    {locale.normalAttack}
                  </MenuItem>
                  {availableMoves.map((move) => (
                    <MenuItem key={move.moveId} value={String(move.moveId)} sx={{ color: trainingPanelColors.text }}>
                      <Stack spacing={0.2} sx={{ minWidth: 0 }}>
                        <Typography sx={{ overflowWrap: 'anywhere', lineHeight: 1.25 }}>{move.moveName}</Typography>
                        <Typography variant="caption" color="text.secondary">
                          {formatSlotDetail(move.mpCost ?? 0, resolveTargetTypeLabel(move.targetType))}
                        </Typography>
                      </Stack>
                    </MenuItem>
                  ))}
                </Select>
              </FormControl>
            </Stack>
            {turn.warning ? (
              <Alert severity="error" sx={{ mt: 0.75, py: 0.25, fontWeight: 700 }}>
                {turn.warning}
              </Alert>
            ) : null}
          </Box>
        )
      })}
    </Stack>
  )

  return (
    <Stack spacing={1.75} sx={{ height: '100%' }}>
      <Stack
        direction="row"
        spacing={1.5}
        alignItems="center"
        sx={{ pb: { xs: 1, sm: 1.5 }, borderBottom: dottedDivider }}
      >
        {/* スマホは上の戦闘画面に相手が出ているので、対戦相手の行は出さず消費MP だけにする */}
        <Stack
          direction="row"
          spacing={1.5}
          alignItems="center"
          sx={{ flex: 1, minWidth: 0, display: { xs: 'none', sm: 'flex' } }}
        >
          <Box
            sx={{
              width: 52,
              height: 52,
              flex: 'none',
              borderRadius: '10px',
              display: 'grid',
              placeItems: 'center',
              boxSizing: 'border-box',
              border: enemy ? 'none' : '2px dashed #c6cdd8',
              backgroundColor: enemy ? '#e3e7ee' : 'transparent',
              backgroundImage:
                enemy && enemyBackgroundKey !== undefined
                  ? `url(${getTrainingBackgroundPath(enemyBackgroundKey)})`
                  : 'none',
              backgroundSize: 'cover',
              backgroundPosition: 'center',
            }}
          >
            {enemyImageSrc ? (
              <Box component="img" src={enemyImageSrc} alt="" sx={{ maxWidth: 46, maxHeight: 46 }} />
            ) : (
              <Typography aria-hidden="true" sx={{ fontSize: '1.4rem', fontWeight: 900, color: '#8f98a8' }}>
                ?
              </Typography>
            )}
          </Box>
          <Stack sx={{ minWidth: 0 }}>
            <Typography variant="caption" sx={{ color: trainingPanelColors.label }}>
              {locale.nextOpponent}
            </Typography>
            <Typography fontWeight={800} sx={{ color: trainingPanelColors.text, overflowWrap: 'anywhere' }}>
              {enemy ? `${enemy.name} ` : null}
              {enemyLevelLabel ? (
                <Typography component="span" variant="body2" fontWeight={700} sx={{ color: trainingPanelColors.label }}>
                  {enemyLevelLabel}
                </Typography>
              ) : null}
            </Typography>
          </Stack>
        </Stack>
        <Box sx={{ ml: 'auto', flex: 'none' }}>
          <MpSummary plannedMp={totalMp} maxMp={maxMp} />
        </Box>
      </Stack>
      {turnSlots}
      <Box sx={{ flex: 1 }} />
      {battleButton}
    </Stack>
  )
}
