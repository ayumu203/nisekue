import { Button, Stack } from '@mui/material'
import { rebirthDebugPlayer } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { softGoldButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import type { DebugRebirthResponse } from '@/schema/debug'
import locale from '../../../locale/debug/Debug.json'

type DebugRebirthSectionProps = {
  accessToken: string
  onChanged: () => Promise<void>
}

export default function DebugRebirthSection({ accessToken, onChanged }: DebugRebirthSectionProps) {
  const { isRunning, message, run } = useDebugAction()

  async function handleRebirth(): Promise<void> {
    if (!window.confirm(locale.rebirth.confirm)) {
      return
    }

    let result: DebugRebirthResponse | null = null
    await run(
      async () => {
        result = await rebirthDebugPlayer(accessToken)
        await onChanged()
      },
      () => (result ? locale.rebirth.done.replace('{count}', String(result.rebirthCount)) : ''),
    )
  }

  return (
    <DebugSection title={locale.rebirth.title} description={locale.rebirth.description} message={message}>
      <Stack direction="row">
        <Button variant="contained" sx={softGoldButtonSx} onClick={() => void handleRebirth()} disabled={isRunning}>
          {locale.rebirth.submit}
        </Button>
      </Stack>
    </DebugSection>
  )
}
