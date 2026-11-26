import { Box, ButtonBase, Collapse, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import { trainingPanelColors } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { ExecuteTrainingResponse } from '@/schema/training'
import locale from '../../../locale/training/Training.json'
import { getTrainingBackgroundPath } from './trainingSelectStyles'
import { font, palette, withAlpha } from '@/theme/tokens'

export type TrainingBattleFighter = {
  name: string
  imagePath: string
  level: number
}

type TrainingBattleFieldProps = {
  player: TrainingBattleFighter
  // まだ相手を選んでいないときは null（「？ 未選択」を出す）
  enemy: TrainingBattleFighter | null
  // 背景を決めるキー（NPC は敵の id、PLAYER はプレイヤーの id）
  backgroundKey: string | number
  // 戦闘のあとだけ渡す。渡したときは HP とログを出す
  result?: ExecuteTrainingResponse | null
  usedMoveNames?: string[]
  playerExp?: number
  nextLevelRequiredExp?: number
}

type TurnLog = {
  turn: number
  playerAction: string | null
  enemyAction: string | null
}

// 自分と相手の画像を置く枠の高さ（大きいほうの画像に合わせる）
const fighterAreaHeight = { xs: 120, md: 230 }

const logTextColor = palette.brown900
const logDivider = `1px dotted ${withAlpha(palette.navy, 0.25)}`

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
  hp,
  hpBarColor,
  imageHeight,
}: {
  fighter: TrainingBattleFighter
  // 戦闘のあとだけ HP を出す
  hp: { current: number; max: number } | null
  hpBarColor: string
  imageHeight: { xs: number; md: number }
}) {
  return (
    <Stack alignItems="center" spacing={0.5} sx={{ minWidth: 0 }}>
      <Box
        sx={{
          minWidth: { xs: 120, sm: 170 },
          px: 2,
          py: 0.75,
          borderRadius: '10px',
          backgroundColor: withAlpha(palette.white, 0.75),
          textAlign: 'center',
        }}
      >
        <Typography
          fontWeight={800}
          sx={{ color: trainingPanelColors.text, lineHeight: 1.3, overflowWrap: 'anywhere' }}
        >
          {fighter.name}{' '}
          <Typography component="span" variant="body2" fontWeight={700} sx={{ color: trainingPanelColors.label }}>
            {locale.currentLevel.replace('{{level}}', String(fighter.level))}
          </Typography>
        </Typography>
        {hp ? (
          <>
            <Box sx={{ height: 8, borderRadius: 9, backgroundColor: palette.sky, my: 0.5, overflow: 'hidden' }}>
              <Box sx={{ width: `${normalizeHp(hp.current, hp.max)}%`, height: '100%', backgroundColor: hpBarColor }} />
            </Box>
            <Typography variant="body2" sx={{ color: trainingPanelColors.label }}>
              HP {hp.current} / {hp.max}
            </Typography>
          </>
        ) : null}
      </Box>
      {/* 画像の枠の高さは自分・相手で同じにし、画像は下端にそろえる（頭上の札の高さがずれないように） */}
      <Box
        sx={{
          height: fighterAreaHeight,
          maxWidth: '100%',
          display: 'flex',
          alignItems: 'flex-end',
          justifyContent: 'center',
        }}
      >
        <Box
          component="img"
          src={resolvePublicAssetPath(fighter.imagePath)}
          alt={fighter.name}
          sx={{ height: imageHeight, maxWidth: '100%', objectFit: 'contain' }}
        />
      </Box>
    </Stack>
  )
}

// まだ相手を選んでいないときの相手側（点線の「？」だけ。頭上の札の分だけ下げて、自分の画像と下端をそろえる）
function EmptyFighter({ imageHeight }: { imageHeight: { xs: number; md: number } }) {
  return (
    <Box
      aria-hidden="true"
      sx={{
        justifySelf: 'center',
        height: imageHeight,
        aspectRatio: '3 / 4',
        boxSizing: 'border-box',
        borderRadius: '12px',
        border: `3px dashed ${withAlpha(palette.white, 0.9)}`,
        backgroundColor: withAlpha(palette.white, 0.25),
        display: 'grid',
        placeItems: 'center',
        color: palette.white,
        fontSize: { xs: font.display, md: font.display },
        fontWeight: 900,
        textShadow: `0 2px 0 ${withAlpha(palette.navy, 0.25)}`,
      }}
    >
      ?
    </Box>
  )
}

