import {
  Box,
  Button,
  Dialog,
  DialogContent,
  Stack,
  Typography,
} from '@mui/material'
import { softGoldButtonSx, mutedRedButtonSx } from '@/constants/styles'
import { resolveJobAssetPath } from '@/lib/assets'
import type { PlayerJobCode } from '@/schema/player'
import locale from '../../../locale/player-job/JobChange.json'

interface UnlockRoadmapDialogProps {
  open: boolean
  jobName: string
  jobCode?: PlayerJobCode
  goldCost: number
  onClose: () => void
  onConfirm: () => void
}

export default function UnlockRoadmapDialog({
  open,
  jobName,
  jobCode,
  goldCost,
  onClose,
  onConfirm,
}: UnlockRoadmapDialogProps) {
  const imageSrc = jobCode ? resolveJobAssetPath(jobCode) : null

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
          overflow: 'hidden',
        },
      }}
    >
      <Box
        sx={{
          px: 2.5,
          py: 1.25,
          background: 'linear-gradient(90deg, #e8c84a 0%, #f2d27a 100%)',
          borderBottom: '2px solid #c8a030',
        }}
      >
        <Typography variant="subtitle1" fontWeight={900} color="#4a2e0a">
          ★ {locale.roadmapUnlockDialogTitle}
        </Typography>
      </Box>

      <DialogContent sx={{ px: 2.5, pt: 2.5, pb: 2 }}>
        <Stack spacing={2}>
          <Stack direction="row" spacing={2} alignItems="center">
            <Box
              sx={{
                width: 64,
                height: 64,
                flexShrink: 0,
                borderRadius: 2,
                border: '2px solid #c8a84a',
                bgcolor: '#fdf4dc',
                display: 'grid',
                placeItems: 'center',
                p: 0.75,
              }}
            >
              {imageSrc ? (
                <Box
                  component="img"
                  src={imageSrc}
                  alt={jobName}
                  sx={{ width: '100%', height: '100%', objectFit: 'contain' }}
                />
              ) : (
                <Typography variant="h5" fontWeight={900} color="#8a6a3a">
                  {jobName.charAt(0)}
                </Typography>
              )}
            </Box>
            <Stack spacing={0.25}>
              <Typography variant="caption" color="#8a7a5a">
                {locale.roadmapUnlockDialogJob.replace('{{jobName}}', '')}
              </Typography>
              <Typography variant="h6" fontWeight={900} color="#4a2e0a" lineHeight={1.2}>
                {jobName}
              </Typography>
            </Stack>
          </Stack>

          <Box
            sx={{
              px: 2,
              py: 1.25,
              bgcolor: '#f5e8b0',
              border: '2px solid #c8a84a',
              borderRadius: 1.5,
              textAlign: 'center',
            }}
          >
            <Typography variant="body2" color="#8a7a5a" sx={{ mb: 0.25 }}>
              {locale.roadmapUnlockDialogCost.replace('{{gold}}', '')}
            </Typography>
            <Typography variant="h5" fontWeight={900} color="#6a4b1a">
              {goldCost.toLocaleString()} Gold
            </Typography>
          </Box>

          <Stack direction="row" spacing={1}>
            <Button
              onClick={onClose}
              variant="outlined"
              fullWidth
              sx={mutedRedButtonSx}
            >
              {locale.roadmapUnlockDialogCancel}
            </Button>
            <Button
              onClick={onConfirm}
              variant="contained"
              fullWidth
              sx={softGoldButtonSx}
            >
              {locale.roadmapUnlockDialogConfirm}
            </Button>
          </Stack>
        </Stack>
      </DialogContent>
    </Dialog>
  )
}
