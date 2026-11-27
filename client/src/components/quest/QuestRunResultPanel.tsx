import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import questRoomLocale from '../../../locale/quest/QuestRoom.json'
import type { BattleColumn, BattleRow, QuestRunDetailResponse } from '@/schema/quest'
import { font, palette, withAlpha } from '@/theme/tokens'

type QuestRunResultPanelProps = {
  run: QuestRunDetailResponse
  endlessBestFloor?: number | null
  onLeaveFinishedRun: () => void
  locale: {
    clearTitle: string
    clearSubtitle: string
    failedTitle: string
    failedSubtitle: string
    leaveFinishedRun: string
    noImage: string
    rewardTitle: string
    rewardExpLabel: string
    rewardGoldLabel: string
    rewardEquipmentLabel: string
    rewardItemLabel: string
    rewardNone: string
    rewardInventoryFullSkipped: string
  }
}

const battleRowOrder: BattleRow[] = ['Front', 'Middle', 'Back']
const battleColumnOrder: BattleColumn[] = ['Left', 'Right']

export default function QuestRunResultPanel({
  run,
  endlessBestFloor,
  onLeaveFinishedRun,
  locale,
}: QuestRunResultPanelProps) {
  if (run.status !== 'Succeeded' && run.status !== 'Failed') {
    return null
  }

  const isEndless = run.floor.isEndless === true
  const isMirror = run.floor.isMirror === true
  const reachedFloor = run.floor.currentFloorNo

  const partyMembers = [...run.partyMembers].sort((left, right) => {
    if (left.position.row !== right.position.row) {
      return battleRowOrder.indexOf(left.position.row) - battleRowOrder.indexOf(right.position.row)
    }

    return battleColumnOrder.indexOf(left.position.column) - battleColumnOrder.indexOf(right.position.column)
  })

  const isSucceeded = run.status === 'Succeeded'
  const skippedCount = run.rewards.inventoryFullSkippedPlayerIds?.length ?? 0
  const rewardRows = [
    {
      label: locale.rewardExpLabel,
      value: `${run.rewards.exp}`,
    },
    {
      label: locale.rewardGoldLabel,
      value: `${run.rewards.gold ?? 0}`,
    },
    {
      label: locale.rewardEquipmentLabel,
      value: run.rewards.equipmentRewardName ?? locale.rewardNone,
    },
    {
      label: locale.rewardItemLabel,
      value: run.rewards.itemRewardName ?? locale.rewardNone,
    },
  ]

  return (
    // 白い箱（GamePanel）の中に端まで広げて置くので、外側の枠と角丸は付けない
    <Box
      sx={{
        flex: 1,
        overflow: 'hidden',
        background: isSucceeded
          ? `linear-gradient(180deg, ${palette.cream} 0%, ${palette.cream} 48%, ${palette.brown200} 100%)`
          : `linear-gradient(180deg, ${palette.white} 0%, ${palette.cream} 52%, ${palette.gray300} 100%)`,
      }}
    >
      <Stack spacing={2} sx={{ p: { xs: 1.5, sm: 3 } }}>
        <Box>
          <Typography
            variant="h4"
            sx={{
              fontWeight: 800,
              letterSpacing: '0.08em',
              textAlign: 'center',
              color: isSucceeded ? palette.gold700 : palette.red700,
              textShadow: `0 2px 0 ${withAlpha(palette.white, 0.5)}`,
              fontSize: { xs: font.xxl, sm: font.display },
            }}
          >
            {isSucceeded ? locale.clearTitle : locale.failedTitle}
          </Typography>
          <Typography variant="body2" sx={{ mt: 0.75, textAlign: 'center', color: palette.brown900 }}>
            {isSucceeded ? locale.clearSubtitle : locale.failedSubtitle}
          </Typography>
        </Box>

        {isEndless ? (
          <Stack direction="row" spacing={2} useFlexGap flexWrap="wrap" justifyContent="center">
            <Stack alignItems="center" spacing={0.25}>
              <Typography variant="caption" sx={{ fontWeight: 700, color: palette.brown500 }}>
                {questRoomLocale.endless.reachedFloorLabel}
              </Typography>
              <Typography variant="h4" sx={{ fontWeight: 900, color: palette.gold700, lineHeight: 1 }}>
                {reachedFloor}
              </Typography>
            </Stack>
            {endlessBestFloor != null ? (
              <Stack alignItems="center" spacing={0.25}>
                <Typography variant="caption" sx={{ fontWeight: 700, color: palette.brown500 }}>
                  {questRoomLocale.endless.bestFloorLabel}
                </Typography>
                <Typography variant="h4" sx={{ fontWeight: 900, color: palette.brown900, lineHeight: 1 }}>
                  {Math.max(endlessBestFloor, reachedFloor)}
                </Typography>
              </Stack>
            ) : null}
          </Stack>
        ) : null}

        <Stack direction="row" spacing={1.5} useFlexGap flexWrap="wrap" justifyContent="center">
          {partyMembers.map((member) => {
            const imageSrc = resolveCharacterAssetPath(member.imagePath)

            return (
              <Stack key={member.participantId} spacing={0.75} alignItems="center" sx={{ width: { xs: 112, sm: 124 } }}>
                <Box
                  sx={{
                    width: '100%',
                    height: { xs: 120, sm: 136 },
                    borderRadius: 2.5,
                    border: `1px solid ${withAlpha(palette.gold700, 0.25)}`,
                    background: isSucceeded
                      ? `linear-gradient(180deg, ${withAlpha(palette.white, 0.75)} 0%, ${withAlpha(palette.cream, 0.9)} 100%)`
                      : `linear-gradient(180deg, ${withAlpha(palette.white, 0.75)} 0%, ${withAlpha(palette.gray300, 0.9)} 100%)`,
                    boxShadow: `inset 0 1px 0 ${withAlpha(palette.white, 0.5)}`,
                    overflow: 'hidden',
                    display: 'flex',
                    alignItems: 'flex-end',
                    justifyContent: 'center',
                    opacity: isSucceeded ? 1 : member.isDead ? 0.55 : 0.82,
                    filter: isSucceeded ? 'none' : 'grayscale(0.2)',
                  }}
                >
                  {imageSrc ? (
                    <Box
                      component="img"
                      src={imageSrc}
                      alt={member.displayName}
                      sx={{
                        width: '100%',
                        height: '100%',
                        objectFit: 'contain',
                        objectPosition: 'center bottom',
                        display: 'block',
                      }}
                    />
                  ) : (
                    <Stack alignItems="center" justifyContent="center" sx={{ width: '100%', height: '100%', px: 1 }}>
                      <Typography variant="caption" color="text.secondary" textAlign="center">
                        {locale.noImage}
                      </Typography>
                    </Stack>
                  )}
                </Box>

                <Typography
                  variant="body2"
                  sx={{
                    maxWidth: '100%',
                    fontWeight: 700,
                    textAlign: 'center',
                    color: palette.brown900,
                    wordBreak: 'break-word',
                  }}
                >
                  {member.displayName}
                </Typography>
              </Stack>
            )
          })}
        </Stack>

        {isSucceeded || isEndless ? (
          <Paper
            variant="outlined"
            sx={{
              px: 2,
              py: 1.5,
              borderRadius: 2,
              borderColor: withAlpha(palette.gold700, 0.25),
              background: withAlpha(palette.white, 0.5),
            }}
          >
            <Stack spacing={0.75} alignItems="stretch">
              <Typography variant="subtitle1" fontWeight={800} color={palette.gold700}>
                {locale.rewardTitle}
              </Typography>
              {isMirror ? (
                <Typography variant="body2" fontWeight={700} color={palette.brown900} textAlign="center">
                  {questRoomLocale.mirror.noReward}
                </Typography>
              ) : null}
              {(isMirror ? [] : rewardRows).map((reward) => (
                <Stack
                  key={reward.label}
                  direction="row"
                  spacing={1}
                  justifyContent="space-between"
                  alignItems="center"
                  sx={{
                    py: 0.25,
                    borderBottom: `1px solid ${withAlpha(palette.gold700, 0.1)}`,
                    '&:last-of-type': {
                      borderBottom: 'none',
                    },
                  }}
                >
                  <Typography variant="body2" fontWeight={700} color={palette.gold700}>
                    {reward.label}
                  </Typography>
                  <Typography variant="body1" fontWeight={700} color={palette.brown900} textAlign="right">
                    {reward.value}
                  </Typography>
                </Stack>
              ))}
              {skippedCount > 0 ? (
                <Typography variant="body2" color={palette.brown500} textAlign="center">
                  {locale.rewardInventoryFullSkipped.replace('{{count}}', String(skippedCount))}
                </Typography>
              ) : null}
            </Stack>
          </Paper>
        ) : null}

        <Box sx={{ display: 'flex', justifyContent: 'center', pt: 0.5 }}>
          <Button variant="contained" onClick={onLeaveFinishedRun} sx={{ minWidth: 156, ...softGreenButtonSx }}>
            {locale.leaveFinishedRun}
          </Button>
        </Box>
      </Stack>
    </Box>
  )
}
