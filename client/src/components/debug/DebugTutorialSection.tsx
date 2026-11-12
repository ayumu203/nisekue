import { Button, Stack, Typography } from '@mui/material'
import { useState } from 'react'
import DebugSection from '@/components/debug/DebugSection'
import { completeTutorial, getTutorialStep, resetTutorial } from '@/lib/tutorial'
import locale from '../../../locale/debug/Debug.json'

type DebugTutorialSectionProps = {
  userId: string
}

export default function DebugTutorialSection({ userId }: DebugTutorialSectionProps) {
  const [step, setStep] = useState(() => getTutorialStep(userId))
  const [message, setMessage] = useState<{ severity: 'success'; text: string } | null>(null)

  function handleComplete(): void {
    completeTutorial(userId)
    setStep(getTutorialStep(userId))
    setMessage({ severity: 'success', text: locale.tutorial.completed })
  }

  function handleReset(): void {
    resetTutorial(userId)
    setStep(getTutorialStep(userId))
    setMessage({ severity: 'success', text: locale.tutorial.reset })
  }

  return (
    <DebugSection title={locale.tutorial.title} description={locale.tutorial.description} message={message}>
      <Typography variant="body2">
        {step === 'completed'
          ? locale.tutorial.stateCompleted
          : locale.tutorial.stateInProgress.replace('{step}', step)}
      </Typography>
      <Stack direction="row" spacing={1} flexWrap="wrap" useFlexGap>
        <Button variant="contained" onClick={handleComplete} disabled={step === 'completed'}>
          {locale.tutorial.complete}
        </Button>
        <Button variant="outlined" onClick={handleReset}>
          {locale.tutorial.restart}
        </Button>
      </Stack>
    </DebugSection>
  )
}