// 訓練の戦闘画面。クエストと同じく背景の上に自分と相手を並べる。
// 戦闘の前は向かい合うだけ、戦闘のあとは HP と結果のログ（誰が勝ったか・経験値など）を出す
export default function TrainingBattleField({
  player,
  enemy,
  backgroundKey,
  result = null,
  usedMoveNames = [],
  playerExp,
  nextLevelRequiredExp,
}: TrainingBattleFieldProps) {
  const [isTurnLogOpen, setIsTurnLogOpen] = useState(false)
  const turnLogs = result && enemy ? buildTurnLogs(player, enemy, result, usedMoveNames) : []
  const resultLogs = result && enemy ? buildResultLogs(player, enemy, result) : []
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
        px: { xs: 2, md: 6 },
        py: { xs: 2, md: 3 },
        backgroundColor: palette.sky,
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
          hp={result ? { current: result.currentPlayerHp, max: result.maxPlayerHp } : null}
          hpBarColor={palette.green400}
          imageHeight={{ xs: 120, md: 230 }}
        />
        <Typography
          sx={{
            alignSelf: 'center',
            fontSize: { xs: font.xxxl, md: font.display },
            fontWeight: 900,
            color: palette.white,
            textShadow: `0 2px 0 ${withAlpha(palette.navy, 0.5)}`,
          }}
        >
          VS
        </Typography>
        {enemy ? (
          <FighterSprite
            fighter={enemy}
            hp={result ? { current: result.currentEnemyHp, max: result.maxEnemyHp } : null}
            hpBarColor={palette.red300}
            imageHeight={{ xs: 96, md: 184 }}
          />
        ) : (
          <EmptyFighter imageHeight={{ xs: 96, md: 184 }} />
        )}
      </Box>
      {result ? (
        <Box
          sx={{
            alignSelf: 'center',
            maxHeight: '100%',
            overflowY: 'auto',
            boxSizing: 'border-box',
            px: 3,
            py: 1.5,
            borderRadius: '12px',
            backgroundColor: withAlpha(palette.white, 0.75),
            // スマホはキャラと同じく中央にそろえる
            textAlign: { xs: 'center', md: 'left' },
          }}
        >
          <Collapse in={isTurnLogOpen}>
            <Stack sx={{ pb: 1, mb: 0.5, borderBottom: `1px solid ${withAlpha(palette.navy, 0.25)}` }}>
              {turnLogs.map((log) => (
                <Stack
                  key={log.turn}
                  direction={{ xs: 'column', md: 'row' }}
                  spacing={{ xs: 0.25, md: 1.5 }}
                  alignItems={{ xs: 'center', md: 'flex-start' }}
                  sx={{ py: 0.5 }}
                >
                  <Typography
                    variant="body2"
                    fontWeight={800}
                    sx={{ color: trainingPanelColors.label, whiteSpace: 'nowrap', minWidth: { md: 72 } }}
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
                  py: 0.75,
                  borderTop: index === 0 ? 'none' : logDivider,
                  color: logTextColor,
                  fontWeight: 400,
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
              spacing={1.5}
              alignItems="center"
              sx={{ pt: 1.5, mt: 0.5, borderTop: `1px solid ${withAlpha(palette.navy, 0.25)}` }}
            >
              <Typography
                variant="body2"
                fontWeight={800}
                sx={{ color: trainingPanelColors.label, whiteSpace: 'nowrap' }}
              >
                {locale.currentLevel.replace('{{level}}', String(player.level))}
              </Typography>
              <Box
                aria-hidden="true"
                sx={{ flex: 1, height: 8, borderRadius: 9, backgroundColor: palette.sky, overflow: 'hidden' }}
              >
                <Box sx={{ width: `${expRatio * 100}%`, height: '100%', backgroundColor: palette.blue300 }} />
              </Box>
              <Typography
                variant="body2"
                fontWeight={700}
                sx={{ color: trainingPanelColors.label, whiteSpace: 'nowrap' }}
              >
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
              fontSize: font.sm,
              fontWeight: 800,
              color: trainingPanelColors.label,
            }}
          >
            {isTurnLogOpen ? `${locale.hideTurnLog} ▴` : `${locale.showTurnLog} ▾`}
          </ButtonBase>
        </Box>
      ) : (
        // PC は右の列を空けておき、戦闘の前後で自分と相手の位置が動かないようにする
        <Box sx={{ display: { xs: 'none', md: 'block' } }} />
      )}
    </Box>
  )
}
