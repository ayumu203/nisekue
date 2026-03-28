import { Button, Card, CardContent, CardMedia, Grid, Stack, Typography } from '@mui/material'
import { innerSurfaceSx, softGreenButtonSx } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

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
  return resolvePublicAssetPath(
    trainingEnemyBackgroundPaths[Math.abs(enemyId) % trainingEnemyBackgroundPaths.length],
  )
}

type TrainingEnemySelectProps = {
  enemies: TrainingEnemy[]
  isActionDisabled: boolean
  lockRemainingSeconds: number
  onFight: (enemy: TrainingEnemy) => Promise<void> | void
}

export default function TrainingEnemySelect({
  enemies,
  isActionDisabled,
  lockRemainingSeconds,
  onFight,
}: TrainingEnemySelectProps) {
  const rematchInSeconds = locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))

  return (
    <Stack spacing={2}>
      <Typography variant="h5">{locale.enemySelectTitle}</Typography>
      <Grid container spacing={2}>
        {enemies.map((enemy) => (
          <Grid key={enemy.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card variant="outlined" sx={{ ...innerSurfaceSx, height: '100%', display: 'flex', flexDirection: 'column' }}>
              <CardMedia
                component="img"
                height="160"
                image={resolvePublicAssetPath(enemy.imagePath)}
                alt={enemy.name}
                sx={{
                  objectFit: 'contain',
                  backgroundColor: '#f6efe0',
                  backgroundImage: `linear-gradient(rgba(255, 250, 240, 0.7), rgba(255, 250, 240, 0.7)), url(${getTrainingEnemyBackgroundPath(enemy.id)})`,
                  backgroundSize: 'cover',
                  backgroundPosition: 'center',
                  p: 1,
                }}
              />
              <CardContent sx={{ display: 'flex', flexDirection: 'column', flexGrow: 1 }}>
                <Stack spacing={1.5} sx={{ height: '100%' }}>
                  <Stack spacing={0.25} alignItems="center">
                    <Typography variant="caption" color="text.secondary" fontWeight={700} textAlign="center">
                      {locale.enemyLevel.replace('{{level}}', String(enemy.level))}
                    </Typography>
                    <Typography
                      variant="subtitle1"
                      fontWeight={700}
                      textAlign="center"
                      sx={{
                        fontSize: { xs: '1rem', sm: '0.95rem', md: '1rem' },
                        lineHeight: 1.35,
                        minHeight: '2.7em',
                      }}
                    >
                      {enemy.name}
                    </Typography>
                  </Stack>
                  <Button
                    variant="contained"
                    disabled={isActionDisabled}
                    onClick={() => onFight(enemy)}
                    sx={{ ...softGreenButtonSx, mt: 'auto' }}
                  >
                    {isActionDisabled ? rematchInSeconds : locale.fight}
                  </Button>
                </Stack>
              </CardContent>
            </Card>
          </Grid>
        ))}
      </Grid>
    </Stack>
  )
}
