import {
  Box,
  Button,
  Chip,
  FormControl,
  LinearProgress,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import type { SelectChangeEvent } from '@mui/material/Select'
import { greenOutlinedInputSx, innerSurfaceSx, playerHpBarSx, softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type {
  BattleColumn,
  BattleRow,
  QuestActionKind,
  QuestChatMessageView,
  QuestPartyMemberView,
  QuestRunDetailResponse,
} from '@/schema/quest'

type AvailableMove = {
  slot: number
  moveId: number | null
  moveName: string
  targetType: 'Enemy' | 'Ally' | 'Self' | null
  attackRange: 'Single' | 'Column' | 'Row' | 'Square' | 'All' | null
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
  currentPendingCommand: {
    actionKind: QuestActionKind
    submittedAt: string
  } | null
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
    leaveFinishedRun: string
    labels: {
      none: string
    }
    actionKinds: Record<'NormalAttack' | 'UseMove' | 'Prayer' | 'Guard' | 'Wait' | 'LeaveQuest' | 'Escape', string>
  }
}

const allyPositions: Record<BattleRow, Record<BattleColumn, { left: string; bottom: string }>> = {
  Front: {
    Left: { left: '28%', bottom: '16%' },
    Right: { left: '49%', bottom: '16%' },
  },
  Middle: {
    Left: { left: '20%', bottom: '31%' },
    Right: { left: '41%', bottom: '31%' },
  },
  Back: {
    Left: { left: '12%', bottom: '46%' },
    Right: { left: '33%', bottom: '46%' },
  },
}

const enemyPositions: Record<BattleRow, Record<BattleColumn, { left: string; bottom: string }>> = {
  Front: {
    Left: { left: '67%', bottom: '16%' },
    Right: { left: '83%', bottom: '16%' },
  },
  Middle: {
    Left: { left: '73%', bottom: '31%' },
    Right: { left: '89%', bottom: '31%' },
  },
  Back: {
    Left: { left: '79%', bottom: '46%' },
    Right: { left: '95%', bottom: '46%' },
  },
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
        px: { xs: 0.45, sm: 0.8 },
        py: { xs: 0.32, sm: 0.55 },
        borderRadius: 1.75,
        borderColor: 'rgba(123, 92, 44, 0.28)',
        backgroundColor: 'rgba(255, 248, 224, 0.88)',
        boxShadow: 'none',
        opacity: member.isDead ? 0.62 : 1,
      }}
    >
      <Stack spacing={0.05} sx={{ minWidth: 0 }}>
        <Typography
          variant="caption"
          noWrap
          title={member.displayName}
          sx={{ fontWeight: 800, color: '#503717', fontSize: { xs: '0.42rem', sm: '0.68rem' }, lineHeight: 1.05 }}
        >
          {member.displayName}
        </Typography>
        <Typography
          variant="caption"
          noWrap
          sx={{ color: '#624927', fontSize: { xs: '0.38rem', sm: '0.62rem' }, lineHeight: 1.05 }}
        >
          HP {member.currentHp}/{member.maxHp ?? '-'}
        </Typography>
        <Typography
          variant="caption"
          noWrap
          sx={{ color: '#30506e', fontSize: { xs: '0.38rem', sm: '0.62rem' }, lineHeight: 1.05 }}
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
  isSelected?: boolean
  targetState?: 'preview' | 'affected' | 'reachable' | 'blocked' | 'none'
  onClick?: () => void
  left: string
  bottom: string
  chatMessage?: QuestChatMessageView | null
}

