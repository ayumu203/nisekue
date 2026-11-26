import { Box } from '@mui/material'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'
import TrainingSelectCard from './TrainingSelectCard'
import { trainingCardGridSx } from './trainingSelectStyles'

type TrainingEnemySelectProps = {
  enemies: TrainingEnemy[]
  selectedEnemyId: number | null
  isSelectionDisabled: boolean
  tutorialHighlightFirstCard?: boolean
  compact?: boolean
  onSelect: (enemy: TrainingEnemy) => void
}

export default function TrainingEnemySelect({
  enemies,
  selectedEnemyId,
  isSelectionDisabled,
  tutorialHighlightFirstCard = false,
  compact = false,
  onSelect,
}: TrainingEnemySelectProps) {
  return (
    <Box role="radiogroup" aria-label={locale.enemySelectTitle} sx={trainingCardGridSx(compact)}>
      {enemies.map((enemy, index) => (
        <TrainingSelectCard
          key={enemy.id}
          id={tutorialHighlightFirstCard && index === 0 ? 'tutorial-fight-card' : undefined}
          radioName="training-enemy"
          name={enemy.name}
          imageSrc={resolvePublicAssetPath(enemy.imagePath)}
          levelLabel={locale.enemyLevel.replace('{{level}}', String(enemy.level))}
          backgroundKey={enemy.id}
          isSelected={enemy.id === selectedEnemyId}
          isDisabled={isSelectionDisabled}
          compact={compact}
          onSelect={() => onSelect(enemy)}
        />
      ))}
    </Box>
  )
}
