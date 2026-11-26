import {
  Alert,
  Box,
  Button,
  ButtonBase,
  CircularProgress,
  Drawer,
  IconButton,
  Snackbar,
  Typography,
} from '@mui/material'
import { useLayoutEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { getJobRoadmap, getJobRoadmapList, unlockJobRoadmap } from '@/api/player'
import GamePanel from '@/components/common/GamePanel'
import { jobChangePanelColors as colors } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { resolveJobAssetPath, resolveJobBustAssetPath } from '@/lib/assets'
import type { GetPlayerResponse, JobRoadmapListEntry, JobRoadmapNode } from '@/schema/player'
import { InfoChip, InfoRow, InfoSection } from './jobChangeParts'
import UnlockRoadmapDialog from './UnlockRoadmapDialog'
import locale from '../../../locale/player-job/JobChange.json'

const panelProps = { bandColor: colors.band, bandSelectedColor: colors.band }
const RANK_COUNT = 6

interface JobRoadmapTabProps {
  player: GetPlayerResponse
  // 解放でゴールドが減るので、親のプレイヤー情報を読み直す
  onPlayerChanged: () => Promise<unknown>
  isWide: boolean
}

type NodeState = 'unlocked' | 'canUnlock' | 'locked'

function stateOf(entry: JobRoadmapListEntry): NodeState {
  return entry.isUnlocked ? 'unlocked' : entry.canUnlock ? 'canUnlock' : 'locked'
}

export default function JobRoadmapTab({ player, onPlayerChanged, isWide }: JobRoadmapTabProps) {
  const { session } = useAuth()
  const accessToken = session?.access_token ?? null
  const [selectedJobId, setSelectedJobId] = useState<number | null>(player.job.value)
  const [sheetOpen, setSheetOpen] = useState(false)
  const [dialogOpen, setDialogOpen] = useState(false)
  const [successMessage, setSuccessMessage] = useState<string | null>(null)
  const [submitError, setSubmitError] = useState<string | null>(null)

  const {
    data: roadmapList,
    error: listError,
    isLoading: isListLoading,
    mutate: mutateList,
  } = useSWR(accessToken ? ['job-roadmap-list'] : null, () => getJobRoadmapList(accessToken!))

  const selectedEntry = roadmapList?.find((entry) => entry.jobId === selectedJobId)
  const {
    data: roadmapTree,
    isLoading: isTreeLoading,
    mutate: mutateTree,
  } = useSWR(
    accessToken && selectedEntry?.isUnlocked ? ['job-roadmap', selectedEntry.jobId] : null,
    () => getJobRoadmap(selectedEntry!.jobId, accessToken!),
    { revalidateOnFocus: false },
  )

  const masteredCodes = useMemo(
    () => new Set((player.masteredJobs ?? []).map((job) => job.code)),
    [player.masteredJobs],
  )

  async function handleUnlockConfirm() {
    if (!accessToken || !selectedEntry) return
    setDialogOpen(false)
    setSubmitError(null)

    try {
      const result = await unlockJobRoadmap({ jobId: selectedEntry.jobId }, accessToken)
      await Promise.all([mutateList(), mutateTree(), onPlayerChanged()])
      setSuccessMessage(locale.roadmapUnlockSuccess.replace('{{jobName}}', result.jobName))
    } catch (error) {
      setSubmitError(error instanceof Error ? error.message : locale.roadmapUnlockFailed)
    }
  }

  function handleSelect(jobId: number) {
    setSelectedJobId(jobId)
    if (!isWide) setSheetOpen(true)
  }

  const entries = roadmapList ?? []
  const listStatus = isListLoading ? (
    <Box sx={{ display: 'flex', justifyContent: 'center', py: 4 }}>
      <CircularProgress size={24} />
    </Box>
  ) : listError ? (
    <Alert severity="warning">{listError.message}</Alert>
  ) : null

  const detail = selectedEntry ? (
    <JobDetail
      entry={selectedEntry}
      entries={entries}
      tree={roadmapTree ?? null}
      isTreeLoading={isTreeLoading}
      gold={player.gold}
      masteredCodes={masteredCodes}
      onUnlock={() => setDialogOpen(true)}
      compact={!isWide}
    />
  ) : null

  return (
    <Box sx={{ flex: 1, minHeight: 0, display: 'flex', flexDirection: 'column', gap: 1.5 }}>
      {submitError ? <Alert severity="error">{submitError}</Alert> : null}
      {isWide ? (
        <Box
          sx={{
            flex: 1,
            minHeight: 0,
            display: 'grid',
            gridTemplateColumns: { md: 'minmax(0, 1fr) 420px', lg: 'minmax(0, 1fr) 520px' },
            gap: 3,
          }}
        >
          <Box sx={{ minHeight: 0 }}>
            <GamePanel title={locale.roadmapTitle} {...panelProps}>
              {listStatus ?? (
                <RoadmapMap
                  entries={entries}
                  selectedJobId={selectedJobId}
                  currentJobId={player.job.value}
                  masteredCodes={masteredCodes}
                  onSelect={handleSelect}
                />
              )}
            </GamePanel>
          </Box>
          <Box sx={{ minHeight: 0 }}>
            <GamePanel title={locale.roadmapDetailTitle} disablePadding {...panelProps}>
              {detail}
            </GamePanel>
          </Box>
        </Box>
      ) : (
        <GamePanel title={locale.roadmapTitle} fillHeight={false} disablePadding {...panelProps}>
          {listStatus ?? (
            <RoadmapRankList
              entries={entries}
              selectedJobId={sheetOpen ? selectedJobId : null}
              masteredCodes={masteredCodes}
              onSelect={handleSelect}
            />
          )}
        </GamePanel>
      )}

      {!isWide ? (
        <Drawer
          anchor="bottom"
          open={sheetOpen && detail != null}
          onClose={() => setSheetOpen(false)}
          PaperProps={{ sx: { borderRadius: '18px 18px 0 0', maxHeight: '88dvh' } }}
        >
          <Box sx={{ position: 'relative' }}>
            <IconButton
              aria-label={locale.close}
              onClick={() => setSheetOpen(false)}
              sx={{
                position: 'absolute',
                right: 10,
                top: 10,
                zIndex: 2,
                width: 44,
                height: 44,
                color: '#ffffff',
                backgroundColor: colors.plate,
                '&:hover': { backgroundColor: colors.bandDark },
              }}
            >
              <CloseIcon />
            </IconButton>
            {detail}
          </Box>
        </Drawer>
      ) : null}

      <UnlockRoadmapDialog
        open={dialogOpen}
        jobName={selectedEntry?.jobName ?? ''}
        jobCode={selectedEntry?.jobCode}
        goldCost={selectedEntry?.goldCostToUnlock ?? 0}
        onClose={() => setDialogOpen(false)}
        onConfirm={handleUnlockConfirm}
      />
      <Snackbar
        open={successMessage !== null}
        autoHideDuration={3000}
        onClose={() => setSuccessMessage(null)}
        message={successMessage}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      />
    </Box>
  )
}

// ランクごとの列に並べる順番。前提ジョブの並び順の平均で並べ、線の交差を減らす
function orderByRank(entries: JobRoadmapListEntry[]): JobRoadmapListEntry[][] {
  const columns: JobRoadmapListEntry[][] = Array.from({ length: RANK_COUNT }, () => [])
  const position = new Map<number, number>()
  for (let rank = 0; rank < RANK_COUNT; rank += 1) {
    const inRank = entries.filter((entry) => entry.rank === rank)
    const keyOf = (entry: JobRoadmapListEntry) => {
      const known = entry.requiredJobIds.map((id) => position.get(id)).filter((value): value is number => value != null)
      return known.length > 0 ? known.reduce((sum, value) => sum + value, 0) / known.length : Number.MAX_SAFE_INTEGER
    }
    inRank.sort((a, b) => keyOf(a) - keyOf(b) || a.jobId - b.jobId)
    inRank.forEach((entry, index) => position.set(entry.jobId, index / Math.max(1, inRank.length - 1)))
    columns[rank] = inRank
  }
  return columns
}

function useElementSize<T extends HTMLElement>() {
  const ref = useRef<T | null>(null)
  const [size, setSize] = useState({ width: 0, height: 0 })
  useLayoutEffect(() => {
    const element = ref.current
    if (!element) return
    const update = () => setSize({ width: element.clientWidth, height: element.clientHeight })
    update()
    const observer = new ResizeObserver(update)
    observer.observe(element)
    return () => observer.disconnect()
  }, [])
  return [ref, size] as const
}

const PILL_HEIGHT = 52

// PC の系統図。見習い → 1次職 … 5次職 の列に丸い札を並べ、前提ジョブとの間を線でつなぐ
function RoadmapMap({
  entries,
  selectedJobId,
  currentJobId,
  masteredCodes,
  onSelect,
}: {
  entries: JobRoadmapListEntry[]
  selectedJobId: number | null
  currentJobId: number
  masteredCodes: Set<string>
  onSelect: (jobId: number) => void
}) {
  const columns = useMemo(() => orderByRank(entries), [entries])
  const [ref, size] = useElementSize<HTMLDivElement>()
  const columnWidth = size.width / RANK_COUNT
  const pillWidth = Math.max(120, Math.min(180, columnWidth - 14))

  const layout = new Map<number, { x: number; y: number }>()
  columns.forEach((column, rank) => {
    column.forEach((entry, index) => {
      layout.set(entry.jobId, {
        x: rank * columnWidth + (columnWidth - pillWidth) / 2,
        y: ((index + 0.5) * size.height) / column.length - PILL_HEIGHT / 2,
      })
    })
  })

  return (
    <Box sx={{ height: '100%', minHeight: 680, display: 'flex', flexDirection: 'column', gap: 1 }}>
      <Legend />
      <Box sx={{ display: 'flex', borderBottom: `1px dotted ${colors.divider}`, pb: 1 }}>
        {locale.roadmapRankHeader.map((label) => (
          <Typography key={label} sx={{ flex: 1, textAlign: 'center', fontSize: '0.875rem', color: colors.label }}>
            {label}
          </Typography>
        ))}
      </Box>
      <Box ref={ref} sx={{ position: 'relative', flex: 1, minHeight: 600 }}>
        {size.width > 0 ? (
          <>
            <Box
              component="svg"
              aria-hidden
              width={size.width}
              height={size.height}
              sx={{ position: 'absolute', inset: 0, overflow: 'visible' }}
            >
              {entries.flatMap((entry) =>
                entry.requiredJobIds.map((requiredId) => {
                  const from = layout.get(requiredId)
                  const to = layout.get(entry.jobId)
                  if (!from || !to) return null
                  const x1 = from.x + pillWidth
                  const y1 = from.y + PILL_HEIGHT / 2
                  const x2 = to.x
                  const y2 = to.y + PILL_HEIGHT / 2
                  const mx = (x1 + x2) / 2
                  return (
                    <path
                      key={`${requiredId}-${entry.jobId}`}
                      d={`M${x1} ${y1} C${mx} ${y1} ${mx} ${y2} ${x2} ${y2}`}
                      fill="none"
                      stroke={entry.isUnlocked ? colors.band : '#cbc3b4'}
                      strokeWidth={entry.isUnlocked ? 2.5 : 1.5}
                      strokeDasharray={entry.isUnlocked ? undefined : '5 5'}
                    />
                  )
                }),
              )}
            </Box>
            {entries.map((entry) => {
              const position = layout.get(entry.jobId)
              if (!position) return null
              return (
                <JobPill
                  key={entry.jobId}
                  entry={entry}
                  isSelected={entry.jobId === selectedJobId}
                  isCurrent={entry.jobId === currentJobId}
                  isMastered={masteredCodes.has(entry.jobCode)}
                  onSelect={() => onSelect(entry.jobId)}
                  sx={{ position: 'absolute', left: position.x, top: position.y, width: pillWidth }}
                />
              )
            })}
          </>
        ) : null}
      </Box>
    </Box>
  )
}

function Legend() {
  const swatch = (sx: object) => (
    <Box component="span" sx={{ width: 26, height: 16, borderRadius: 999, boxSizing: 'border-box', ...sx }} />
  )
  const items: [ReactNode, string][] = [
    [swatch({ backgroundColor: '#ffffff', border: `2px solid ${colors.band}` }), locale.roadmapUnlocked],
    [swatch({ backgroundColor: colors.canUnlock, border: `2px dashed ${colors.gold}` }), locale.roadmapCanUnlock],
    [swatch({ backgroundColor: colors.locked, border: `2px solid ${colors.lockedBorder}` }), locale.roadmapLocked],
    [
      <Box
        component="span"
        sx={{
          width: 16,
          height: 16,
          borderRadius: '50%',
          backgroundColor: colors.soft,
          boxShadow: `0 0 0 3px ${colors.gold}`,
        }}
      />,
      locale.roadmapLegendMastered,
    ],
  ]
  return (
    <Box
      sx={{
        display: 'flex',
        flexWrap: 'wrap',
        gap: 2.75,
        alignItems: 'center',
        fontSize: '0.875rem',
        color: colors.label,
      }}
    >
      {items.map(([icon, label]) => (
        <Box key={label} component="span" sx={{ display: 'flex', alignItems: 'center', gap: 0.75 }}>
          {icon}
          <span>{label}</span>
        </Box>
      ))}
    </Box>
  )
}

function JobPill({
  entry,
  isSelected,
  isCurrent = false,
  isMastered,
  onSelect,
  sx,
}: {
  entry: JobRoadmapListEntry
  isSelected: boolean
  isCurrent?: boolean
  isMastered: boolean
  onSelect: () => void
  sx?: object
}) {
  const state = stateOf(entry)
  const boxSx = {
    unlocked: { backgroundColor: '#ffffff', border: `2px solid ${colors.band}` },
    canUnlock: { backgroundColor: colors.canUnlock, border: `2px dashed ${colors.gold}` },
    locked: { backgroundColor: colors.locked, border: `2px solid ${colors.lockedBorder}` },
  }[state]
  const visible = state !== 'locked'

  return (
    <ButtonBase
      aria-pressed={isSelected}
      onClick={onSelect}
      sx={{
        height: PILL_HEIGHT,
        boxSizing: 'border-box',
        borderRadius: 999,
        pl: 0.5,
        pr: 1.25,
        gap: 1,
        justifyContent: 'flex-start',
        fontFamily: 'inherit',
        outline: isSelected ? `4px solid ${colors.bandDark}` : 'none',
        outlineOffset: 2,
        ...boxSx,
        ...sx,
      }}
    >
      <Box
        component="span"
        sx={{
          width: 40,
          height: 40,
          flex: 'none',
          borderRadius: '50%',
          overflow: 'hidden',
          backgroundColor: colors.soft,
          boxShadow: isMastered ? `0 0 0 3px ${colors.gold}` : 'none',
        }}
      >
        <Box
          component="img"
          src={resolveJobBustAssetPath(entry.jobCode) ?? ''}
          alt=""
          sx={{
            width: '100%',
            height: '150%',
            objectFit: 'cover',
            objectPosition: 'top',
            display: 'block',
            filter: visible ? 'none' : 'grayscale(1) opacity(0.5)',
          }}
        />
      </Box>
      <Box
        component="span"
        sx={{
          flex: 1,
          minWidth: 0,
          fontSize: '0.8125rem',
          textAlign: 'left',
          color: visible ? colors.text : colors.lockedText,
          whiteSpace: 'nowrap',
          overflow: 'hidden',
          textOverflow: 'ellipsis',
        }}
      >
        {entry.jobName}
      </Box>
      {state !== 'unlocked' ? (
        <Box component="span" sx={{ display: 'flex', color: state === 'canUnlock' ? colors.goldText : '#8a8173' }}>
          <LockIcon />
        </Box>
      ) : null}
      {isCurrent ? (
        <Box
          component="span"
          sx={{
            position: 'absolute',
            right: -6,
            top: -10,
            fontSize: '0.6875rem',
            color: '#ffffff',
            backgroundColor: colors.bandDark,
            borderRadius: 999,
            px: 1,
          }}
        >
          {locale.currentBadge}
        </Box>
      ) : null}
    </ButtonBase>
  )
}

// スマホの系統図：ランクごとに 2 列の札。選ぶと下から詳細のシートが出る
function RoadmapRankList({
  entries,
  selectedJobId,
  masteredCodes,
  onSelect,
}: {
  entries: JobRoadmapListEntry[]
  selectedJobId: number | null
  masteredCodes: Set<string>
  onSelect: (jobId: number) => void
}) {
  const columns = orderByRank(entries)
  return (
    <Box>
      <Box sx={{ px: 1.75, pt: 1.5 }}>
        <Legend />
      </Box>
      {columns.map((column, rank) =>
        column.length > 0 ? (
          <Box
            key={locale.roadmapRankHeader[rank]}
            sx={{
              display: 'flex',
              flexDirection: 'column',
              gap: 1,
              px: 1.75,
              py: 1.5,
              borderTop: rank === 0 ? 'none' : `1px dotted ${colors.divider}`,
            }}
          >
            <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.875rem', color: colors.label }}>
              <span>{locale.roadmapRankHeader[rank]}</span>
              <span>
                {locale.roadmapUnlockedCount
                  .replace('{{count}}', String(column.filter((entry) => entry.isUnlocked).length))
                  .replace('{{total}}', String(column.length))}
              </span>
            </Box>
            <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1 }}>
              {column.map((entry) => (
                <JobPill
                  key={entry.jobId}
                  entry={entry}
                  isSelected={entry.jobId === selectedJobId}
                  isMastered={masteredCodes.has(entry.jobCode)}
                  onSelect={() => onSelect(entry.jobId)}
                  sx={{ position: 'relative', minWidth: 0 }}
                />
              ))}
            </Box>
          </Box>
        ) : null,
      )}
    </Box>
  )
}

