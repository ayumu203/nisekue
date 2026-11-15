import ExpandMoreIcon from '@mui/icons-material/ExpandMore'
import { IconButton } from '@mui/material'
import { deepGreen } from './ItemsConstants'
import locale from '../../../locale/items/Items.json'

// 訓練の戦闘ログと同じ、開くと 180° 回転するおりかえしアイコン。
export function DetailsToggleButton({
  isOpen,
  onToggle,
  controlsId,
}: {
  isOpen: boolean
  onToggle: () => void
  controlsId?: string
}) {
  return (
    <IconButton
      size="small"
      onClick={onToggle}
      aria-label={isOpen ? locale.hideDetails : locale.showDetails}
      aria-expanded={isOpen}
      aria-controls={controlsId}
      sx={{ alignSelf: 'center', color: deepGreen }}
    >
      <ExpandMoreIcon
        fontSize="small"
        sx={{ transform: isOpen ? 'rotate(180deg)' : 'none', transition: 'transform 150ms ease' }}
      />
    </IconButton>
  )
}
