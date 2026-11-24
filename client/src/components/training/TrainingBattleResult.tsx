import { Box, ButtonBase, Collapse, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { homePanelColors } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse } from '@/schema/training'
import locale from '../../../locale/training/Training.json'
import { getTrainingBackgroundPath } from './trainingSelectStyles'

export type TrainingBattleFighter = {
  name: string
  imagePath: string
  level: number
}

type TrainingBattleResultProps = {
  player: TrainingBattleFighter
  enemy: TrainingBattleFighter
  // 背景を決めるキー（NPC は敵の id、PLAYER はプレイヤーの id）
  backgroundKey: string | number
  result: ExecuteTrainingResponse
  usedMoveNames: string[]
  playerExp?: number
  nextLevelRequiredExp?: number
}

type TurnLog = {
  turn: number
  playerAction: string | null
  enemyAction: string | null
}

const logTextColor = '#3b2f1f'
const logDivider = '1px dotted rgba(90, 70, 40, 0.3)'

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

// 結果のログ。誰が勝ったか・経験値・レベルアップ・覚えた技を 1 行ずつ出す
function buildResultLogs(
  player: TrainingBattleFighter,
  enemy: TrainingBattleFighter,
  result: ExecuteTrainingResponse,
): string[] {
  const logs: string[] = []

  if (result.trainingResult === 'Win') {
    logs.push(locale.winnerLog.replace('{{name}}', player.name))
  } else if (result.trainingResult === 'Lose') {
    logs.push(locale.winnerLog.replace('{{name}}', enemy.name))
  } else {
    logs.push(locale.drawLog)
  }

  logs.push(locale.expGainedLog.replace('{{exp}}', String(result.exp)))

  if (result.isPlayerLevelUp) {
    logs.push(locale.playerLevelUpLog.replace('{{name}}', player.name))
  }

  if (result.isJobLevelUp) {
    logs.push(locale.jobLevelUpLog.replace('{{name}}', player.name))
  }

  for (const move of result.newlyLearnedMoves) {
    logs.push(locale.learnedMoveLog.replace('{{move}}', move.moveName))
  }

  return logs
}

// キャラの頭上に出す名前・レベル・HP の札（背景が見えるよう半透明）
function FighterSprite({
  fighter,
  currentHp,
  maxHp,
  hpBarColor,
  imageHeight,
}: {
  fighter: TrainingBattleFighter
  currentHp: number
  maxHp: number
  hpBarColor: string
  imageHeight: { xs: number; md: number }
}) {
  return (
    <Stack alignItems="center" spacing={0.5} sx={{ minWidth: 0 }}>
      <Box
        sx={{
          minWidth: { xs: 120, sm: 170 },
          px: 1.75,
          py: 0.75,
          borderRadius: '10px',
          backgroundColor: 'rgba(255, 255, 255, 0.55)',
          textAlign: 'center',
        }}
      >
        <Typography fontWeight={800} sx={{ color: homePanelColors.text, lineHeight: 1.3, overflowWrap: 'anywhere' }}>
          {fighter.name}{' '}
          <Typography component="span" variant="body2" fontWeight={700} sx={{ color: homePanelColors.label }}>
            {locale.currentLevel.replace('{{level}}', String(fighter.level))}
          </Typography>
        </Typography>
        <Box sx={{ height: 8, borderRadius: 9, backgroundColor: '#e5d9bd', my: 0.6, overflow: 'hidden' }}>
          <Box sx={{ width: `${normalizeHp(currentHp, maxHp)}%`, height: '100%', backgroundColor: hpBarColor }} />
        </Box>
        <Typography variant="body2" sx={{ color: homePanelColors.label }}>
          HP {currentHp} / {maxHp}
        </Typography>
      </Box>
      <Box
        component="img"
        src={resolvePublicAssetPath(fighter.imagePath)}
        alt={fighter.name}
        sx={{ height: imageHeight, maxWidth: '100%', objectFit: 'contain' }}
      />
    </Stack>
  )
}