function BattleSprite({
  name,
  imagePath,
  hp,
  maxHp,
  isDead,
  isAlly,
  isSelected = false,
  targetState = 'none',
  onClick,
  left,
  bottom,
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
        width: isAlly ? { xs: 84, sm: 96, md: 110 } : { xs: 58, sm: 66, md: 76 },
        transform: 'translateX(-50%)',
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
            py: 0.7,
            borderRadius: 2,
            border: '1px solid rgba(112, 83, 31, 0.4)',
            backgroundColor: 'rgba(255, 252, 241, 0.96)',
            boxShadow: '0 10px 20px rgba(44, 31, 18, 0.18)',
            zIndex: 4,
            pointerEvents: 'none',
            '&::after': {
              content: '""',
              position: 'absolute',
              left: '50%',
              bottom: -7,
              width: 12,
              height: 12,
              backgroundColor: 'rgba(255, 252, 241, 0.96)',
              borderRight: '1px solid rgba(112, 83, 31, 0.4)',
              borderBottom: '1px solid rgba(112, 83, 31, 0.4)',
              transform: 'translateX(-50%) rotate(45deg)',
            },
          }}
        >
          <Typography
            variant="caption"
            sx={{
              display: '-webkit-box',
              overflow: 'hidden',
              color: '#503717',
              textAlign: 'center',
              lineHeight: 1.35,
              fontSize: { xs: '0.62rem', sm: '0.7rem' },
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
        spacing={0.35}
        sx={{
          mb: 0.4,
          px: 0.25,
          width: isAlly
            ? { xs: 'calc(100% - 34px)', sm: 'calc(100% - 10px)' }
            : { xs: 'calc(100% - 14px)', sm: 'calc(100% + 10px)' },
          ml: isAlly ? { xs: '17px', sm: '5px' } : { xs: '7px', sm: '-5px' },
        }}
      >
        <Typography
          variant="caption"
          fontWeight={700}
          noWrap
          title={name}
          textAlign="center"
          sx={{
            fontSize: { xs: '0.46rem', sm: '0.75rem' },
            color: isDead ? 'rgba(90,90,90,0.92)' : '#3f2f16',
            textShadow: '0 1px 0 rgba(255,255,255,0.55)',
          }}
        >
          {name}
        </Typography>
        <Typography
          variant="caption"
          color="text.secondary"
          textAlign="center"
          sx={{ fontSize: { xs: '0.43rem', sm: '0.75rem' }, textShadow: '0 1px 0 rgba(255,255,255,0.55)' }}
        >
          HP {hp}/{maxHp ?? '-'}
        </Typography>
        <LinearProgress variant="determinate" value={getStatusRate(hp, maxHp)} sx={playerHpBarSx} />
      </Stack>

      <Box
        sx={{
          position: 'relative',
          height: { xs: 94, sm: 106, md: 120 },
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
              transform: isAlly ? 'scaleX(-1)' : 'none',
              filter: isDead ? 'grayscale(1)' : 'none',
              opacity: isDead ? 0.78 : 1,
              outline: isSelected ? '3px solid #ffffff' : 'none',
              outlineOffset: isSelected ? '2px' : 0,
            }}
          />
        ) : (
          <Box
            sx={{
              width: '100%',
              height: '100%',
              borderRadius: 2,
              backgroundColor: 'rgba(255,255,255,0.55)',
              border: '1px dashed rgba(120, 88, 32, 0.45)',
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
                  ? '3px solid #6df075'
                  : targetState === 'affected'
                    ? '2px solid rgba(109, 240, 117, 0.92)'
                    : targetState === 'reachable'
                      ? '2px dashed rgba(109, 240, 117, 0.92)'
                      : '2px dashed rgba(160, 160, 160, 0.88)',
              boxShadow:
                targetState === 'preview'
                  ? '0 0 0 4px rgba(109, 240, 117, 0.22), 0 12px 20px rgba(16, 52, 18, 0.28)'
                  : targetState === 'affected'
                    ? '0 0 0 3px rgba(109, 240, 117, 0.14)'
                    : 'none',
              backgroundColor:
                targetState === 'preview'
                  ? 'rgba(109, 240, 117, 0.12)'
                  : targetState === 'affected'
                    ? 'rgba(109, 240, 117, 0.08)'
                    : targetState === 'blocked'
                      ? 'rgba(80, 80, 80, 0.18)'
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
              border: '2px solid rgba(183, 28, 28, 0.88)',
              backgroundColor: 'rgba(183, 28, 28, 0.14)',
              boxShadow: '0 0 0 3px rgba(183, 28, 28, 0.12)',
              pointerEvents: 'none',
              '&::before, &::after': {
                content: '""',
                position: 'absolute',
                top: '50%',
                left: '50%',
                width: '78%',
                height: 3,
                borderRadius: 999,
                backgroundColor: 'rgba(183, 28, 28, 0.92)',
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
  currentPendingCommand,
  canSubmitCurrentTurn,
  isCommandSubmitting,
  onActionKindChange,
  onMoveChange,
  onTargetRowChange,
  onTargetColumnChange,
  onSubmitCommand,
  locale,
}: QuestBattleStatusPanelProps) {
  const battlefieldImageSrc = resolvePublicAssetPath(battlefieldImagePath ?? 'image/quest/dummy-battlefield.svg')
  const actionOptions: Array<{ value: QuestActionKind; label: string }> = [
    { value: 'UseMove', label: locale.actionKinds.UseMove },
    { value: 'NormalAttack', label: locale.actionKinds.NormalAttack },
    { value: 'Guard', label: locale.actionKinds.Guard },
    { value: 'Wait', label: locale.actionKinds.Wait },
    { value: 'Escape', label: locale.actionKinds.Escape },
  ]

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
    selectedActionKind === 'NormalAttack' || (selectedActionKind === 'UseMove' && selectedMove?.targetType === 'Enemy')
  const effectiveAttackRange =
    selectedActionKind === 'NormalAttack'
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
          onClick={() => onActionKindChange(option.value)}
          sx={{
            minWidth: { xs: 38, sm: 92 },
            width: { xs: 38, sm: 'auto' },
            minHeight: { xs: 38, sm: 46 },
            borderRadius: 2.5,
            px: { xs: 0.2, sm: 1.75 },
            py: { xs: 0.15, sm: 0.75 },
            fontWeight: 700,
            fontSize: { xs: '0.52rem', sm: '0.9375rem' },
            lineHeight: { xs: 1.02, sm: 1.3 },
            textAlign: 'center',
            whiteSpace: 'normal',
            letterSpacing: '0.02em',
            color: selectedActionKind === option.value ? '#fffdf4' : '#6a4300',
            backgroundColor: selectedActionKind === option.value ? '#8c4b16' : '#ffc83d',
            border: 'none',
            boxShadow: 'none',
            '&:hover': {
              backgroundColor: selectedActionKind === option.value ? '#8c4b16' : '#ffc83d',
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
            sx={{ backgroundColor: '#fff8e2' }}
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
          fontSize: { xs: '0.56rem', sm: '0.9375rem' },
          ...softGreenButtonSx,
        }}
      >
        {isCommandSubmitting ? locale.submittingCommand : locale.submitCommand}
      </Button>
    </>
  )

  return (
    <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 1.5, sm: 2.5 } }}>
      <Stack spacing={2}>
        <Stack direction="row" spacing={1} useFlexGap flexWrap="wrap">
          <Chip label={`${locale.waitingParticipantsLabel}: ${run.turn.waitingParticipantIds.length}`} />
        </Stack>

        <Box
          sx={{
            position: 'relative',
            overflow: 'hidden',
            borderRadius: 4,
            minHeight: { xs: 500, sm: 575, md: 620 },
            border: '1px solid #c7a96f',
            backgroundColor: '#d9e6df',
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

          {currentPendingCommand ? (
            <Chip
              color="success"
              label={`${locale.pendingCommandTitle}: ${locale.actionKinds[currentPendingCommand.actionKind]}`}
              sx={{
                display: 'inline-flex',
                position: 'absolute',
                top: { xs: 58, sm: 72 },
                right: { xs: 10, sm: 16 },
                zIndex: 4,
                maxWidth: { xs: 'calc(100% - 20px)', sm: 'none' },
              }}
            />
          ) : null}

          <Box
            sx={{
              position: 'absolute',
              inset: 0,
              background:
                'linear-gradient(180deg, rgba(255,255,255,0.08) 0%, rgba(255,255,255,0) 24%, rgba(0,0,0,0.04) 100%)',
            }}
          />

          <Box
            sx={{
              position: 'absolute',
              left: 0,
              right: 0,
              bottom: { xs: '12.5%', sm: '12%', md: '11.5%' },
              height: 2,
              backgroundColor: 'rgba(215, 174, 73, 0.7)',
              zIndex: 1,
            }}
          />

          {sortedPartyMembers.map((member) => {
            const position = allyPositions[member.position.row][member.position.column]
            const allyKey = `${member.position.row}:${member.position.column}`
            const isActor = member.participantId === selfParticipantId
            const isReachableAlly = !member.isDead && !isActor
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
              gap: { xs: 0.45, sm: 0.65 },
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
    </Paper>
  )
}
