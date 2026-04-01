import {
  Alert,
  Box,
  CircularProgress,
  Container,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
} from '@mui/material'
import { useMemo, useState } from 'react'
import useSWR from 'swr'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { getRankings } from '@/api/ranking'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { innerSurfaceSx, outerPagePaperSx } from '@/constants/styles'
import locale from '../../locale/ranking/Ranking.json'

function getRankingTypeLabel(type: string): string {
  const labels = locale.rankingTypes as Record<string, string>
  return labels[type] ?? type
}

function getPeriodLabel(period: string): string {
  if (period === 'Total') return locale.total
  if (period === 'Weekly') return locale.weekly
  if (period === 'Daily') return locale.daily
  return period
}

function periodPriority(period: string): number {
  if (period === 'Total') return 0
  if (period === 'Weekly') return 1
  if (period === 'Daily') return 2
  return 99
}

export default function Ranking() {
  const { session, isLoading } = useAuth()
  const rankingSWRKey = session?.access_token ? (['rankings'] as const) : null
  const { data, error, isLoading: isRankingLoading } = useSWR(rankingSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.loading)
    }

    return getRankings(session.access_token)
  })

  const types = useMemo(
    () => Array.from(new Set((data?.rows ?? []).map((x) => x.rankingType))).sort((a, b) => a.localeCompare(b)),
    [data?.rows],
  )
  const [selectedType, setSelectedType] = useState<string>('')
  const [period, setPeriod] = useState<string>('ALL')
  const [combatRank, setCombatRank] = useState<string>('ALL')

  const activeType = selectedType || types[0] || ''

  const baseFilteredRows = (data?.rows ?? [])
    .filter((row) => !activeType || row.rankingType === activeType)
    .filter((row) => period === 'ALL' || row.periodKind === period)
    .filter((row) => combatRank === 'ALL' || row.combatIndexRank === combatRank)
    .sort((a, b) => a.rankPosition - b.rankPosition)

  const filteredRows =
    period === 'ALL' && combatRank !== 'ALL'
      ? Array.from(
          baseFilteredRows
            .reduce((map, row) => {
              const existing = map.get(row.player.userId)
              if (!existing || periodPriority(row.periodKind) < periodPriority(existing.periodKind)) {
                map.set(row.player.userId, row)
              }
              return map
            }, new Map<string, (typeof baseFilteredRows)[number]>())
            .values(),
        ).sort((a, b) => a.rankPosition - b.rankPosition)
      : baseFilteredRows

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.loading}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={2}>
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.title} />
          </Stack>

          <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={2}>
              <Typography variant="h4" fontWeight={900}>
                {locale.title}
              </Typography>
              <Typography variant="body2" color="text.secondary">
                {locale.snapshotAt}: {data?.snapshotAt ? new Date(data.snapshotAt).toLocaleString('ja-JP') : '-'}
              </Typography>

              {isRankingLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.loading}</Typography>
                </Stack>
              ) : error ? (
                <Alert severity="warning">{error.message}</Alert>
              ) : !data || data.rows.length === 0 ? (
                <Alert severity="info">{locale.empty}</Alert>
              ) : (
                <>
                  <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                    <FormControl fullWidth>
                      <InputLabel>{locale.title}</InputLabel>
                      <Select value={activeType} label={locale.title} onChange={(e) => setSelectedType(e.target.value)}>
                        {types.map((type) => (
                          <MenuItem key={type} value={type}>
                            {getRankingTypeLabel(type)}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                    <FormControl fullWidth>
                      <InputLabel>{locale.period}</InputLabel>
                      <Select value={period} label={locale.period} onChange={(e) => setPeriod(e.target.value)}>
                        <MenuItem value="ALL">{locale.allPeriods}</MenuItem>
                        <MenuItem value="Total">{locale.total}</MenuItem>
                        <MenuItem value="Weekly">{locale.weekly}</MenuItem>
                        <MenuItem value="Daily">{locale.daily}</MenuItem>
                      </Select>
                    </FormControl>
                    <FormControl fullWidth>
                      <InputLabel>{locale.combatRank}</InputLabel>
                      <Select value={combatRank} label={locale.combatRank} onChange={(e) => setCombatRank(e.target.value)}>
                        <MenuItem value="ALL">{locale.allCombatRanks}</MenuItem>
                        {['SSS', 'SS', 'S', 'A', 'B', 'C', 'D', 'E', 'F', 'G'].map((r) => (
                          <MenuItem key={r} value={r}>
                            {r}
                          </MenuItem>
                        ))}
                      </Select>
                    </FormControl>
                  </Stack>

                  <Stack spacing={1}>
                    {filteredRows.map((row) => (
                      <Paper key={`${row.rankingType}-${row.periodKind}-${row.combatIndexRank}-${row.rankPosition}-${row.player.userId}`} variant="outlined" sx={{ p: 1.5 }}>
                        <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
                          <Stack direction="row" spacing={1.25} alignItems="center" sx={{ minWidth: 0 }}>
                            <Box
                              component="img"
                              src={resolveCharacterAssetPath(row.player.imagePath) ?? undefined}
                              alt={row.player.userName ?? row.player.userId}
                              sx={{ width: 42, height: 42, borderRadius: 1.5, objectFit: 'cover', objectPosition: 'center top' }}
                            />
                            <Stack spacing={0.2}>
                              <Typography variant="body1" fontWeight={800}>
                                {row.rankPosition}位 {row.player.userName ?? '-'}
                              </Typography>
                              <Typography variant="caption" color="text.secondary">
                                {getRankingTypeLabel(row.rankingType)} / {getPeriodLabel(row.periodKind)}
                                {row.combatIndexRank ? ` / ${row.combatIndexRank}` : ''}
                              </Typography>
                            </Stack>
                          </Stack>
                          <Typography variant="body1" fontWeight={900}>
                            {row.score.toLocaleString('ja-JP')}
                          </Typography>
                        </Stack>
                      </Paper>
                    ))}
                  </Stack>
                </>
              )}
            </Stack>
          </Paper>
        </Stack>
      </Paper>
    </Container>
  )
}
