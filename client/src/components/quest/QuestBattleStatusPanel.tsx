import {
  Box,
  Button,
  Chip,
  Dialog,
  DialogActions,
  DialogContent,
  DialogContentText,
  DialogTitle,
  FormControl,
  LinearProgress,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { useRef } from 'react'
import { greenOutlinedInputSx, playerHpBarSx, softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import { resolveEndlessBattlefield } from '@/lib/endless'
import type { MoveAttackRange } from '@/schema/player'
import type {
  BattleColumn,
  BattleRow,
  QuestActionKind,
  QuestChatMessageView,
  QuestPartyMemberView,
  QuestRunDetailResponse,
} from '@/schema/quest'
import { font, palette, withAlpha } from '@/theme/tokens'

type AvailableMove = {
  slot: number
  moveId: number | null
  moveName: string
  targetType: 'Enemy' | 'Ally' | 'Self' | null
  targetLifeState: 'Alive' | 'Dead' | 'Any' | null | undefined
  attackRange: MoveAttackRange | null
}

type QuestBattleStatusPanelProps = {
  run: QuestRunDetailResponse
  battlefieldImagePath: string | null
  selfParticipantId: string | null
  availableMoves: AvailableMove[]
  selectedActionKind: QuestActionKind
  selectedMoveId: number | ''
  selectedTargetRow: BattleRow | ''
  selectedTargetColumn: BattleColumn | ''
  canCapture: boolean
  canSummonPet: boolean
  canSubmitCurrentTurn: boolean
  isCommandSubmitting: boolean
  onActionKindChange: (actionKind: QuestActionKind) => void
  onMoveChange: (moveId: number | '') => void
  onTargetRowChange: (row: BattleRow | '') => void
  onTargetColumnChange: (column: BattleColumn | '') => void
  onSubmitCommand: () => void | Promise<void>
  locale: {
    runStatus: Record<'InProgress' | 'Succeeded' | 'Failed' | 'Aborted', string>
    waitingParticipantsLabel: string
    submitCommand: string
    submittingCommand: string
    pendingCommandTitle: string
    petDeploymentLabel: string
    leaveFinishedRun: string
    labels: {
      none: string
    }
    actionKinds: Record<
      | 'NormalAttack'
      | 'UseMove'
      | 'Prayer'
      | 'Guard'
      | 'Wait'
      | 'LeaveQuest'
      | 'Escape'
      | 'EndlessFinish'
      | 'Capture'
      | 'SummonPet'
      | 'EntrustPet',
      string
    >
    endlessCheckpoint: {
      bannerTitle: string
      bannerBody: string
      confirmTitle: string
      confirmBody: string
      confirmButton: string
      cancelButton: string
    }
  }
}

// 立ち位置は画面の中央を軸に左右対称。奥の列ほど上・内側に置く。
// 戦場背景（image/quest/*-battlefield.png）は下から 44% あたりまでが地面になっている。
const allyPositions: Record<BattleRow, Record<BattleColumn, { left: string; bottom: string }>> = {
  Front: {
    Left: { left: '24%', bottom: '16%' },
    Right: { left: '40%', bottom: '16%' },
  },
  Middle: {
    Left: { left: '17%', bottom: '27%' },
    Right: { left: '33%', bottom: '27%' },
  },
  Back: {
    Left: { left: '10%', bottom: '38%' },
    Right: { left: '26%', bottom: '38%' },
  },
}

const enemyPositions: Record<BattleRow, Record<BattleColumn, { left: string; bottom: string }>> = {
  Front: {
    Left: { left: '60%', bottom: '16%' },
    Right: { left: '76%', bottom: '16%' },
  },
  Middle: {
    Left: { left: '67%', bottom: '27%' },
    Right: { left: '83%', bottom: '27%' },
  },
  Back: {
    Left: { left: '74%', bottom: '38%' },
    Right: { left: '90%', bottom: '38%' },
  },
}

// 奥の列ほど少し小さく見せて遠近感を出す
const battleRowScale: Record<BattleRow, number> = {
  Front: 1,
  Middle: 0.92,
  Back: 0.85,
}

const battleRowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const battleColumnOrder: BattleColumn[] = ['Left', 'Right']

function getLatestQuestChatMessages(messages: QuestChatMessageView[]) {
  const map = new Map<string, QuestChatMessageView>()

  for (const message of messages) {
    const existing = map.get(message.senderParticipantId)
    if (!existing || new Date(existing.sentAt) < new Date(message.sentAt)) {
      map.set(message.senderParticipantId, message)
    }
  }

  return map
}

function truncateChatBubbleText(text: string, limit = 36) {
  if (text.length <= limit) {
    return text
  }

  return `${text.slice(0, limit - 1)}…`
}

function AllyStatusCard({ member }: { member: QuestPartyMemberView }) {
  return (
    <Paper
      variant="outlined"
      sx={{
        px: { xs: 0.5, sm: 0.75 },
        py: { xs: 0.25, sm: 0.5 },
        borderRadius: 1.75,
        borderColor: withAlpha(palette.brown500, 0.25),
        backgroundColor: withAlpha(palette.cream, 0.9),
        boxShadow: 'none',
        opacity: member.isDead ? 0.62 : 1,
      }}
    >
      <Stack spacing={0} sx={{ minWidth: 0 }}>
        <Typography
          variant="caption"
          noWrap
          title={member.displayName}
          sx={{ fontWeight: 800, color: palette.brown900, fontSize: { xs: font.xxs, sm: font.md }, lineHeight: 1.2 }}
        >
          {member.displayName}
        </Typography>
        <Typography
          variant="caption"
          noWrap
          sx={{ color: palette.brown900, fontSize: { xs: font.xxs, sm: font.sm }, lineHeight: 1.2 }}
        >
          HP {member.currentHp}/{member.maxHp ?? '-'}
        </Typography>
        <Typography
          variant="caption"
          noWrap
          sx={{ color: palette.teal700, fontSize: { xs: font.xxs, sm: font.sm }, lineHeight: 1.2 }}
        >
          MP {member.currentMp}/{member.maxMp ?? '-'}
        </Typography>
      </Stack>
    </Paper>
  )
}

function getStatusRate(current: number, max: number | null | undefined): number {
  if (!max || max <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (current / max) * 100))
}

type BattleSpriteProps = {
  name: string
  imagePath: string | null | undefined
  hp: number
  maxHp: number | null | undefined
  isDead: boolean
  isAlly: boolean
  // 右側に並ぶ敵は左右反転して、味方と向かい合わせる
  isEnemy?: boolean
  isSelected?: boolean
  targetState?: 'preview' | 'affected' | 'reachable' | 'blocked' | 'none'
  onClick?: () => void
  left: string
  bottom: string
  scale?: number
  chatMessage?: QuestChatMessageView | null
}

function BattleSprite({
  name,
  imagePath,
  hp,
  maxHp,
  isDead,
  isAlly,
  isEnemy = false,
  isSelected = false,
  targetState = 'none',
  onClick,
  left,
  bottom,
  scale = 1,
  chatMessage,
}: BattleSpriteProps) {
  const spriteSrc = isAlly ? resolveCharacterAssetPath(imagePath) : imagePath ? resolvePublicAssetPath(imagePath) : null
  const bubbleText = chatMessage ? truncateChatBubbleText(chatMessage.message, 50) : null

  return (
    <Box
      sx={{
        position: 'absolute',
        left,
        bottom,
        // 敵（48px のドット絵）は味方と同じくらいの大きさで見えるよう、画像の枠の高さいっぱいまで広げる
        // PC の広い画面ではバトル画面が大きくなるので、キャラも大きくする
        width: isAlly ? { xs: 84, sm: 96, md: 110, lg: 150, xl: 180 } : { xs: 84, sm: 106, md: 120, lg: 160, xl: 190 },
        transform: `translateX(-50%) scale(${scale})`,
        transformOrigin: '50% 100%',
        zIndex: 2,
        cursor: onClick ? 'pointer' : 'default',
        opacity: targetState === 'blocked' ? 0.72 : 1,
      }}
      onClick={onClick}
    >
      {bubbleText ? (
        <Box
          sx={{
            position: 'absolute',
            left: '50%',
            bottom: '100%',
            transform: 'translate(-50%, -10px)',
            width: { xs: 132, sm: 168 },
            px: 1,
            py: 0.75,
            borderRadius: 2,
            border: `1px solid ${withAlpha(palette.brown500, 0.5)}`,
            backgroundColor: palette.cream,
            boxShadow: `0 10px 20px ${withAlpha(palette.brown900, 0.25)}`,
            zIndex: 4,
            pointerEvents: 'none',
            '&::after': {
              content: '""',
              position: 'absolute',
              left: '50%',
              bottom: -7,
              width: 12,
              height: 12,
              backgroundColor: palette.cream,
              borderRight: `1px solid ${withAlpha(palette.brown500, 0.5)}`,
              borderBottom: `1px solid ${withAlpha(palette.brown500, 0.5)}`,
              transform: 'translateX(-50%) rotate(45deg)',
            },
          }}
        >
          <Typography
            variant="caption"
            sx={{
              display: '-webkit-box',
              overflow: 'hidden',
              color: palette.brown900,
              textAlign: 'center',
              lineHeight: 1.35,
              fontSize: { xs: font.xs, sm: font.sm },
              wordBreak: 'break-word',
              WebkitLineClamp: 2,
              WebkitBoxOrient: 'vertical',
            }}
          >
            {bubbleText}
          </Typography>
        </Box>
      ) : null}

      <Stack
        spacing={0.25}
        sx={{
          mb: 0.5,
          px: 0.25,
          width: isAlly
            ? { xs: 'calc(100% - 34px)', sm: 'calc(100% - 10px)' }
            : { xs: 'calc(100% + 22px)', sm: 'calc(100% + 10px)' },
          // 余白ではなく、上の width（calc）と組になった位置合わせの値
          // eslint-disable-next-line no-restricted-syntax
          ml: isAlly ? { xs: '17px', sm: '5px' } : { xs: '-11px', sm: '-5px' },
        }}
      >
        <Typography
          variant="caption"
          fontWeight={700}
          noWrap={isAlly}
          title={name}
          textAlign="center"
          sx={{
            fontSize: { xs: font.xxs, sm: font.md },
            color: isDead ? withAlpha(palette.teal700, 0.9) : palette.brown900,
            textShadow: `0 1px 0 ${withAlpha(palette.white, 0.5)}`,
            lineHeight: isAlly ? 1.1 : { xs: 1.02, sm: 1.1 },
            wordBreak: isAlly ? 'normal' : 'keep-all',
            display: isAlly ? 'block' : '-webkit-box',
            overflow: 'hidden',
            WebkitLineClamp: isAlly ? 'none' : 2,
            WebkitBoxOrient: isAlly ? 'initial' : 'vertical',
            minHeight: isAlly ? 'auto' : { xs: '1.26rem', sm: 'auto' },
          }}
        >
          {name}
        </Typography>
        <Typography
          variant="caption"
          color="text.secondary"
          textAlign="center"
          sx={{ fontSize: { xs: font.xxs, sm: font.sm }, textShadow: `0 1px 0 ${withAlpha(palette.white, 0.5)}` }}
        >
          HP {hp}/{maxHp ?? '-'}
        </Typography>
        <LinearProgress variant="determinate" value={getStatusRate(hp, maxHp)} sx={playerHpBarSx} />
      </Stack>

      <Box
        sx={{
          position: 'relative',
          height: { xs: 94, sm: 106, md: 120, lg: 160, xl: 190 },
          display: 'flex',
          alignItems: 'flex-end',
          justifyContent: 'center',
        }}
      >
        {spriteSrc ? (
          <Box
            component="img"
            src={spriteSrc}
            alt={name}
            sx={{
              width: '100%',
              height: '100%',
              objectFit: 'contain',
              objectPosition: 'center bottom',
              display: 'block',
              transform: isAlly || isEnemy ? 'scaleX(-1)' : 'none',
              // 敵のドット絵は拡大してもぼやけないようにする
              imageRendering: isAlly ? 'auto' : 'pixelated',
              filter: isDead ? 'grayscale(1)' : 'none',
              opacity: isDead ? 0.78 : 1,
              outline: isSelected ? `3px solid ${palette.white}` : 'none',
              outlineOffset: isSelected ? '2px' : 0,
            }}
          />
        ) : (
          <Box
            sx={{
              width: '100%',
              height: '100%',
              borderRadius: 2,
              backgroundColor: withAlpha(palette.white, 0.5),
              border: `1px dashed ${withAlpha(palette.brown500, 0.5)}`,
            }}
          />
        )}

        {targetState !== 'none' ? (
          <Box
            sx={{
              position: 'absolute',
              inset: targetState === 'preview' ? -5 : -3,
              borderRadius: 2.5,
              border:
                targetState === 'preview'
                  ? `3px solid ${palette.green400}`
                  : targetState === 'affected'
                    ? `2px solid ${withAlpha(palette.green400, 0.9)}`
                    : targetState === 'reachable'
                      ? `2px dashed ${withAlpha(palette.green400, 0.9)}`
                      : `2px dashed ${withAlpha(palette.gray600, 0.9)}`,
              boxShadow:
                targetState === 'preview'
                  ? `0 0 0 4px ${withAlpha(palette.green400, 0.25)}, 0 12px 20px ${withAlpha(palette.green700, 0.25)}`
                  : targetState === 'affected'
                    ? `0 0 0 3px ${withAlpha(palette.green400, 0.1)}`
                    : 'none',
              backgroundColor:
                targetState === 'preview'
                  ? withAlpha(palette.green400, 0.1)
                  : targetState === 'affected'
                    ? withAlpha(palette.green400, 0.1)
                    : targetState === 'blocked'
                      ? withAlpha(palette.brown900, 0.25)
                      : 'transparent',
              pointerEvents: 'none',
            }}
          />
        ) : null}

        {isDead ? (
          <Box
            sx={{
              position: 'absolute',
              inset: -4,
              borderRadius: 2.5,
              border: `2px solid ${withAlpha(palette.red700, 0.9)}`,
              backgroundColor: withAlpha(palette.red700, 0.1),
              boxShadow: `0 0 0 3px ${withAlpha(palette.red700, 0.1)}`,
              pointerEvents: 'none',
              '&::before, &::after': {
                content: '""',
                position: 'absolute',
                top: '50%',
                left: '50%',
                width: '78%',
                height: 3,
                borderRadius: 999,
                backgroundColor: withAlpha(palette.red700, 0.9),
                transformOrigin: 'center',
              },
              '&::before': {
                transform: 'translate(-50%, -50%) rotate(28deg)',
              },
              '&::after': {
                transform: 'translate(-50%, -50%) rotate(-28deg)',
              },
            }}
          />
        ) : null}
      </Box>
    </Box>
  )
}

export default function QuestBattleStatusPanel({
  run,
  battlefieldImagePath,
  selfParticipantId,
  availableMoves,
  selectedActionKind,
  selectedMoveId,
  selectedTargetRow,
  selectedTargetColumn,
  canCapture,
  canSummonPet,
  canSubmitCurrentTurn,
  isCommandSubmitting,
  onActionKindChange,
  onMoveChange,
  onTargetRowChange,
  onTargetColumnChange,
  onSubmitCommand,
  locale,
}: QuestBattleStatusPanelProps) {
  // エンドレスはテーマ帯ごとに背景を切り替える。
  const effectiveBattlefieldImagePath = run.floor.isEndless
    ? (resolveEndlessBattlefield(run.floor.themeNo) ?? battlefieldImagePath)
    : battlefieldImagePath
  const battlefieldImageSrc = resolvePublicAssetPath(
    effectiveBattlefieldImagePath ?? 'image/quest/dummy-battlefield.svg',
  )
  // エンドレスでは「逃走」を boss_interval 倍数フロア（チェックポイント）限定の「終了（成功）」に転用する。
  const isEndlessCheckpointFloor =
    run.floor.isEndless === true &&
    run.floor.bossInterval != null &&
    run.floor.bossInterval > 0 &&
    run.floor.currentFloorNo % run.floor.bossInterval === 0
  const escapeActionOption: Array<{ value: QuestActionKind; label: string }> = !run.floor.isEndless
    ? [{ value: 'Escape', label: locale.actionKinds.Escape }]
    : isEndlessCheckpointFloor
      ? [{ value: 'Escape', label: locale.actionKinds.EndlessFinish }]
      : []
  // 終了ダイアログを開く前の選択。キャンセル時にこの選択へ戻す。
  const actionKindBeforeFinishDialogRef = useRef<QuestActionKind>(selectedActionKind)
  // 確認ダイアログの開閉は専用 state を持たず選択状態から派生させる。こうすると SignalR 更新で
  // 非チェックポイント階へ遷移した場合に自動で閉じ、開閉フラグが取り残されることもない。
  // 選択された 'Escape' 自体の差し戻しは Quest 側のアクション妥当性チェックで行う。
  const isEndlessFinishConfirmOpen =
    run.floor.isEndless === true && selectedActionKind === 'Escape' && isEndlessCheckpointFloor
  const endlessCheckpointBannerTitle = locale.endlessCheckpoint.bannerTitle.replace(
    '{{interval}}',
    String(run.floor.bossInterval ?? ''),
  )
  const endlessFinishConfirmBody = locale.endlessCheckpoint.confirmBody.replace(
    '{{floor}}',
    String(run.floor.currentFloorNo),
  )
  // 「終了」はランを確定終了する不可逆操作。チェックポイント階では選択即確認ダイアログを挟む。
  const handleActionButtonClick = (actionKind: QuestActionKind) => {
    if (run.floor.isEndless && actionKind === 'Escape') {
      // 'Escape' を選択すると派生値 isEndlessFinishConfirmOpen が真になり確認ダイアログが開く。
      actionKindBeforeFinishDialogRef.current = selectedActionKind
      onActionKindChange('Escape')
      return
    }

    onActionKindChange(actionKind)
  }
  const handleEndlessFinishCancel = () => {
    // 選択を開く前へ戻すと派生値が偽になりダイアログが閉じる。送信ボタンからの誤 Escape も防げる。
    onActionKindChange(actionKindBeforeFinishDialogRef.current)
  }
  const handleEndlessFinishConfirm = () => {
    void onSubmitCommand()
  }
  // ペット出撃中はプレイヤー側が下がっているため、「ペットにまかせる」だけを出す。
  const petDeployment = run.petSummon.deployment
  const isPetDeploymentActive = petDeployment != null
  const actionOptions: Array<{ value: QuestActionKind; label: string }> = isPetDeploymentActive
    ? [{ value: 'EntrustPet', label: locale.actionKinds.EntrustPet }]
    : [
        { value: 'UseMove', label: locale.actionKinds.UseMove },
        { value: 'NormalAttack', label: locale.actionKinds.NormalAttack },
        { value: 'Guard', label: locale.actionKinds.Guard },
        // 「待機」はフロントエンドの選択肢からは除外（リクエスト経由では引き続き利用可能）。
        ...(canCapture ? [{ value: 'Capture' as const, label: locale.actionKinds.Capture }] : []),
        ...(canSummonPet ? [{ value: 'SummonPet' as const, label: locale.actionKinds.SummonPet }] : []),
        ...escapeActionOption,
      ]
  const deployedPets = petDeployment?.pets ?? []

  const selectedMove = availableMoves.find((move) => move.moveId === selectedMoveId) ?? null
  const selectedTargetKey =
    selectedTargetRow !== '' && selectedTargetColumn !== '' ? `${selectedTargetRow}:${selectedTargetColumn}` : null
  const selfPartyMember = selfParticipantId
    ? (run.partyMembers.find((member) => member.participantId === selfParticipantId) ?? null)
    : null
  const isAllyTargetingAction = selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Ally'
  const isSelfTargetingAction = selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Self'
  const aliveEnemies = run.enemies
    .filter((enemy) => !enemy.isDead)
    .sort((left, right) => {
      if (left.position.row !== right.position.row) {
        return battleRowOrder.indexOf(left.position.row) - battleRowOrder.indexOf(right.position.row)
      }

      if (left.position.column === right.position.column) {
        return 0
      }

      return battleColumnOrder.indexOf(left.position.column) - battleColumnOrder.indexOf(right.position.column)
    })
  const occupiedRows = Array.from(new Set(aliveEnemies.map((enemy) => enemy.position.row)))
  const reachableRows = new Set<BattleRow>(
    selfPartyMember == null
      ? []
      : occupiedRows.slice(
          0,
          selfPartyMember.position.row === 'Front'
            ? 1
            : selfPartyMember.position.row === 'Middle'
              ? 2
              : occupiedRows.length,
        ),
  )
  const reachableEnemies = aliveEnemies.filter((enemy) => reachableRows.has(enemy.position.row))
  const selectedAnchorEnemy =
    selectedTargetKey == null
      ? null
      : (reachableEnemies.find((enemy) => `${enemy.position.row}:${enemy.position.column}` === selectedTargetKey) ??
        null)
  const anchorEnemy = selectedAnchorEnemy ?? reachableEnemies[0] ?? null
  const isEnemyTargetingAction =
    selectedActionKind === 'NormalAttack' ||
    selectedActionKind === 'Capture' ||
    (selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Enemy')
  const effectiveAttackRange =
    selectedActionKind === 'NormalAttack' || selectedActionKind === 'Capture'
      ? 'Single'
      : selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Enemy'
        ? (selectedMove.attackRange ?? 'Single')
        : null
  const previewTargetKeys = new Set(
    !isEnemyTargetingAction || anchorEnemy == null || effectiveAttackRange == null
      ? []
      : reachableEnemies
          .filter((enemy) => {
            if (effectiveAttackRange === 'All') {
              return true
            }

            if (effectiveAttackRange === 'Single') {
              return enemy.enemyInstanceId === anchorEnemy.enemyInstanceId
            }

            if (effectiveAttackRange === 'Column') {
              return enemy.position.column === anchorEnemy.position.column
            }

            if (effectiveAttackRange === 'Row') {
              return enemy.position.row === anchorEnemy.position.row
            }

            return false
          })
          .map((enemy) => `${enemy.position.row}:${enemy.position.column}`),
  )

  if (effectiveAttackRange === 'Square' && anchorEnemy != null) {
    reachableEnemies
      .filter((enemy) => {
        return battleRowOrder.indexOf(enemy.position.row) <= battleRowOrder.indexOf(anchorEnemy.position.row) + 1
      })
      .slice(0, 4)
      .forEach((enemy) => previewTargetKeys.add(`${enemy.position.row}:${enemy.position.column}`))
  }

  const reachableTargetKeys = new Set(reachableEnemies.map((enemy) => `${enemy.position.row}:${enemy.position.column}`))
  const anchorTargetKey = anchorEnemy == null ? null : `${anchorEnemy.position.row}:${anchorEnemy.position.column}`
  const chatEntries = [...run.chatMessages, ...(run.lastTurnResults?.chatMessages ?? [])]
  const allyChatMap = getLatestQuestChatMessages(chatEntries)
  const sortedPartyMembers = [...run.partyMembers].sort((left, right) => {
    if (left.position.row !== right.position.row) {
      return battleRowOrder.indexOf(left.position.row) - battleRowOrder.indexOf(right.position.row)
    }

    return battleColumnOrder.indexOf(left.position.column) - battleColumnOrder.indexOf(right.position.column)
  })

  const handleMoveChange = (event: SelectChangeEvent<string>) => {
    const nextValue = String(event.target.value)
    onMoveChange(nextValue === '' ? '' : Number(nextValue))
  }

  const commandControls = (
    <>
      {actionOptions.map((option) => (
        <Button
          key={option.value}
          variant="contained"
          disableRipple
          onClick={() => handleActionButtonClick(option.value)}
          sx={{
            minWidth: { xs: 38, sm: 92 },
            width: { xs: 38, sm: 'auto' },
            minHeight: { xs: 38, sm: 46 },
            borderRadius: 2.5,
            px: { xs: 0.25, sm: 2 },
            py: { xs: 0.25, sm: 0.75 },
            fontWeight: 700,
            fontSize: { xs: font.xxs, sm: font.md },
            lineHeight: { xs: 1.02, sm: 1.3 },
            textAlign: 'center',
            whiteSpace: 'normal',
            letterSpacing: '0.02em',
            color: selectedActionKind === option.value ? palette.cream : palette.gold700,
            backgroundColor: selectedActionKind === option.value ? palette.brown500 : palette.yellow,
            border: 'none',
            boxShadow: 'none',
            '&:hover': {
              backgroundColor: selectedActionKind === option.value ? palette.brown500 : palette.yellow,
              boxShadow: 'none',
            },
          }}
        >
          {option.label}
        </Button>
      ))}

      {selectedActionKind === 'UseMove' ? (
        <FormControl sx={{ minWidth: { xs: 116, sm: 180 }, ml: { sm: 'auto' }, ...greenOutlinedInputSx }}>
          <Select
            displayEmpty
            value={selectedMoveId === '' ? '' : String(selectedMoveId)}
            onChange={handleMoveChange}
            size="small"
            sx={{ backgroundColor: palette.cream }}
          >
            <MenuItem value="">{locale.labels.none}</MenuItem>
            {availableMoves.map((move) => (
              <MenuItem key={move.slot} value={move.moveId!}>
                {move.moveName}
              </MenuItem>
            ))}
          </Select>
        </FormControl>
      ) : null}

      <Button
        variant="contained"
        onClick={() => void onSubmitCommand()}
        disabled={isCommandSubmitting || !canSubmitCurrentTurn}
        sx={{
          minWidth: { xs: 64, sm: 124 },
          minHeight: { xs: 38, sm: 46 },
          ml: selectedActionKind === 'UseMove' ? 0 : { sm: 'auto' },
          fontSize: { xs: font.xxs, sm: font.md },
          ...softGreenButtonSx,
        }}
      >
        {isCommandSubmitting ? locale.submittingCommand : locale.submitCommand}
      </Button>
    </>
  )

  return (
    // 白い箱（GamePanel）の中に端まで広げて置く。待機中の人数は箱の見出しに出す
    <Box sx={{ flex: 1, display: 'flex', flexDirection: 'column' }}>
      <Stack spacing={0} sx={{ flex: 1 }}>
        {run.floor.isEndless ? (
          isEndlessCheckpointFloor ? (
            <Box
              sx={{
                borderRadius: 2.5,
                px: { xs: 1.5, sm: 2 },
                py: { xs: 1, sm: 1.5 },
                backgroundColor: palette.cream,
                border: `1px solid ${palette.gold300}`,
                color: palette.gold700,
              }}
            >
              <Typography sx={{ fontWeight: 700, fontSize: { xs: font.xs, sm: font.md } }}>
                ★ {endlessCheckpointBannerTitle}
              </Typography>
              <Typography sx={{ mt: 0.25, fontSize: { xs: font.xs, sm: font.sm }, lineHeight: 1.5 }}>
                {locale.endlessCheckpoint.bannerBody}
              </Typography>
            </Box>
          ) : null
        ) : null}

        <Box
          sx={{
            position: 'relative',
            overflow: 'hidden',
            flex: 1,
            minHeight: { xs: 500, sm: 575, md: 620 },
            backgroundColor: palette.gray300,
          }}
        >
          <Box
            component="img"
            src={battlefieldImageSrc}
            alt=""
            aria-hidden="true"
            onError={(event) => {
              const fallbackSrc = resolvePublicAssetPath('image/quest/dummy-battlefield.svg')
              if (event.currentTarget.getAttribute('src') === fallbackSrc) {
                return
              }

              event.currentTarget.setAttribute('src', fallbackSrc)
            }}
            sx={{
              position: 'absolute',
              inset: 0,
              width: '100%',
              height: '100%',
              objectFit: 'cover',
              objectPosition: 'center',
              zIndex: 0,
              pointerEvents: 'none',
              userSelect: 'none',
            }}
          />

          <Stack
            direction="row"
            spacing={0.25}
            useFlexGap
            flexWrap="wrap"
            sx={{
              display: 'flex',
              position: 'absolute',
              top: { xs: 10, sm: 14 },
              left: { xs: 10, sm: 16 },
              right: { xs: 10, sm: 16 },
              zIndex: 4,
              alignItems: 'center',
            }}
          >
            {commandControls}
          </Stack>

          {petDeployment != null ? (
            <Chip
              color="warning"
              label={locale.petDeploymentLabel.replace('{{turns}}', String(petDeployment.remainingTurns))}
              sx={{
                display: 'inline-flex',
                position: 'absolute',
                top: { xs: 58, sm: 72 },
                left: { xs: 10, sm: 16 },
                zIndex: 4,
                fontWeight: 700,
              }}
            />
          ) : null}

          <Box
            sx={{
              position: 'absolute',
              inset: 0,
              background: `linear-gradient(180deg, ${withAlpha(palette.white, 0.1)} 0%, transparent 24%, ${withAlpha(palette.navy, 0.1)} 100%)`,
            }}
          />

          <Box
            sx={{
              position: 'absolute',
              left: 0,
              right: 0,
              bottom: { xs: '12.5%', sm: '12%', md: '11.5%' },
              height: 2,
              backgroundColor: withAlpha(palette.gold300, 0.75),
              zIndex: 1,
            }}
          />

          {deployedPets.map((pet) => {
            const position = allyPositions[pet.position.row][pet.position.column]

            return (
              <BattleSprite
                key={pet.actorId}
                name={pet.name}
                imagePath={pet.imagePath}
                hp={pet.currentHp}
                maxHp={pet.maxHp}
                isDead={pet.isDead}
                isAlly={false}
                left={position.left}
                bottom={position.bottom}
                scale={battleRowScale[pet.position.row]}
              />
            )
          })}

          {(isPetDeploymentActive ? [] : sortedPartyMembers).map((member) => {
            const position = allyPositions[member.position.row][member.position.column]
            const allyKey = `${member.position.row}:${member.position.column}`
            const isActor = member.participantId === selfParticipantId
            const isReachableAlly = !member.isDead && (!isActor || selectedMove?.targetLifeState === 'Alive')
            const isSelectedAlly = selectedTargetKey === allyKey

            let allyTargetState: BattleSpriteProps['targetState'] = 'none'
            if (isAllyTargetingAction) {
              allyTargetState = isSelectedAlly ? 'preview' : isReachableAlly ? 'reachable' : 'blocked'
            } else if (isSelfTargetingAction && isActor) {
              allyTargetState = 'preview'
            }

            return (
              <BattleSprite
                key={member.participantId}
                name={member.displayName}
                imagePath={member.imagePath}
                hp={member.currentHp}
                maxHp={member.maxHp}
                isDead={member.isDead}
                isAlly
                targetState={allyTargetState}
                onClick={
                  isAllyTargetingAction && isReachableAlly
                    ? () => {
                        onTargetRowChange(member.position.row)
                        onTargetColumnChange(member.position.column)
                      }
                    : undefined
                }
                left={position.left}
                bottom={position.bottom}
                scale={battleRowScale[member.position.row]}
                chatMessage={allyChatMap.get(member.participantId) ?? null}
              />
            )
          })}

          {run.enemies.map((enemy) => {
            const position = enemyPositions[enemy.position.row][enemy.position.column]
            const targetKey = `${enemy.position.row}:${enemy.position.column}`

            return (
              <BattleSprite
                key={enemy.enemyInstanceId}
                name={enemy.name}
                imagePath={enemy.imagePath}
                hp={enemy.currentHp}
                maxHp={enemy.maxHp}
                isDead={enemy.isDead}
                isAlly={false}
                isEnemy
                isSelected={targetKey === anchorTargetKey}
                targetState={
                  !isEnemyTargetingAction
                    ? 'none'
                    : targetKey === anchorTargetKey
                      ? 'preview'
                      : previewTargetKeys.has(targetKey)
                        ? 'affected'
                        : reachableTargetKeys.has(targetKey)
                          ? 'reachable'
                          : 'blocked'
                }
                onClick={
                  enemy.isDead || !reachableTargetKeys.has(targetKey) || !isEnemyTargetingAction
                    ? undefined
                    : () => {
                        onTargetRowChange(enemy.position.row)
                        onTargetColumnChange(enemy.position.column)
                      }
                }
                left={position.left}
                bottom={position.bottom}
                scale={battleRowScale[enemy.position.row]}
              />
            )
          })}

          <Box
            sx={{
              position: 'absolute',
              left: { xs: 8, sm: 16 },
              right: { xs: 8, sm: 16 },
              bottom: { xs: 6, sm: 10 },
              zIndex: 3,
              display: 'grid',
              gap: { xs: 0.5, sm: 0.75 },
              gridTemplateColumns: {
                xs: 'repeat(2, minmax(0, 1fr))',
                sm: 'repeat(3, minmax(0, 1fr))',
                md: 'repeat(6, minmax(0, 1fr))',
              },
            }}
          >
            {sortedPartyMembers.map((member) => (
              <AllyStatusCard key={`status-band-${member.participantId}`} member={member} />
            ))}
          </Box>
        </Box>
      </Stack>

      <Dialog open={isEndlessFinishConfirmOpen} onClose={handleEndlessFinishCancel}>
        <DialogTitle>{locale.endlessCheckpoint.confirmTitle}</DialogTitle>
        <DialogContent>
          <DialogContentText>{endlessFinishConfirmBody}</DialogContentText>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleEndlessFinishCancel} disabled={isCommandSubmitting}>
            {locale.endlessCheckpoint.cancelButton}
          </Button>
          <Button
            variant="contained"
            onClick={handleEndlessFinishConfirm}
            disabled={isCommandSubmitting || !canSubmitCurrentTurn}
          >
            {locale.endlessCheckpoint.confirmButton}
          </Button>
        </DialogActions>
      </Dialog>
    </Box>
  )
}
