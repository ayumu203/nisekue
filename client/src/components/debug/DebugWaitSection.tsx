import { Button, Stack, Typography } from '@mui/material'
import { completeDebugTreasureMap, resetDebugCooldowns } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { softGoldButtonSx, softGreenButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import locale from '../../../locale/debug/Debug.json'

type DebugWaitSectionProps = {
  accessToken: string
}

export default function DebugWaitSection({ accessToken }: DebugWaitSectionProps) {
  const { isRunning, message, run } = useDebugAction()

  return (
    <DebugSection title={locale.wait.title} description={locale.wait.description} message={message}>
      <Stack spacing={0.5}>
        <Typography variant="body2">{locale.wait.resetCooldownsHelp}</Typography>
        <Stack direction="row">
          <Button
            variant="contained"
            sx={softGreenButtonSx}
            onClick={() => void run(() => resetDebugCooldowns(accessToken), locale.wait.cooldownsReset)}
            disabled={isRunning}
          >
            {locale.wait.resetCooldowns}
          </Button>
        </Stack>
      </Stack>
      <Stack spacing={0.5}>
        <Typography variant="body2">{locale.wait.completeTreasureMapHelp}</Typography>
        <Stack direction="row">
          <Button
            variant="contained"
            sx={softGoldButtonSx}
            onClick={() => void run(() => completeDebugTreasureMap(accessToken), locale.wait.treasureMapCompleted)}
            disabled={isRunning}
          >
            {locale.wait.completeTreasureMap}
          </Button>
        </Stack>
      </Stack>
    </DebugSection>
  )
}
