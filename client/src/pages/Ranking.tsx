import {
  Alert,
  Box,
  CircularProgress,
  FormControl,
  InputLabel,
  MenuItem,
  Paper,
  Select,
  Stack,
  Typography,
  useMediaQuery,
  type Theme,
} from '@mui/material'
import { alpha } from '@mui/material/styles'
import { Link } from 'react-router-dom'
import { useMemo, useState } from 'react'
import useSWR from 'swr'
import GamePageFrame from '@/components/common/GamePageFrame'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { getRankings } from '@/api/ranking'
import { useAuth } from '@/contexts/useAuth'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { resolveRankColor } from '@/lib/playerRank'
import { gamePageAccent, gamePageBackground, innerSurfaceSx } from '@/constants/styles'
import locale from '../../locale/ranking/Ranking.json'
import { font, palette, withAlpha } from '@/theme/tokens'
import { EmptyText } from '@/components/common/EmptyText'

function getRankingTypeLabel(type: string): string {
  const labels = locale.rankingTypes as Record<string, string>
  return labels[type] ?? type
}

function periodPriority(period: string): number {
  if (period === 'Total') return 0
  if (period === 'Weekly') return 1
  if (period === 'Daily') return 2
  return 99
}

const combatRankApplicableTypes = new Set([
  'questClearByCombatRank',
  'trainingBattleByCombatRank',
  'treasureMapUsageByCombatRank',
])

