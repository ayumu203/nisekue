import {
  Avatar,
  Button,
  Card,
  CardContent,
  Chip,
  Grid,
  Pagination,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useRef, useState } from 'react'
import { resolveCharacterAssetPath } from '@/lib/assets'
import type { PlayerSummary } from '@/schema/player'
import locale from '../../../locale/training/Training.json'

type TrainingOpponentSelectProps = {
  opponents: PlayerSummary[]
  isActionDisabled: boolean
  lockRemainingSeconds: number
  onFight: (opponent: PlayerSummary) => Promise<void> | void
}

const PAGE_SIZE = 5

export default function TrainingOpponentSelect({
  opponents,
  isActionDisabled,
  lockRemainingSeconds,
  onFight,
}: TrainingOpponentSelectProps) {
  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const [page, setPage] = useState(1)
  const topRef = useRef<HTMLDivElement | null>(null)
  const rematchInSeconds = locale.rematchInSeconds.replace('{{seconds}}', String(lockRemainingSeconds))

  if (opponents.length === 0) {
    return (
      <Typography variant="body2" sx={{ color: 'rgba(248, 221, 207, 0.6)', textAlign: 'center', py: 2 }}>
        {locale.noOpponentsAvailable}
      </Typography>
    )
  }

  const pageCount = Math.ceil(opponents.length / PAGE_SIZE)
  const safePage = Math.min(Math.max(1, page), pageCount)
  const visible = isMobile ? opponents.slice((safePage - 1) * PAGE_SIZE, safePage * PAGE_SIZE) : opponents

  function handlePageChange(_: React.ChangeEvent<unknown>, nextPage: number) {
    setPage(nextPage)
    topRef.current?.scrollIntoView({ behavior: 'smooth', block: 'start' })
  }

  return (
    <Stack spacing={2} ref={topRef}>
      <Grid container spacing={2}>
        {visible.map((opponent) => (
          <Grid key={opponent.userId} size={{ xs: 12, sm: 6, md: 4 }}>
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
              <CardContent sx={{ display: 'flex', flexDirection: 'column', flexGrow: 1 }}>
                <Stack spacing={1.5} sx={{ height: '100%' }}>
                  <Stack spacing={0.9} alignItems="center">
                    <Avatar
                      src={resolveCharacterAssetPath(opponent.imagePath) ?? undefined}
                      alt={opponent.userName ?? locale.anonymousPlayer}
                      sx={{ width: 64, height: 64, bgcolor: '#5a2e30' }}
                    />
                    <Chip
                      label={locale.enemyLevel.replace('{{level}}', String(opponent.level))}
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
                      {opponent.userName ?? locale.anonymousPlayer}
                    </Typography>
                    <Typography variant="caption" sx={{ color: 'rgba(248, 221, 207, 0.7)' }}>
                      {opponent.job.displayName}
                    </Typography>
                  </Stack>
                  <Button
                    variant="contained"
                    disabled={isActionDisabled}
                    onClick={() => onFight(opponent)}
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
      {isMobile && pageCount > 1 && (
        <Stack alignItems="center">
          <Pagination
            page={safePage}
            count={pageCount}
            onChange={handlePageChange}
            color="primary"
            sx={{
              '& .MuiPaginationItem-root': {
                color: '#f5f0df',
                borderColor: 'rgba(214, 146, 112, 0.4)',
                backgroundColor: 'rgba(255, 255, 255, 0.06)',
              },
              '& .MuiPaginationItem-root:hover': {
                backgroundColor: 'rgba(255, 255, 255, 0.14)',
              },
              '& .MuiPaginationItem-root.Mui-selected': {
                color: '#2a1112',
                backgroundColor: '#e8a88f',
                borderColor: '#e8a88f',
                fontWeight: 800,
              },
              '& .MuiPaginationItem-root.Mui-selected:hover': {
                backgroundColor: '#d99077',
              },
              '& .MuiPaginationItem-ellipsis': {
                color: 'rgba(245, 240, 223, 0.84)',
                backgroundColor: 'transparent',
              },
            }}
          />
        </Stack>
      )}
    </Stack>
  )
}
