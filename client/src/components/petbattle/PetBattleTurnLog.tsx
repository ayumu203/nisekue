import { Paper, Stack, Typography } from '@mui/material'
import { cyberColors, cyberPanelSx } from '@/components/petbattle/petBattleStyles'
import locale from '../../../locale/pet/PetBattle.json'
import type { PetBattleLastTurnResults } from '@/schema/petBattle'

type PetBattleTurnLogProps = {
  results: PetBattleLastTurnResults
}

function getActionLabel(actionKind: string, moveName: string | null | undefined): string {
  if (moveName) {
    return moveName
  }

  const actionKinds: Record<string, string> = locale.actionKinds
  return actionKinds[actionKind] ?? actionKind
}

export default function PetBattleTurnLog({ results }: PetBattleTurnLogProps) {
  return (
    <Paper variant="outlined" sx={{ ...cyberPanelSx, p: { xs: 1.5, sm: 2 } }}>
      <Stack spacing={1}>
        <Typography variant="subtitle2" sx={{ color: cyberColors.accent, fontWeight: 800 }}>
          {locale.turnLogTitle}（{locale.logTurnPrefix} {results.turnNo}）
        </Typography>

        <Stack spacing={0.75}>
          {results.actions.map((action, actionIndex) => (
            <Stack key={`${action.actorParticipantId}-${actionIndex}`} spacing={0.2}>
              <Typography variant="body2" sx={{ color: cyberColors.text, fontWeight: 700 }}>
                ▶ {action.actorDisplayName} の {getActionLabel(action.actionKind, action.moveName)}
                {action.succeeded ? '' : locale.log.failedSuffix}
              </Typography>

              {action.targetSummaries.map((target, targetIndex) => {
                const lines: string[] = []

                if (target.resultType === 'Miss') {
                  lines.push(`${target.targetDisplayName}${locale.log.missText}`)
                } else {
                  if (target.hpChange < 0) {
                    lines.push(`${target.targetDisplayName}に ${-target.hpChange} ${locale.log.damageSuffix}`)
                  } else if (target.hpChange > 0) {
                    lines.push(`${target.targetDisplayName}のHPが ${target.hpChange} ${locale.log.healSuffix}`)
                  }

                  if (target.appliedEffects.length > 0) {
                    lines.push(
                      `${target.targetDisplayName}は ${target.appliedEffects.join('、')} ${locale.log.effectSuffix}`,
                    )
                  }
                }

                if (target.isDeadAfterAction) {
                  lines.push(`${target.targetDisplayName}${locale.log.defeatedText}`)
                }

                return lines.map((line, lineIndex) => (
                  <Typography
                    key={`${target.targetParticipantId}-${targetIndex}-${lineIndex}`}
                    variant="caption"
                    sx={{ color: cyberColors.textDim, pl: 2 }}
                  >
                    {line}
                  </Typography>
                ))
              })}
            </Stack>
          ))}
        </Stack>
      </Stack>
    </Paper>
  )
}
