import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { Box, Chip, Collapse, IconButton, LinearProgress, Stack, Typography } from '@mui/material'
import { useState, type ReactNode } from 'react'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

export type TrainingBattleFighter = {
  name: string
  imagePath: string
  level: number
}

type TrainingBattleResultProps = {
  player: TrainingBattleFighter
  enemy: TrainingBattleFighter
  result: ExecuteTrainingResponse
  usedMoveNames: string[]
  playerExp?: number
  nextLevelRequiredExp?: number
  movePlanSlot?: ReactNode
}

type TurnLog = {
  turn: number
  playerAction: string | null
  enemyAction: string | null
}

const resultLabelByOutcome: Record<ExecuteTrainingResponse['trainingResult'], string> = {
  Win: locale.resultWin,
  Lose: locale.resultLose,
  Draw: locale.resultDraw,
}

const resultColorByOutcome: Record<ExecuteTrainingResponse['trainingResult'], string> = {
  Win: '#f3d27a',
  Lose: '#e88b7a',
  Draw: '#d8cfc0',
}

function normalizeHp(current: number, max: number): number {
  if (max <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (current / max) * 100))
}

function formatAction(actorName: string, moveName: string): string {
  return locale.actionLog.replace('{{actor}}', actorName).replace('{{move}}', moveName)
}

// 行動順はレスポンスに含まれないため、左右に分けて表示する。
// 決着がついた最終ターンは、倒れた側が行動できたか分からないので勝った側の行動だけを出す。
function buildTurnLogs(
  player: TrainingBattleFighter,
  enemy: TrainingBattleFighter,
  result: ExecuteTrainingResponse,
  usedMoveNames: string[],
): TurnLog[] {
  return Array.from({ length: result.turn }, (_, index) => {
    const isFinalTurn = index === result.turn - 1
    const playerDown = isFinalTurn && result.currentPlayerHp <= 0
    const enemyDown = isFinalTurn && result.currentEnemyHp <= 0

    return {
      turn: index + 1,
      playerAction: playerDown ? null : formatAction(player.name, usedMoveNames[index] ?? locale.normalAttack),
      enemyAction: enemyDown ? null : formatAction(enemy.name, locale.normalAttack),
    }
  })
}

function buildClosingMessage(
  player: TrainingBattleFighter,
  enemy: TrainingBattleFighter,
  result: ExecuteTrainingResponse,
): string {
  if (result.currentEnemyHp <= 0) {
    return locale.defeatedLog.replace('{{name}}', enemy.name)
  }

  if (result.currentPlayerHp <= 0) {
    return locale.defeatedLog.replace('{{name}}', player.name)
  }

  return locale.drawLog
}

function FighterPanel({
  fighter,
  currentHp,
  maxHp,
  hpBarColor,
}: {
  fighter: TrainingBattleFighter
  currentHp: number
  maxHp: number
  hpBarColor: string
}) {
  return (
    <Stack spacing={0.75} alignItems="center" sx={{ minWidth: 0 }}>
      <Typography
        variant="subtitle1"
        fontWeight={800}
        textAlign="center"
        sx={{ color: '#fff7dd', lineHeight: 1.25, overflowWrap: 'anywhere' }}
      >
        {fighter.name}
      </Typography>
      <Typography variant="caption" sx={{ color: 'rgba(245, 240, 223, 0.72)' }}>
        {locale.currentLevel.replace('{{level}}', String(fighter.level))}
      </Typography>
      <Box
        component="img"
        src={resolvePublicAssetPath(fighter.imagePath)}
        alt={fighter.name}
        sx={{
          width: '100%',
          maxWidth: 180,
          height: { xs: 110, sm: 150 },
          objectFit: 'contain',
          opacity: currentHp <= 0 ? 0.45 : 1,
          filter: currentHp <= 0 ? 'grayscale(0.8)' : 'none',
        }}
      />
      <Stack spacing={0.4} sx={{ width: '100%', maxWidth: 220 }}>
        <Stack direction="row" justifyContent="space-between" alignItems="baseline" spacing={1}>
          <Typography variant="caption" fontWeight={800} sx={{ color: '#fff0c8' }}>
            HP
          </Typography>
          <Typography variant="caption" fontWeight={700} sx={{ color: '#fff7dd' }}>
            {currentHp} / {maxHp}
          </Typography>
        </Stack>
        <LinearProgress
          variant="determinate"
          value={normalizeHp(currentHp, maxHp)}
          sx={{
            height: 8,
            borderRadius: 999,
            backgroundColor: 'rgba(245, 240, 223, 0.14)',
            '& .MuiLinearProgress-bar': {
              backgroundColor: hpBarColor,
            },
          }}
        />
      </Stack>
    </Stack>
  )
}

