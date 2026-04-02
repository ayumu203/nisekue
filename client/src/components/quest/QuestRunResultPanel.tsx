import { Box, Button, Paper, Stack, Typography } from '@mui/material'
import { innerSurfaceSx } from '@/constants/styles'
import { softGreenButtonSx } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import type { BattleColumn, BattleRow, QuestRunDetailResponse } from '@/schema/quest'

type QuestRunResultPanelProps = {
  run: QuestRunDetailResponse
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

export default function QuestRunResultPanel({ run, onLeaveFinishedRun, locale }: QuestRunResultPanelProps) {
  if (run.status !== 'Succeeded' && run.status !== 'Failed') {
    return null
  }

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
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        overflow: 'hidden',
        borderRadius: 3,
        borderColor: isSucceeded ? '#d9c36f' : '#d8b3b0',
        background: isSucceeded
          ? 'linear-gradient(180deg, #fff6d6 0%, #fff1cf 48%, #ffe7b0 100%)'
          : 'linear-gradient(180deg, #fff3f1 0%, #fde5e1 52%, #f8d4ce 100%)',
      }}
    >
      <Stack spacing={2} sx={{ p: { xs: 1.5, sm: 2.75 } }}>
        <Box>
          <Typography
            variant="h4"
            sx={{
              fontWeight: 800,
              letterSpacing: '0.08em',
              textAlign: 'center',
              color: isSucceeded ? '#8b5a00' : '#9b2c1d',
              textShadow: '0 2px 0 rgba(255,255,255,0.45)',
              fontSize: { xs: '1.6rem', sm: '2.125rem' },
            }}
          >
            {isSucceeded ? locale.clearTitle : locale.failedTitle}
          </Typography>
          <Typography variant="body2" sx={{ mt: 0.75, textAlign: 'center', color: '#5b4b2d' }}>
            {isSucceeded ? locale.clearSubtitle : locale.failedSubtitle}
          </Typography>
        </Box>

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
                    border: '1px solid rgba(124, 91, 25, 0.2)',
                    background: isSucceeded
                      ? 'linear-gradient(180deg, rgba(255,255,255,0.7) 0%, rgba(255,241,196,0.86) 100%)'
                      : 'linear-gradient(180deg, rgba(255,255,255,0.72) 0%, rgba(246,219,214,0.88) 100%)',
                    boxShadow: 'inset 0 1px 0 rgba(255,255,255,0.45)',
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
                    color: '#433118',
                    wordBreak: 'break-word',
                  }}
                >
                  {member.displayName}
                </Typography>
              </Stack>
            )
          })}
        </Stack>

        {isSucceeded ? (
          <Paper
            variant="outlined"
            sx={{
              px: 2,
              py: 1.5,
              borderRadius: 2,
              borderColor: 'rgba(124, 91, 25, 0.28)',
              background: 'rgba(255,255,255,0.55)',
            }}
          >
            <Stack spacing={0.75} alignItems="stretch">
              <Typography variant="subtitle1" fontWeight={800} color="#6f4d11">
                {locale.rewardTitle}
              </Typography>
              {rewardRows.map((reward) => (
                <Stack
                  key={reward.label}
                  direction="row"
                  spacing={1}
                  justifyContent="space-between"
                  alignItems="center"
                  sx={{
                    py: 0.25,
                    borderBottom: '1px solid rgba(124, 91, 25, 0.12)',
                    '&:last-of-type': {
                      borderBottom: 'none',
                    },
                  }}
                >
                  <Typography variant="body2" fontWeight={700} color="#6f4d11">
                    {reward.label}
                  </Typography>
                  <Typography variant="body1" fontWeight={700} color="#4b391c" textAlign="right">
                    {reward.value}
                  </Typography>
                </Stack>
              ))}
              {skippedCount > 0 ? (
                <Typography variant="body2" color="#7b5e2f" textAlign="center">
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
    </Paper>
  )
}
