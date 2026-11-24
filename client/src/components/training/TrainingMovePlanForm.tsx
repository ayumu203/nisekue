import { Alert, Box, Button, FormControl, MenuItem, Select, Stack, Typography } from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { resolvePublicAssetPath } from '@/lib/assets'
import { homePanelColors, panelPrimaryButtonSx, trainingPanelColors } from '@/constants/styles'
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
  // 再戦までの待ち時間の進み具合（0〜1）。待っていないときは null
  lockProgress?: number | null
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
  targetLabel: string
  warning: string | null
}

const dottedDivider = `1px dotted ${homePanelColors.divider}`

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
        targetLabel: locale.targetTypeEnemy,
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
      targetLabel: resolveTargetTypeLabel(move?.targetType ?? 'Enemy'),
      warning: canUse ? null : locale.waitWarning,
    }
  })
}

function MpMeter({ plannedMp, maxMp }: { plannedMp: number; maxMp: number }) {
  const isOver = plannedMp > maxMp
  const ratio = maxMp > 0 ? Math.min(1, plannedMp / maxMp) : 0

  return (
    <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0 }}>
      <Typography variant="body2" fontWeight={800} sx={{ color: homePanelColors.label, whiteSpace: 'nowrap' }}>
        {locale.plannedMpLabel}
      </Typography>
      <Box
        aria-hidden="true"
        sx={{ flex: 1, minWidth: 60, height: 10, borderRadius: 9, backgroundColor: '#e9e2d6', overflow: 'hidden' }}
      >
        <Box
          sx={{
            width: `${ratio * 100}%`,
            height: '100%',
            backgroundColor: isOver ? '#c62828' : '#4a7fc1',
          }}
        />
      </Box>
      <Typography
        fontWeight={800}
        sx={{ color: isOver ? '#c62828' : homePanelColors.text, whiteSpace: 'nowrap', fontSize: '0.95rem' }}
      >
        {locale.plannedMpSummary.replace('{{plannedMp}}', String(plannedMp)).replace('{{maxMp}}', String(maxMp))}
      </Typography>
    </Stack>
  )
}

