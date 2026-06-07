import {
  Box,
  Button,
  Dialog,
  DialogContent,
  Stack,
  Typography,
} from '@mui/material'
import { softGoldButtonSx, mutedRedButtonSx } from '@/constants/styles'
import locale from '../../../locale/player-job/JobChange.json'

interface UnlockRoadmapDialogProps {
  open: boolean
  jobName: string
  goldCost: number
  remainingGold: number
  onClose: () => void
  onConfirm: () => void
}

export default function UnlockRoadmapDialog({
  open,
  jobName,
  goldCost,
  remainingGold,
  onClose,
  onConfirm,
}: UnlockRoadmapDialogProps) {
  return (
    <Dialog
      open={open}
      onClose={onClose}
      maxWidth="xs"
      fullWidth
      PaperProps={{
        sx: {
          border: '3px solid #8f6b2f',
          borderRadius: 2,
          bgcolor: '#fff7e8',
          boxShadow: '0 4px 24px rgba(80, 50, 0, 0.25)',
        },
      }}
    >
      <Box
        sx={{
          px: 2.5,
          py: 1.25,
          borderBottom: '2px solid #d2c08b',
          background: 'linear-gradient(90deg, #e8c84a 0%, #f2d27a 100%)',
        }}
      >
        <Typography variant="subtitle1" fontWeight={900} color="#4a2e0a">
          ★ {locale.roadmapUnlockDialogTitle}
        </Typography>
      </Box>

      <DialogContent sx={{ px: 2.5, py: 2 }}>
        <Stack spacing={1.5}>
          <Stack spacing={0.5}>
            <Typography variant="body2" color="#4f4638">
              {locale.roadmapUnlockDialogJob.replace('{{jobName}}', jobName)}
            </Typography>
            <Box
              sx={{
                display: 'inline-flex',
                alignItems: 'center',
                gap: 0.5,
                px: 1.5,
                py: 0.75,
                bgcolor: '#f5e8b0',
                border: '2px solid #c8a84a',
                borderRadius: 1,
                width: 'fit-content',
              }}
            >
              <Typography variant="body2" fontWeight={900} color="#6a4b1a">
                {locale.roadmapUnlockDialogCost.replace('{{gold}}', goldCost.toLocaleString())}
              </Typography>
            </Box>
          </Stack>

          <Typography variant="caption" color="#8a7a5a">
            {locale.roadmapUnlockDialogRemaining.replace('{{remaining}}', remainingGold.toLocaleString())}
          </Typography>

          <Stack direction="row" spacing={1} justifyContent="flex-end" sx={{ pt: 0.5 }}>
            <Button
              onClick={onClose}
              variant="outlined"
              size="small"
              sx={mutedRedButtonSx}
            >
              {locale.roadmapUnlockDialogCancel}
            </Button>
            <Button
              onClick={onConfirm}
              variant="contained"
              size="small"
              sx={softGoldButtonSx}
            >
              {locale.roadmapUnlockDialogConfirm.replace('{{gold}}', goldCost.toLocaleString())}
            </Button>
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  )
}
