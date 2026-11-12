import { Button, MenuItem, Stack, TextField } from '@mui/material'
import { useState } from 'react'
import useSWR from 'swr'
import { applyDebugTemplate, getDebugTemplates } from '@/api/debug'
import DebugSection from '@/components/debug/DebugSection'
import { greenOutlinedInputSx, softGreenButtonSx } from '@/constants/styles'
import { useDebugAction } from '@/hooks/useDebugAction'
import locale from '../../../locale/debug/Debug.json'

type DebugTemplateSectionProps = {
  accessToken: string
  onChanged: () => Promise<void>
}

export default function DebugTemplateSection({ accessToken, onChanged }: DebugTemplateSectionProps) {
  const { data: templates = [] } = useSWR(['debug-templates'], () => getDebugTemplates(accessToken), {
    revalidateOnFocus: false,
  })
  const [templateId, setTemplateId] = useState('')
  const { isRunning, message, run } = useDebugAction()
  const selected = templates.find((template) => template.templateId === templateId)

  async function handleApply(): Promise<void> {
    if (!selected || !window.confirm(locale.template.confirm)) {
      return
    }

    await run(
      async () => {
        await applyDebugTemplate({ templateId: selected.templateId }, accessToken)
        await onChanged()
      },
      locale.template.done.replace('{name}', selected.name),
    )
  }

  return (
    <DebugSection title={locale.template.title} description={locale.template.description} message={message}>
      <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1.5}>
        <TextField
          select
          label={locale.template.label}
          size="small"
          value={templateId}
          onChange={(event) => setTemplateId(event.target.value)}
          sx={{ ...greenOutlinedInputSx, minWidth: 240 }}
        >
          {templates.map((template) => (
            <MenuItem key={template.templateId} value={template.templateId}>
              {template.name}
            </MenuItem>
          ))}
        </TextField>
        <Button
          variant="contained"
          sx={softGreenButtonSx}
          onClick={() => void handleApply()}
          disabled={!selected || isRunning}
        >
          {locale.template.submit}
        </Button>
      </Stack>
    </DebugSection>
  )
}
