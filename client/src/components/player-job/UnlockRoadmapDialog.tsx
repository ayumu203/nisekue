import { Box, Button, Dialog, DialogContent, Stack, Typography } from '@mui/material'
import { resolveJobAssetPath } from '@/lib/assets'
import type { PlayerJobCode } from '@/schema/player'
import locale from '../../../locale/player-job/JobChange.json'
import { font, palette } from '@/theme/tokens'

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
          bgcolor: palette.navy,
          border: `3px solid ${palette.white}`,
          borderRadius: 0,
          boxShadow: `4px 4px 0 ${palette.gray600}, inset 0 0 0 1px ${palette.brown900}`,
          overflow: 'hidden',
        },
      }}
    >
      <DialogContent sx={{ p: 0 }}>
        <Stack spacing={0}>
          {/* ジョブ情報 */}
          <Box sx={{ px: 3, pt: 3, pb: 2, borderBottom: `1px solid ${palette.brown900}`, textAlign: 'center' }}>
            <Box
              sx={{
                width: 56,
                height: 56,
                border: `2px solid ${palette.white}`,
                display: 'grid',
                placeItems: 'center',
                bgcolor: palette.brown900,
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
                <Typography variant="h5" fontWeight={900} color={palette.white}>
                  {jobName.charAt(0)}
                </Typography>
              )}
            </Box>
            <Typography sx={{ fontSize: font.xxs, color: palette.gray600, letterSpacing: '0.1em' }}>
              {locale.roadmapUnlockDialogJob}
            </Typography>
            <Typography fontWeight={900} color={palette.white} lineHeight={1.2} sx={{ fontSize: font.lg, mt: 0.25 }}>
              {jobName}
            </Typography>
          </Box>

          {/* 質問テキスト */}
          <Box sx={{ px: 3, py: 2.5, textAlign: 'center' }}>
            <Typography color={palette.white} fontWeight={700} sx={{ fontSize: font.md, lineHeight: 1.7 }}>
              {locale.roadmapUnlockDialogTitle}
            </Typography>
            <Stack direction="row" alignItems="baseline" spacing={1} justifyContent="center" sx={{ mt: 1.5 }}>
              <Typography sx={{ fontSize: font.xs, color: palette.gray600, letterSpacing: '0.08em' }}>
                {locale.roadmapUnlockDialogCost}
              </Typography>
              <Typography fontWeight={900} color={palette.white} sx={{ fontSize: font.xl }}>
                {goldCost.toLocaleString()}
              </Typography>
              <Typography fontWeight={700} color={palette.gray300} sx={{ fontSize: font.sm }}>
                G
              </Typography>
            </Stack>
          </Box>

          {/* ボタン */}
          <Box sx={{ borderTop: `1px solid ${palette.brown900}`, px: 3, py: 2 }}>
            <Stack direction="row" spacing={2} justifyContent="center">
              <Button
                onClick={onClose}
                sx={{
                  '&&': {
                    minWidth: 80,
                    border: `2px solid ${palette.gray600}`,
                    borderRadius: 0,
                    color: palette.gray300,
                    bgcolor: palette.navy,
                    fontWeight: 900,
                    fontSize: font.sm,
                    py: 0.75,
                    letterSpacing: '0.05em',
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
                    border: `2px solid ${palette.white}`,
                    borderRadius: 0,
                    color: palette.navy,
                    bgcolor: palette.white,
                    fontWeight: 900,
                    fontSize: font.sm,
                    py: 0.75,
                    letterSpacing: '0.05em',
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
