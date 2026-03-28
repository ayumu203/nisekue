import { Button, Card, CardContent, CardMedia, Chip, Grid, Stack, Typography } from '@mui/material'
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
      <Grid container spacing={2}>
        {enemies.map((enemy) => (
          <Grid key={enemy.id} size={{ xs: 12, sm: 6, md: 4 }}>
            <Card
              variant="outlined"
              sx={{
                height: '100%',
                display: 'flex',
                flexDirection: 'column',
                color: '#f5f0df',
                backgroundColor: '#382526',
                borderColor: 'rgba(214, 146, 112, 0.42)',
                boxShadow: 'inset 0 1px 0 rgba(255, 227, 214, 0.08)',
              }}
            >
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
              <CardContent sx={{ display: 'flex', flexDirection: 'column', flexGrow: 1 }}>
                <Stack spacing={1.5} sx={{ height: '100%' }}>
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
                    sx={{
                      mt: 'auto',
                      borderRadius: 999,
                      border: 'none',
                      fontWeight: 800,
                      color: '#fff5ef',
                      backgroundColor: '#b65f49',
                      boxShadow: 'none',
                      '&:hover': {
                        backgroundColor: '#c96a52',
                        boxShadow: 'none',
                      },
                      '&.Mui-disabled': {
                        color: 'rgba(255, 238, 229, 0.58)',
                        backgroundColor: 'rgba(182, 95, 73, 0.24)',
                      },
                    }}
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