function MoveSlotValue({ turnNumber, moveName, detail }: { turnNumber: number; moveName: string; detail: string }) {
  return (
    <Stack component="span" direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
      <Stack component="span" alignItems="center" sx={{ width: 44, flex: 'none' }}>
        <Typography
          component="span"
          sx={{ fontSize: '0.7rem', fontWeight: 800, color: '#8a4a2c', letterSpacing: '0.04em', lineHeight: 1.2 }}
        >
          {locale.moveSlotTurn}
        </Typography>
        <Typography component="span" sx={{ fontSize: '1.25rem', fontWeight: 900, color: '#8a4a2c', lineHeight: 1 }}>
          {turnNumber}
        </Typography>
      </Stack>
      <Box component="span" sx={{ width: '1px', alignSelf: 'stretch', backgroundColor: '#eadfce', flex: 'none' }} />
      <Stack component="span" spacing={0.25} sx={{ minWidth: 0 }}>
        <Typography
          component="span"
          sx={{
            fontWeight: 800,
            color: homePanelColors.text,
            lineHeight: 1.25,
            whiteSpace: 'normal',
            overflowWrap: 'anywhere',
          }}
        >
          {moveName}
        </Typography>
        <Typography component="span" variant="caption" sx={{ color: homePanelColors.label, lineHeight: 1.3 }}>
          {detail}
        </Typography>
      </Stack>
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
  lockProgress,
  onClick,
}: {
  id?: string
  disabled: boolean
  lockRemainingSeconds: number
  lockProgress: number | null
  onClick: () => void
}) {
  const isLocked = lockRemainingSeconds > 0

  return (
    <Button
      id={id}
      variant="contained"
      fullWidth
      disabled={disabled}
      onClick={onClick}
      sx={{
        ...panelPrimaryButtonSx,
        position: 'relative',
        flex: 'none',
        height: 60,
        borderRadius: '12px',
        fontSize: '1.375rem',
        '&&': {
          ...panelPrimaryButtonSx['&&'],
          backgroundColor: trainingPanelColors.band,
          borderColor: trainingPanelColors.band,
          boxShadow: `0 4px 0 ${trainingPanelColors.bandSelected}`,
        },
        '&&:hover': {
          ...panelPrimaryButtonSx['&&:hover'],
          backgroundColor: trainingPanelColors.band,
          boxShadow: `0 4px 0 ${trainingPanelColors.bandSelected}`,
        },
        '&&.Mui-disabled': {
          backgroundColor: '#d9cfc0',
          borderColor: '#d9cfc0',
          color: '#6b5d47',
          boxShadow: 'none',
        },
      }}
    >
      {isLocked ? locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds)) : locale.startBattle}
      {isLocked && lockProgress !== null ? (
        <Box
          component="span"
          aria-hidden="true"
          sx={{
            position: 'absolute',
            left: 16,
            right: 16,
            bottom: 9,
            height: 4,
            borderRadius: 9,
            backgroundColor: 'rgba(255, 255, 255, 0.55)',
            overflow: 'hidden',
          }}
        >
          <Box
            component="span"
            sx={{
              display: 'block',
              width: `${Math.min(1, Math.max(0, lockProgress)) * 100}%`,
              height: '100%',
              backgroundColor: '#8a7c66',
              transition: 'width 250ms linear',
            }}
          />
        </Box>
      ) : null}
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
  lockProgress = null,
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
      lockProgress={lockProgress}
      onClick={() => void onSubmit()}
    />
  )

  const turnSlots = (
    <Stack spacing={1}>
      {plannedTurns.map((turn, index) => {
        const turnLabel = locale.turnLabel.replace('{{turn}}', String(index + 1))
        return (
          <Box key={`training-turn-${index}`}>
            <FormControl
              id={tutorialMoveSelect?.index === index ? tutorialMoveSelect.id : undefined}
              fullWidth
              disabled={!hasEnemy}
              sx={{ minWidth: 0, maxWidth: '100%', opacity: hasEnemy ? 1 : 0.5 }}
            >
              <Select
                value={turn.moveId === null ? 'normal-attack' : String(turn.moveId)}
                renderValue={() => (
                  <MoveSlotValue
                    turnNumber={index + 1}
                    moveName={turn.moveName}
                    detail={formatSlotDetail(turn.mpCost, turn.targetLabel)}
                  />
                )}
                inputProps={{ 'aria-label': `${turnLabel} ${locale.moveLabel}` }}
                onChange={(event: SelectChangeEvent<string>) =>
                  onChangeMoveId(index, event.target.value === 'normal-attack' ? null : Number(event.target.value))
                }
                sx={{
                  borderRadius: '10px',
                  backgroundColor: '#fffdf8',
                  '& .MuiOutlinedInput-notchedOutline': { borderColor: '#e3d6c2' },
                  '&:hover .MuiOutlinedInput-notchedOutline': { borderColor: '#c9b791' },
                  '&.Mui-focused .MuiOutlinedInput-notchedOutline': { borderColor: trainingPanelColors.band },
                  '& .MuiSelect-select': { py: 1, pl: 1.5, minHeight: 44, display: 'flex', alignItems: 'center' },
                  '& .MuiSvgIcon-root': { color: homePanelColors.label },
                }}
              >
                <MenuItem value="normal-attack" sx={{ color: homePanelColors.text }}>
                  {locale.normalAttack}
                </MenuItem>
                {availableMoves.map((move) => (
                  <MenuItem key={move.moveId} value={String(move.moveId)} sx={{ color: homePanelColors.text }}>
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
        direction={{ xs: 'column', sm: 'row' }}
        spacing={1.5}
        alignItems={{ xs: 'stretch', sm: 'center' }}
        sx={{ pb: 1.5, borderBottom: dottedDivider }}
      >
        <Stack direction="row" spacing={1.5} alignItems="center" sx={{ flex: 1, minWidth: 0 }}>
          <Box
            sx={{
              width: 52,
              height: 52,
              flex: 'none',
              borderRadius: '10px',
              display: 'grid',
              placeItems: 'center',
              backgroundColor: '#e9dfd0',
              backgroundImage:
                enemyBackgroundKey !== undefined ? `url(${getTrainingBackgroundPath(enemyBackgroundKey)})` : 'none',
              backgroundSize: 'cover',
              backgroundPosition: 'center',
            }}
          >
            {enemyImageSrc ? (
              <Box component="img" src={enemyImageSrc} alt="" sx={{ maxWidth: 46, maxHeight: 46 }} />
            ) : null}
          </Box>
          <Stack sx={{ minWidth: 0 }}>
            <Typography variant="caption" sx={{ color: homePanelColors.label }}>
              {locale.nextOpponent}
            </Typography>
            <Typography fontWeight={800} sx={{ color: homePanelColors.text, overflowWrap: 'anywhere' }}>
              {enemy?.name ?? locale.notSelected}{' '}
              {enemyLevelLabel ? (
                <Typography component="span" variant="body2" fontWeight={700} sx={{ color: homePanelColors.label }}>
                  {enemyLevelLabel}
                </Typography>
              ) : null}
            </Typography>
          </Stack>
        </Stack>
        <Box sx={{ width: { xs: '100%', sm: 260 }, flex: 'none' }}>
          <MpMeter plannedMp={totalMp} maxMp={maxMp} />
        </Box>
      </Stack>
      {turnSlots}
      <Box sx={{ flex: 1 }} />
      {battleButton}
    </Stack>
  )
}