export default function Ranking() {
  const { session, isLoading } = useAuth()
  const rankingSWRKey = session?.access_token ? (['rankings'] as const) : null
  const {
    data,
    error,
    isLoading: isRankingLoading,
  } = useSWR(rankingSWRKey, async () => {
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
  const isCombatRankSelectorEnabled = combatRankApplicableTypes.has(activeType)
  const activeCombatRank = isCombatRankSelectorEnabled ? combatRank : 'ALL'

  const baseFilteredRows = (data?.rows ?? [])
    .filter((row) => !activeType || row.rankingType === activeType)
    .filter((row) => period === 'ALL' || row.periodKind === period)
    .filter((row) => {
      if (!isCombatRankSelectorEnabled) {
        return true
      }

      if (activeCombatRank === 'ALL') {
        return row.combatIndexRank === null
      }

      return row.combatIndexRank === activeCombatRank
    })
    .sort((a, b) => a.rankPosition - b.rankPosition)

  const formattedSnapshot = data?.snapshotAt
    ? new Date(data.snapshotAt).toLocaleString('ja-JP', { hour12: false })
    : '-'

  const filteredRows =
    period === 'ALL'
      ? Array.from(
          baseFilteredRows
            .reduce((map, row) => {
              const dedupeKey = `${row.player.userId}:${row.combatIndexRank ?? 'ALL'}`
              const existing = map.get(dedupeKey)
              if (!existing || periodPriority(row.periodKind) < periodPriority(existing.periodKind)) {
                map.set(dedupeKey, row)
              }
              return map
            }, new Map<string, (typeof baseFilteredRows)[number]>())
            .values(),
        ).sort((a, b) => a.rankPosition - b.rankPosition)
      : baseFilteredRows

  const isDesktop = useMediaQuery((theme: Theme) => theme.breakpoints.up('sm'), { noSsr: true })

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
    <GamePageFrame currentKey="ranking" currentColor={gamePageAccent} background={gamePageBackground}>
      <Stack spacing={2}>
        {isDesktop ? null : (
          <Stack direction="row" justifyContent="flex-start">
            <HomeNavIconButton ariaLabel={locale.title} />
          </Stack>
        )}

        <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
          <Stack spacing={1.5}>
            {isRankingLoading ? (
              <Stack direction="row" spacing={1} alignItems="center">
                <CircularProgress size={16} />
                <Typography variant="body2">{locale.loading}</Typography>
              </Stack>
            ) : error ? (
              <Alert severity="warning">{error.message}</Alert>
            ) : !data || data.rows.length === 0 ? (
              <EmptyText>{locale.empty}</EmptyText>
            ) : (
              <Box
                sx={{
                  borderRadius: 3,
                  backgroundColor: palette.teal700,
                  p: { xs: 2, sm: 3 },
                }}
              >
                <Stack spacing={2}>
                  <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 } }}>
                    <Stack spacing={1.5}>
                      <Typography
                        variant="overline"
                        sx={{ letterSpacing: '0.25em', color: withAlpha(palette.brown900, 0.75) }}
                      >
                        {locale.overline}
                      </Typography>
                      <Typography variant="h4" fontWeight={900} sx={{ color: palette.gold300 }}>
                        {locale.title}
                      </Typography>
                      <Typography variant="body2" color="text.secondary">
                        {locale.snapshotAt}: {formattedSnapshot}
                      </Typography>
                      <Stack direction={{ xs: 'column', md: 'row' }} spacing={1.5}>
                        <FormControl fullWidth>
                          <InputLabel>{locale.title}</InputLabel>
                          <Select
                            value={activeType}
                            label={locale.title}
                            onChange={(e) => setSelectedType(e.target.value)}
                          >
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
                          <Select
                            value={activeCombatRank}
                            label={locale.combatRank}
                            disabled={!isCombatRankSelectorEnabled}
                            onChange={(e) => setCombatRank(e.target.value)}
                          >
                            <MenuItem value="ALL">{locale.allCombatRanks}</MenuItem>
                            {['SSS', 'SS', 'S', 'A', 'B', 'C', 'D', 'E', 'F', 'G'].map((r) => (
                              <MenuItem key={r} value={r}>
                                {r}
                              </MenuItem>
                            ))}
                          </Select>
                        </FormControl>
                      </Stack>
                    </Stack>
                  </Paper>
                  <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 1.5, sm: 2.5 } }}>
                    <Stack spacing={1}>
                      {filteredRows.map((row) => {
                        const jobName = row.player.job.displayName ?? row.player.job.code
                        const rankColor = resolveRankColor(row.player.combatIndexRank) ?? palette.teal400
                        return (
                          <Paper
                            key={`${row.rankingType}-${row.periodKind}-${row.combatIndexRank}-${row.rankPosition}-${row.player.userId}`}
                            variant="outlined"
                            sx={{ p: 1.5 }}
                          >
                            <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1}>
                              <Stack direction="row" spacing={1.5} alignItems="center" sx={{ minWidth: 0 }}>
                                <Box
                                  component={Link}
                                  to={`/players/${row.player.userId}/visit`}
                                  aria-label={`${row.player.userName ?? row.player.userId} の部屋へ`}
                                  sx={{
                                    width: 42,
                                    height: 42,
                                    borderRadius: 1.5,
                                    overflow: 'hidden',
                                    position: 'relative',
                                    display: 'inline-flex',
                                  }}
                                >
                                  <Box
                                    component="img"
                                    src={resolveCharacterAssetPath(row.player.imagePath) ?? undefined}
                                    alt={row.player.userName ?? row.player.userId}
                                    sx={{
                                      width: 42,
                                      height: 42,
                                      borderRadius: 1.5,
                                      objectFit: 'cover',
                                      objectPosition: 'center top',
                                    }}
                                  />
                                  {row.rankPosition === 1 ? (
                                    <Box
                                      component="span"
                                      sx={{
                                        position: 'absolute',
                                        top: -4,
                                        right: -6,
                                        fontSize: font.sm,
                                      }}
                                    >
                                      👑
                                    </Box>
                                  ) : null}
                                </Box>
                                <Stack spacing={0.5}>
                                  <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
                                    <Typography variant="body1" fontWeight={800}>
                                      {row.rankPosition}位 {row.player.userName ?? '-'}
                                    </Typography>
                                    <Box
                                      component="span"
                                      sx={{
                                        minWidth: 52,
                                        px: 1.5,
                                        py: 0.75,
                                        display: 'inline-flex',
                                        alignItems: 'center',
                                        justifyContent: 'center',
                                        borderRadius: 999,
                                        border: '1px solid',
                                        borderColor: alpha(rankColor, 0.38),
                                        backgroundColor: alpha(rankColor, 0.14),
                                        color: rankColor,
                                        fontSize: font.md,
                                        fontWeight: 900,
                                        lineHeight: 1,
                                      }}
                                    >
                                      {row.player.combatIndexRank}
                                    </Box>
                                  </Stack>
                                  <Stack direction="row" spacing={1} sx={{ flexWrap: 'wrap', gap: 1 }}>
                                    <Typography variant="caption" color="text.secondary">
                                      {locale.jobLabel}: {jobName}
                                    </Typography>
                                    <Typography variant="caption" color="text.secondary">
                                      {locale.levelLabel} {row.player.level}
                                    </Typography>
                                    <Typography variant="caption" color="text.secondary">
                                      {locale.rebirthLabel} {row.player.rebirthCount}
                                    </Typography>
                                  </Stack>
                                </Stack>
                              </Stack>
                              <Typography variant="body1" fontWeight={900}>
                                {row.score.toLocaleString('ja-JP')}
                              </Typography>
                            </Stack>
                          </Paper>
                        )
                      })}
                    </Stack>
                  </Paper>
                </Stack>
              </Box>
            )}
          </Stack>
        </Paper>
      </Stack>
    </GamePageFrame>
  )
}
