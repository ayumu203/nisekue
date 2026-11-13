import { Box, Card, CardContent, CardMedia, Chip, Grid, Radio, Stack, Typography } from '@mui/material'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'
import { trainingSelectCardSx, trainingSelectRadioSx } from './trainingSelectStyles'

const trainingEnemyBackgroundPaths = [
  'image/quest/ancient-ruins-battlefield.svg',
  'image/quest/cave-battlefield.svg',
  'image/quest/crystal-cavern-battlefield.svg',
  'image/quest/enchanted-forest-battlefield.svg',
  'image/quest/floating-islands-battlefield.svg',
  'image/quest/hell-battlefield.svg',
  'image/quest/moonlit-castle-battlefield.svg',
  'image/quest/residential-battlefield.svg',
  'image/quest/riverbank-battlefield.svg',
] as const

function getTrainingEnemyBackgroundPath(enemyId: number): string {
  return resolvePublicAssetPath(trainingEnemyBackgroundPaths[Math.abs(enemyId) % trainingEnemyBackgroundPaths.length])
}

type TrainingEnemySelectProps = {
  enemies: TrainingEnemy[]
  selectedEnemyId: number | null
  isSelectionDisabled: boolean
  tutorialHighlightFirstCard?: boolean
  onSelect: (enemy: TrainingEnemy) => void
}

export default function TrainingEnemySelect({
  enemies,
  selectedEnemyId,
  isSelectionDisabled,
  tutorialHighlightFirstCard = false,
  onSelect,
}: TrainingEnemySelectProps) {
  return (
    <Grid container spacing={2} role="radiogroup" aria-label={locale.enemySelectTitle}>
      {enemies.map((enemy, index) => {
        const isSelected = enemy.id === selectedEnemyId

        return (
          <Grid key={enemy.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card
              component="label"
              id={tutorialHighlightFirstCard && index === 0 ? 'tutorial-fight-card' : undefined}
              elevation={0}
              sx={trainingSelectCardSx(isSelected, isSelectionDisabled)}
            >
              <Box sx={{ position: 'relative' }}>
                <Radio
                  name="training-enemy"
                  checked={isSelected}
                  disabled={isSelectionDisabled}
                  onChange={() => onSelect(enemy)}
                  inputProps={{ 'aria-label': enemy.name }}
                  size="small"
                  sx={trainingSelectRadioSx}
                />
                <CardMedia
                  component="img"
                  height="160"
                  image={resolvePublicAssetPath(enemy.imagePath)}
                  alt={enemy.name}
                  sx={{
                    objectFit: 'contain',
                    backgroundColor: '#1e1213',
                    backgroundImage: `linear-gradient(rgba(34, 18, 19, 0.36), rgba(34, 18, 19, 0.58)), url(${getTrainingEnemyBackgroundPath(enemy.id)})`,
                    backgroundSize: 'cover',
                    backgroundPosition: 'center',
                    p: 1.25,
                    borderBottom: '1px solid rgba(214, 146, 112, 0.22)',
                  }}
                />
              </Box>
              <CardContent sx={{ '&:last-child': { pb: 2 } }}>
                <Stack spacing={0.9} alignItems="center">
                  <Chip
                    label={locale.enemyLevel.replace('{{level}}', String(enemy.level))}
                    size="small"
                    sx={{
                      fontWeight: 800,
                      color: '#ffe9dc',
                      backgroundColor: 'rgba(214, 146, 112, 0.18)',
                      border: '1px solid rgba(214, 146, 112, 0.35)',
                    }}
                  />
                  <Typography
                    variant="subtitle1"
                    fontWeight={700}
                    textAlign="center"
                    sx={{
                      color: '#fff7dd',
                      fontSize: { xs: '1.02rem', sm: '0.98rem', md: '1.02rem' },
                      lineHeight: 1.35,
                    }}
                  >
                    {enemy.name}
                  </Typography>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        )
      })}
    </Grid>
  )
}
