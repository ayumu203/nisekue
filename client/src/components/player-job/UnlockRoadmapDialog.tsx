import {
  Button,
  Dialog,
  DialogActions,
  DialogContent,
  DialogTitle,
  Typography,
} from '@mui/material'
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
    <Dialog open={open} onClose={onClose} maxWidth="xs" fullWidth>
      <DialogTitle>{locale.roadmapUnlockDialogTitle}</DialogTitle>
      <DialogContent>
        <Typography variant="body1" gutterBottom>
          {locale.roadmapUnlockDialogJob.replace('{{jobName}}', jobName)}
        </Typography>
        <Typography variant="body1" gutterBottom>
          {locale.roadmapUnlockDialogCost.replace('{{gold}}', goldCost.toLocaleString())}
        </Typography>
        <Typography variant="body2" color="text.secondary">
          {locale.roadmapUnlockDialogRemaining.replace('{{remaining}}', remainingGold.toLocaleString())}
        </Typography>
      </DialogContent>
      <DialogActions>
        <Button onClick={onClose}>{locale.roadmapUnlockDialogCancel}</Button>
        <Button
          onClick={onConfirm}
          variant="contained"
          sx={{
            '&&': {
              backgroundColor: '#4a6c51',
              color: '#fff8e8',
            },
            '&&:hover': {
              backgroundColor: '#3f5f46',
            },
          }}
        >
          {locale.roadmapUnlockDialogConfirm.replace('{{gold}}', goldCost.toLocaleString())}
        </Button>
      </DialogActions>
    </Dialog>
  )
}