// 選んだジョブの詳細。上に絵、下に未解放なら前提ジョブと解放費用、解放済みなら転職の条件
function JobDetail({
  entry,
  entries,
  tree,
  isTreeLoading,
  gold,
  masteredCodes,
  onUnlock,
  compact,
}: {
  entry: JobRoadmapListEntry
  entries: JobRoadmapListEntry[]
  tree: JobRoadmapNode | null
  isTreeLoading: boolean
  gold: number
  masteredCodes: Set<string>
  onUnlock: () => void
  compact: boolean
}) {
  const state = stateOf(entry)
  const chip =
    state === 'unlocked' ? (
      <InfoChip tone="band">{locale.roadmapUnlocked}</InfoChip>
    ) : state === 'canUnlock' ? (
      <InfoChip tone="gold">{locale.roadmapCanUnlock}</InfoChip>
    ) : (
      <InfoChip tone="mute">{locale.roadmapLocked}</InfoChip>
    )

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', minHeight: '100%' }}>
      <Box
        sx={{
          height: compact ? 190 : 250,
          flex: 'none',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'flex-end',
          overflow: 'hidden',
          backgroundColor: colors.soft,
          borderBottom: `4px solid ${colors.gold}`,
        }}
      >
        <Box
          component="img"
          src={resolveJobBustAssetPath(entry.jobCode) ?? resolveJobAssetPath(entry.jobCode) ?? ''}
          alt={entry.jobName}
          sx={{ height: compact ? 170 : 230 }}
        />
      </Box>
      <Box
        sx={{ flex: 1, display: 'flex', flexDirection: 'column', gap: 2, px: compact ? 2.25 : 3.25, pt: 2.25, pb: 3 }}
      >
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-end', gap: 1.5 }}>
          <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.5 }}>
            <Typography sx={{ fontSize: '0.875rem', color: colors.label }}>
              {locale.roadmapRankDetail.replaceAll('{{rank}}', String(entry.rank))}
            </Typography>
            <Typography sx={{ fontSize: compact ? '1.375rem' : '1.625rem', color: colors.text }}>
              {entry.jobName}
            </Typography>
          </Box>
          {chip}
        </Box>
        {entry.isUnlocked ? (
          <UnlockedConditions tree={tree} isLoading={isTreeLoading} masteredCodes={masteredCodes} />
        ) : (
          <LockedConditions entry={entry} entries={entries} gold={gold} onUnlock={onUnlock} />
        )}
      </Box>
    </Box>
  )
}