export default function TrainingBattleResult({
  player,
  enemy,
  result,
  usedMoveNames,
  playerExp,
  nextLevelRequiredExp,
  movePlanSlot,
}: TrainingBattleResultProps) {
  const [isTurnLogOpen, setIsTurnLogOpen] = useState(false)
  const turnLogs = buildTurnLogs(player, enemy, result, usedMoveNames)
  const closingMessage = buildClosingMessage(player, enemy, result)
  const expValue = locale.expValue.replace('{{exp}}', String(result.exp))
  const expProgressLabel =
    typeof playerExp === 'number' && typeof nextLevelRequiredExp === 'number'
      ? locale.expProgress
          .replace('{{current}}', String(playerExp))
          .replace('{{required}}', String(nextLevelRequiredExp))
      : null
  const levelUpLabel =
    result.isPlayerLevelUp && result.isJobLevelUp
      ? locale.playerAndJobLevelUp
      : result.isPlayerLevelUp
        ? locale.playerLevelUp
        : result.isJobLevelUp
          ? locale.levelUp
          : null

  return (
    <Stack spacing={2} sx={{ color: '#f5f0df' }}>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: 'minmax(0, 1fr) auto minmax(0, 1fr)',
          alignItems: 'center',
          gap: { xs: 1, sm: 2 },
        }}
      >
        <FighterPanel
          fighter={player}
          currentHp={result.currentPlayerHp}
          maxHp={result.maxPlayerHp}
          hpBarColor="#78c27d"
        />
        <Typography variant="h6" fontWeight={900} sx={{ color: 'rgba(243, 179, 141, 0.8)' }}>
          VS
        </Typography>
        <FighterPanel
          fighter={enemy}
          currentHp={result.currentEnemyHp}
          maxHp={result.maxEnemyHp}
          hpBarColor="#b65f49"
        />
      </Box>

      <Stack spacing={0.5} alignItems="center">
        <Stack alignItems="center" spacing={0}>
          <Typography variant="body1" fontWeight={700} textAlign="center" sx={{ color: '#fff7dd' }}>
            {closingMessage}
          </Typography>
          <IconButton
            size="small"
            onClick={() => setIsTurnLogOpen((current) => !current)}
            aria-expanded={isTurnLogOpen}
            aria-label={isTurnLogOpen ? locale.hideTurnLog : locale.showTurnLog}
            sx={{ color: 'rgba(245, 240, 223, 0.72)' }}
          >
            <ExpandMoreIcon
              fontSize="small"
              sx={{ transform: isTurnLogOpen ? 'rotate(180deg)' : 'none', transition: 'transform 150ms ease' }}
            />
          </IconButton>
        </Stack>
        <Collapse in={isTurnLogOpen} sx={{ width: '100%' }}>
          <Stack spacing={0.75} sx={{ pt: 0.5 }}>
            {turnLogs.map((log) => (
              <Box
                key={log.turn}
                sx={{
                  display: 'grid',
                  gridTemplateColumns: { xs: '1fr', sm: 'minmax(0, 1fr) auto minmax(0, 1fr)' },
                  gridTemplateAreas: { xs: '"turn" "player" "enemy"', sm: '"player turn enemy"' },
                  alignItems: 'center',
                  columnGap: 1,
                  rowGap: 0.25,
                  textAlign: { xs: 'center', sm: 'initial' },
                }}
              >
                <Typography
                  variant="body2"
                  sx={{
                    gridArea: 'player',
                    textAlign: { xs: 'center', sm: 'right' },
                    color: '#fff7dd',
                    overflowWrap: 'anywhere',
                  }}
                >
                  {log.playerAction ?? ''}
                </Typography>
                <Typography
                  variant="caption"
                  fontWeight={800}
                  sx={{ gridArea: 'turn', color: '#f3b38d', whiteSpace: 'nowrap' }}
                >
                  {locale.turnLabel.replace('{{turn}}', String(log.turn))}
                </Typography>
                <Typography
                  variant="body2"
                  sx={{
                    gridArea: 'enemy',
                    textAlign: { xs: 'center', sm: 'left' },
                    color: '#fff7dd',
                    overflowWrap: 'anywhere',
                  }}
                >
                  {log.enemyAction ?? ''}
                </Typography>
              </Box>
            ))}
          </Stack>
        </Collapse>
      </Stack>

      <Stack spacing={1} alignItems="center">
        <Typography variant="h5" fontWeight={900} sx={{ color: resultColorByOutcome[result.trainingResult] }}>
          {resultLabelByOutcome[result.trainingResult]}
        </Typography>
        <Typography variant="body2" fontWeight={800} sx={{ color: '#fff7dd' }}>
          {locale.currentLevel.replace('{{level}}', String(player.level))}
          {expProgressLabel ? ` / ${expProgressLabel}` : ''}
        </Typography>
        <Typography variant="h6" fontWeight={900} sx={{ color: '#f3b38d' }}>
          {expValue}
        </Typography>
        {levelUpLabel ? (
          <Chip
            label={levelUpLabel}
            sx={{
              fontWeight: 800,
              color: '#1d2b20',
              backgroundColor: '#d9e6be',
            }}
          />
        ) : null}
        {result.newlyLearnedMoves.length > 0 ? (
          <Stack spacing={0.75} alignItems="center">
            <Typography variant="body2" fontWeight={800} sx={{ color: '#fff0c8' }}>
              {locale.newlyLearnedMoves}
            </Typography>
            <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap justifyContent="center">
              {result.newlyLearnedMoves.map((move) => (
                <Chip
                  key={move.moveId}
                  label={move.moveName}
                  variant="outlined"
                  sx={{
                    color: '#f5f0df',
                    borderColor: 'rgba(214, 146, 112, 0.38)',
                  }}
                />
              ))}
            </Stack>
          </Stack>
        ) : null}
      </Stack>

      {movePlanSlot}
    </Stack>
  )
}
