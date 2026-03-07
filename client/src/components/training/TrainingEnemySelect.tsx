import { Button, Card, CardContent, CardMedia, Grid, Stack, Typography } from '@mui/material'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { TrainingEnemy } from '@/schema/training'
import locale from '../../../locale/training/Training.json'

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
          <Grid key={enemy.id} size={{ xs: 12, sm: 6 }}>
            <Card variant="outlined" sx={{ height: '100%' }}>
              <CardMedia
                component="img"
                height="160"
                image={resolvePublicAssetPath(enemy.imagePath)}
                alt={enemy.name}
                sx={{
                  objectFit: 'contain',
                  backgroundColor: 'action.hover',
                  p: 1,
                }}
              />
              <CardContent>
                <Stack spacing={1.5}>
                  <Stack direction="row" justifyContent="space-between" alignItems="center">
                    <Typography variant="subtitle1" fontWeight={700}>
                      {enemy.name}
                    </Typography>
                    <Typography variant="body2" color="text.secondary">
                      {locale.enemyLevel.replace('{{level}}', String(enemy.level))}
                    </Typography>
                  </Stack>
                  <Button variant="contained" disabled={isActionDisabled} onClick={() => onFight(enemy)}>
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