function RequirementRow({ name, status, ok }: { name: string; status: string; ok: boolean }) {
  return (
    <Box sx={{ display: 'flex', alignItems: 'center', gap: 1.25, fontSize: '1rem', color: colors.text }}>
      <Box
        component="span"
        sx={{
          width: 28,
          height: 28,
          flex: 'none',
          borderRadius: '50%',
          display: 'grid',
          placeItems: 'center',
          backgroundColor: ok ? colors.soft : colors.locked,
          color: ok ? colors.band : '#8a8173',
        }}
      >
        {ok ? <CheckIcon /> : <LockIcon />}
      </Box>
      <span>{name}</span>
      <Box component="span" sx={{ ml: 'auto', fontSize: '0.875rem', color: ok ? colors.band : '#8a8173' }}>
        {status}
      </Box>
    </Box>
  )
}

function LockedConditions({
  entry,
  entries,
  gold,
  onUnlock,
}: {
  entry: JobRoadmapListEntry
  entries: JobRoadmapListEntry[]
  gold: number
  onUnlock: () => void
}) {
  const prerequisites = entry.requiredJobIds
    .map((id) => entries.find((candidate) => candidate.jobId === id))
    .filter((candidate): candidate is JobRoadmapListEntry => candidate != null)
  const cost = entry.goldCostToUnlock
  const after = gold - cost
  const canPay = after >= 0

  return (
    <>
      {prerequisites.length > 0 ? (
        <InfoSection title={locale.roadmapPrerequisites}>
          {prerequisites.map((prerequisite) => (
            <RequirementRow
              key={prerequisite.jobId}
              name={prerequisite.jobName}
              ok={prerequisite.isUnlocked}
              status={prerequisite.isUnlocked ? locale.roadmapUnlocked : locale.roadmapLocked}
            />
          ))}
        </InfoSection>
      ) : null}
      <InfoSection title={locale.roadmapCostTitle}>
        <InfoRow label={locale.roadmapRequiredGold} value={`${cost.toLocaleString()} G`} strong />
        <InfoRow
          label={locale.goldLabel}
          value={
            <>
              {gold.toLocaleString()} G →{' '}
              <Box component="span" sx={{ color: canPay ? colors.goldText : '#b3261e' }}>
                {after.toLocaleString()} G
              </Box>
            </>
          }
        />
      </InfoSection>
      <Box sx={{ mt: 'auto' }}>
        <Button
          fullWidth
          onClick={onUnlock}
          disabled={!entry.canUnlock || !canPay}
          sx={{
            height: 52,
            borderRadius: '12px',
            backgroundColor: colors.band,
            color: '#ffffff',
            fontFamily: 'inherit',
            fontSize: '1.0625rem',
            boxShadow: `0 3px 0 ${colors.bandDark}`,
            '&:hover': { backgroundColor: colors.bandDark },
            '&.Mui-disabled': { backgroundColor: '#ebe6dc', color: '#6f6758', boxShadow: 'none' },
          }}
        >
          {!entry.canUnlock
            ? locale.roadmapPrerequisiteLocked
            : !canPay
              ? locale.roadmapGoldShort
              : locale.roadmapUnlockWithGold.replace('{{gold}}', cost.toLocaleString())}
        </Button>
      </Box>
    </>
  )
}

