import {
  Box,
  Button,
  Dialog,
  DialogContent,
  Stack,
  Typography,
} from '@mui/material'
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
          bgcolor: '#000',
          border: '3px solid #fff',
          borderRadius: 0,
          boxShadow: '4px 4px 0 #888, inset 0 0 0 1px #444',
          overflow: 'hidden',
        },
      }}
    >
      <DialogContent sx={{ p: 0 }}>
        <Stack spacing={0}>
          {/* ジョブ情報 */}
          <Box sx={{ px: 3, pt: 3, pb: 2, borderBottom: '1px solid #333', textAlign: 'center' }}>
            <Box
              sx={{
                width: 56,
                height: 56,
                border: '2px solid #fff',
                display: 'grid',
                placeItems: 'center',
                bgcolor: '#111',
                mx: 'auto',
                mb: 1.5,
              }}
            >
              {imageSrc ? (
                <Box
                  component="img"
                  src={imageSrc}
                  alt={jobName}
                  sx={{ width: 44, height: 44, objectFit: 'contain', imageRendering: 'pixelated' }}
                />
              ) : (
                <Typography variant="h5" fontWeight={900} color="#fff">
                  {jobName.charAt(0)}
                </Typography>
              )}
            </Box>
            <Typography sx={{ fontSize: '0.65rem', color: '#888', letterSpacing: '0.1em' }}>
              {locale.roadmapUnlockDialogJob}
            </Typography>
            <Typography
              fontWeight={900}
              color="#fff"
              lineHeight={1.2}
              sx={{ fontSize: '1.1rem', mt: 0.25 }}
            >
              {jobName}
            </Typography>
          </Box>

          {/* 質問テキスト */}
          <Box sx={{ px: 3, py: 2.5, textAlign: 'center' }}>
            <Typography
              color="#fff"
              fontWeight={700}
              sx={{ fontSize: '0.95rem', lineHeight: 1.7 }}
            >
              {locale.roadmapUnlockDialogTitle}
            </Typography>
            <Stack direction="row" alignItems="baseline" spacing={1} justifyContent="center" sx={{ mt: 1.5 }}>
              <Typography sx={{ fontSize: '0.7rem', color: '#888', letterSpacing: '0.08em' }}>
                {locale.roadmapUnlockDialogCost}
              </Typography>
              <Typography fontWeight={900} color="#fff" sx={{ fontSize: '1.2rem' }}>
                {goldCost.toLocaleString()}
              </Typography>
              <Typography fontWeight={700} color="#aaa" sx={{ fontSize: '0.85rem' }}>
                G
              </Typography>
            </Stack>
          </Box>

          {/* ボタン */}
          <Box sx={{ borderTop: '1px solid #333', px: 3, py: 2 }}>
            <Stack direction="row" spacing={2} justifyContent="center">
              <Button
                onClick={onClose}
                sx={{
                  '&&': {
                    minWidth: 80,
                    border: '2px solid #666',
                    borderRadius: 0,
                    color: '#aaa',
                    bgcolor: '#000',
                    fontWeight: 900,
                    fontSize: '0.9rem',
                    py: 0.75,
                    letterSpacing: '0.05em',
                    boxShadow: 'none',
                  },
                  '&&:hover': {
                    border: '2px solid #aaa',
                    color: '#fff',
                    bgcolor: '#1a1a1a',
                    boxShadow: 'none',
                  },
                }}
              >
                {locale.roadmapUnlockDialogCancel}
              </Button>
              <Button
                onClick={onConfirm}
                sx={{
                  '&&': {
                    minWidth: 80,
                    border: '2px solid #fff',
                    borderRadius: 0,
                    color: '#000',
                    bgcolor: '#fff',
                    fontWeight: 900,
                    fontSize: '0.9rem',
                    py: 0.75,
                    letterSpacing: '0.05em',
                    boxShadow: 'none',
                  },
                  '&&:hover': {
                    bgcolor: '#ddd',
                    border: '2px solid #ddd',
                    boxShadow: 'none',
                  },
                }}
              >
                {locale.roadmapUnlockDialogConfirm}
              </Button>
            </Stack>
          </Box>
        </Stack>
      </DialogContent>
    </Dialog>
  )
}