// 訓練の戦闘結果。クエストの戦闘画面と同じく背景の上に自分と相手を並べ、結果はログの形で出す
export default function TrainingBattleResult({
  player,
  enemy,
  backgroundKey,
  result,
  usedMoveNames,
  playerExp,
  nextLevelRequiredExp,
}: TrainingBattleResultProps) {
  const [isTurnLogOpen, setIsTurnLogOpen] = useState(false)
  const turnLogs = buildTurnLogs(player, enemy, result, usedMoveNames)
  const resultLogs = buildResultLogs(player, enemy, result)
  const hasExpProgress = typeof playerExp === 'number' && typeof nextLevelRequiredExp === 'number'
  const expRatio = hasExpProgress && nextLevelRequiredExp > 0 ? Math.min(1, playerExp / nextLevelRequiredExp) : 0

  return (
    <Box
      sx={{
        // GamePanel の中身（縦並びの flex）いっぱいに広げる
        flex: 1,
        minHeight: 0,
        boxSizing: 'border-box',
        display: 'grid',
        gridTemplateColumns: { xs: 'minmax(0, 1fr)', md: 'minmax(0, 1fr) auto minmax(0, 1fr) 440px' },
        alignItems: 'end',
        gap: { xs: 2, md: 3 },
        px: { xs: 2, md: 8 },
        py: { xs: 2, md: 3 },
        backgroundColor: '#e9dfd0',
        backgroundImage: `url(${getTrainingBackgroundPath(backgroundKey)})`,
        backgroundSize: 'cover',
        backgroundPosition: 'center',
      }}
    >
      <Box
        sx={{
          gridColumn: { xs: '1', md: 'auto' },
          display: { xs: 'grid', md: 'contents' },
          gridTemplateColumns: 'minmax(0, 1fr) auto minmax(0, 1fr)',
          alignItems: 'end',
          gap: 1,
        }}
      >
        <FighterSprite
          fighter={player}
          currentHp={result.currentPlayerHp}
          maxHp={result.maxPlayerHp}
          hpBarColor="#3f9f4a"
          imageHeight={{ xs: 120, md: 230 }}
        />
        <Typography
          sx={{
            alignSelf: 'center',
            fontSize: { xs: '1.75rem', md: '2.5rem' },
            fontWeight: 900,
            color: '#ffffff',
            textShadow: '0 2px 0 rgba(0, 0, 0, 0.4)',
          }}
        >
          VS
        </Typography>
        <FighterSprite
          fighter={enemy}
          currentHp={result.currentEnemyHp}
          maxHp={result.maxEnemyHp}
          hpBarColor="#b65f49"
          imageHeight={{ xs: 96, md: 184 }}
        />
      </Box>
      <Box
        sx={{
          alignSelf: 'center',
          maxHeight: '100%',
          overflowY: 'auto',
          boxSizing: 'border-box',
          px: 2.75,
          py: 1.5,
          borderRadius: '12px',
          backgroundColor: 'rgba(255, 253, 248, 0.6)',
        }}
      >
        <Collapse in={isTurnLogOpen}>
          <Stack sx={{ pb: 1, mb: 0.5, borderBottom: '1px solid rgba(90, 70, 40, 0.25)' }}>
            {turnLogs.map((log) => (
              <Stack key={log.turn} direction="row" spacing={1.5} sx={{ py: 0.5 }}>
                <Typography
                  variant="body2"
                  fontWeight={800}
                  sx={{ color: homePanelColors.label, whiteSpace: 'nowrap', minWidth: 52 }}
                >
                  {locale.turnLabel.replace('{{turn}}', String(log.turn))}
                </Typography>
                <Stack sx={{ minWidth: 0 }}>
                  {log.playerAction ? (
                    <Typography variant="body2" sx={{ color: logTextColor }}>
                      {log.playerAction}
                    </Typography>
                  ) : null}
                  {log.enemyAction ? (
                    <Typography variant="body2" sx={{ color: logTextColor }}>
                      {log.enemyAction}
                    </Typography>
                  ) : null}
                </Stack>
              </Stack>
            ))}
          </Stack>
        </Collapse>
        <Stack>
          {resultLogs.map((log, index) => (
            <Typography
              key={`${index}-${log}`}
              sx={{
                py: 0.85,
                borderTop: index === 0 ? 'none' : logDivider,
                color: logTextColor,
                fontWeight: 600,
                lineHeight: 1.5,
              }}
            >
              {log}
            </Typography>
          ))}
        </Stack>
        {hasExpProgress ? (
          <Stack
            direction="row"
            spacing={1.25}
            alignItems="center"
            sx={{ pt: 1.25, mt: 0.5, borderTop: '1px solid rgba(90, 70, 40, 0.25)' }}
          >
            <Typography variant="body2" fontWeight={800} sx={{ color: homePanelColors.label, whiteSpace: 'nowrap' }}>
              {locale.currentLevel.replace('{{level}}', String(player.level))}
            </Typography>
            <Box
              aria-hidden="true"
              sx={{ flex: 1, height: 8, borderRadius: 9, backgroundColor: '#e9e2d6', overflow: 'hidden' }}
            >
              <Box sx={{ width: `${expRatio * 100}%`, height: '100%', backgroundColor: '#b9a98a' }} />
            </Box>
            <Typography variant="body2" fontWeight={700} sx={{ color: homePanelColors.label, whiteSpace: 'nowrap' }}>
              {locale.expProgress
                .replace('{{current}}', String(playerExp))
                .replace('{{required}}', String(nextLevelRequiredExp))}
            </Typography>
          </Stack>
        ) : null}
        <ButtonBase
          onClick={() => setIsTurnLogOpen((current) => !current)}
          aria-expanded={isTurnLogOpen}
          sx={{
            mt: 1,
            py: 0.5,
            fontFamily: 'inherit',
            fontSize: '0.85rem',
            fontWeight: 800,
            color: homePanelColors.label,
          }}
        >
          {isTurnLogOpen ? `${locale.hideTurnLog} ▴` : `${locale.showTurnLog} ▾`}
        </ButtonBase>
      </Box>
    </Box>
  )
}