function UnlockedConditions({
  tree,
  isLoading,
  masteredCodes,
}: {
  tree: JobRoadmapNode | null
  isLoading: boolean
  masteredCodes: Set<string>
}) {
  if (isLoading && !tree) {
    return (
      <Box sx={{ display: 'flex', justifyContent: 'center', py: 3 }}>
        <CircularProgress size={22} />
      </Box>
    )
  }

  const jobs = (tree?.requirements ?? []).filter((node) => node.type === 'job')
  const items = (tree?.requirements ?? []).filter((node) => node.type === 'item')

  if (jobs.length === 0 && items.length === 0) {
    return <Typography sx={{ fontSize: '0.9375rem', color: colors.label }}>{locale.roadmapNoCondition}</Typography>
  }

  return (
    <>
      {jobs.length > 0 ? (
        <InfoSection title={`① ${locale.roadmapItemRequiredJobs}`}>
          {jobs.map((node) => {
            const mastered = node.jobCode != null && masteredCodes.has(node.jobCode)
            return (
              <RequirementRow
                key={node.jobId}
                name={node.jobName ?? ''}
                ok={mastered}
                status={mastered ? locale.mastered : locale.roadmapLocked}
              />
            )
          })}
        </InfoSection>
      ) : null}
      {items.length > 0 ? (
        <InfoSection title={`${jobs.length > 0 ? '② ' : ''}${locale.roadmapItemTitle}`}>
          {items.map((node) => (
            <Box key={node.itemId} sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
              <Typography sx={{ fontSize: '1.125rem', color: colors.text }}>{node.itemName}</Typography>
              {node.stages && node.stages.length > 0 ? (
                <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75, alignItems: 'center' }}>
                  <Typography component="span" sx={{ fontSize: '0.875rem', color: colors.label, mr: 0.5 }}>
                    {locale.roadmapItemStage}
                  </Typography>
                  {node.stages.map((stage) => (
                    <InfoChip key={stage.id}>{stage.name}</InfoChip>
                  ))}
                </Box>
              ) : null}
            </Box>
          ))}
        </InfoSection>
      ) : null}
      {items.length > 0 ? (
        <Box sx={{ mt: 'auto' }}>
          <Button
            fullWidth
            component={Link}
            to="/items"
            sx={{
              height: 52,
              borderRadius: '12px',
              border: `2px solid ${colors.band}`,
              color: colors.band,
              fontFamily: 'inherit',
              fontSize: '1rem',
            }}
          >
            {locale.roadmapToItems}
          </Button>
        </Box>
      ) : null}
    </>
  )
}

function CheckIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="3"
      strokeLinecap="round"
      strokeLinejoin="round"
      aria-hidden="true"
    >
      <path d="M5 12l5 5 9-10" />
    </svg>
  )
}

function LockIcon() {
  return (
    <svg
      width="16"
      height="16"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.4"
      strokeLinecap="round"
      aria-hidden="true"
    >
      <rect x="5" y="11" width="14" height="10" rx="2" />
      <path d="M8 11V8a4 4 0 0 1 8 0v3" />
    </svg>
  )
}

function CloseIcon() {
  return (
    <svg
      width="18"
      height="18"
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeWidth="2.4"
      strokeLinecap="round"
      aria-hidden="true"
    >
      <path d="M6 6l12 12M18 6 6 18" />
    </svg>
  )
}
