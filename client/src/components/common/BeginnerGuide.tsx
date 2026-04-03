import HelpOutlineIcon from '@mui/icons-material/HelpOutline'
import { Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, Typography } from '@mui/material'
import { useEffect, useState, type SxProps, type Theme } from 'react'
import {
  beginnerGuideBadge,
  beginnerGuideCloseLabel,
  beginnerGuideOpenLabel,
  type BeginnerGuideDefinition,
} from '@/lib/beginnerGuides'
import { hasSeenBeginnerGuide, markBeginnerGuideSeen } from '@/lib/beginnerGuideStorage'

type BeginnerGuideProps = {
  userId?: string | null
  guide: BeginnerGuideDefinition
  inverted?: boolean
  triggerSx?: SxProps<Theme>
}

export default function BeginnerGuide({ userId, guide, inverted = false, triggerSx }: BeginnerGuideProps) {
  const [open, setOpen] = useState(false)

  useEffect(() => {
    if (!userId || hasSeenBeginnerGuide(userId, guide.pageKey)) {
      return
    }

    setOpen(true)
  }, [guide.pageKey, userId])

  function handleClose(): void {
    if (userId) {
      markBeginnerGuideSeen(userId, guide.pageKey)
    }

    setOpen(false)
  }

  return (
    <>
      <Button
        variant={inverted ? 'outlined' : 'text'}
        size="small"
        startIcon={<HelpOutlineIcon fontSize="small" />}
        onClick={() => setOpen(true)}
        sx={{
          alignSelf: 'flex-start',
          borderRadius: 999,
          px: 1.5,
          color: inverted ? '#ffffff' : '#5a4d32',
          borderColor: inverted ? 'rgba(255,255,255,0.4)' : 'rgba(90,77,50,0.22)',
          backgroundColor: inverted ? 'rgba(255,255,255,0.08)' : 'rgba(255,255,255,0.28)',
          '&:hover': {
            borderColor: inverted ? 'rgba(255,255,255,0.56)' : 'rgba(90,77,50,0.32)',
            backgroundColor: inverted ? 'rgba(255,255,255,0.14)' : 'rgba(255,255,255,0.45)',
          },
          ...triggerSx,
        }}
      >
        {beginnerGuideOpenLabel}
      </Button>

      <Dialog open={open} onClose={handleClose} fullWidth maxWidth="sm">
        <DialogTitle>
          <Stack spacing={0.5}>
            <Typography variant="overline" sx={{ letterSpacing: '0.14em', color: 'text.secondary', lineHeight: 1.2 }}>
              {beginnerGuideBadge}
            </Typography>
            <Typography variant="h6" fontWeight={800} component="span">
              {guide.title}
            </Typography>
          </Stack>
        </DialogTitle>
        <DialogContent dividers>
          <Stack spacing={2}>
            <Typography variant="body2" color="text.secondary">
              {guide.description}
            </Typography>
            {guide.sections.map((section) => (
              <Stack key={section.title} spacing={0.5}>
                <Typography variant="subtitle2" fontWeight={800}>
                  {section.title}
                </Typography>
                <Typography variant="body2">{section.body}</Typography>
              </Stack>
            ))}
          </Stack>
        </DialogContent>
        <DialogActions>
          <Button onClick={handleClose}>{beginnerGuideCloseLabel}</Button>
        </DialogActions>
      </Dialog>
    </>
  )
}
