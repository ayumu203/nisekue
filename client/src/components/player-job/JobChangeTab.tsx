import { Box, ButtonBase, Button, Typography } from '@mui/material'
import { useState } from 'react'
import { Link } from 'react-router-dom'
import GamePanel from '@/components/common/GamePanel'
import { jobChangePanelColors as colors } from '@/constants/styles'
import { resolveJobAssetPath, resolveJobBustAssetPath } from '@/lib/assets'
import type { GetPlayerResponse, PlayerJob, PlayerJobCode } from '@/schema/player'
import { ExpBar, InfoChip, PlateChip } from './jobChangeParts'
import { formatExpProgress, stageBackgroundSx } from './jobChangeStyles'
import locale from '../../../locale/player-job/JobChange.json'
import { font, palette, withAlpha } from '@/theme/tokens'

const BASE_JOB_ORDER: PlayerJobCode[] = ['Warrior', 'Guardian', 'Mage', 'Priest', 'Ranger']
const UNLOCK_LEVEL = 5

const panelProps = { bandColor: colors.band, bandSelectedColor: colors.band }

interface JobChangeTabProps {
  player: GetPlayerResponse
  isSubmittingJobValue: number | null
  onJobChange: (jobValue: number) => void
  // PC（md 以上）は左に現在の職業、右に舞台。狭い画面は縦に積む
  isWide: boolean
  // 箱をすぐ上のタブの帯とつなげる（スマホ）
  attachToTabs: boolean
  // 転職ボタンの id（チュートリアルで指すとき）
  changeButtonId?: string
}

type ActorState = 'current' | 'open' | 'locked'

export default function JobChangeTab({
  player,
  isSubmittingJobValue,
  onJobChange,
  isWide,
  attachToTabs,
  changeButtonId,
}: JobChangeTabProps) {
  const baseJobs = BASE_JOB_ORDER.map((code) => (player.jobProfiles ?? []).find((job) => job.code === code)).filter(
    (job): job is PlayerJob => job != null,
  )
  const isLocked = player.level < UNLOCK_LEVEL
  const masteredCodes = new Set((player.masteredJobs ?? []).map((job) => job.code))
  const [selectedCode, setSelectedCode] = useState<PlayerJobCode | null>(
    () => baseJobs.find((job) => job.code !== player.job.code)?.code ?? null,
  )
  // 転職したあとなど、選んでいた職業が現在の職業になったら選び直す
  const selectedJob =
    isLocked || selectedCode === player.job.code ? null : (baseJobs.find((job) => job.code === selectedCode) ?? null)

  function stateOf(job: PlayerJob): ActorState {
    if (isLocked) return 'locked'
    return job.code === player.job.code ? 'current' : 'open'
  }

  const footer = isLocked ? (
    <LockedFooter />
  ) : selectedJob ? (
    <SelectFooter
      job={selectedJob}
      isSubmitting={isSubmittingJobValue !== null}
      onChange={() => onJobChange(selectedJob.value)}
      compact={!isWide}
      buttonId={changeButtonId}
    />
  ) : null

  if (isWide) {
    return (
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          display: 'grid',
          gridTemplateColumns: { md: '380px minmax(0, 1fr)', lg: '440px minmax(0, 1fr)' },
          gap: 3,
        }}
      >
        <Box sx={{ minHeight: 0 }}>
          <GamePanel hideBand aria-label={locale.currentJobTitle} {...panelProps}>
            <CurrentJobDetail player={player} isMastered={masteredCodes.has(player.job.code)} />
          </GamePanel>
        </Box>
        <Box sx={{ minHeight: 0 }}>
          <GamePanel hideBand aria-label={locale.jobListTitle} disablePadding {...panelProps}>
            <Box
              role="group"
              aria-label={locale.jobSelectLabel}
              sx={{
                ...stageBackgroundSx,
                position: 'relative',
                flex: 1,
                minHeight: 420,
                overflow: 'hidden',
                display: 'grid',
                gridTemplateColumns: 'repeat(5, minmax(0, 1fr))',
                gridTemplateRows: 'minmax(0, 1fr)',
                pt: 6,
                px: 3,
                borderBottom: `4px solid ${colors.gold}`,
              }}
            >
              {baseJobs.map((job) => (
                <StageActor
                  key={job.code}
                  job={job}
                  state={stateOf(job)}
                  isSelected={selectedJob?.code === job.code}
                  isMastered={masteredCodes.has(job.code)}
                  onSelect={() => setSelectedCode(job.code)}
                />
              ))}
            </Box>
            {footer}
          </GamePanel>
        </Box>
      </Box>
    )
  }

  // スマホ：選んでいる 1 人を舞台の中央に大きく立たせ、下に 5 職業の顔のボタンを並べる
  const shownJob = selectedJob ?? baseJobs.find((job) => job.code === player.job.code) ?? baseJobs[0]
  const shownIndex = shownJob ? baseJobs.indexOf(shownJob) : 0
  const leftJob = baseJobs[shownIndex - 1]
  const rightJob = baseJobs[shownIndex + 1]

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1.5 }}>
      <GamePanel
        hideBand
        attachTop={attachToTabs}
        aria-label={locale.currentJobTitle}
        fillHeight={false}
        {...panelProps}
      >
        <CurrentJobCompact player={player} isMastered={masteredCodes.has(player.job.code)} />
      </GamePanel>
      <GamePanel hideBand aria-label={locale.jobListTitle} fillHeight={false} disablePadding {...panelProps}>
        <Box
          sx={{
            ...stageBackgroundSx,
            position: 'relative',
            height: 300,
            overflow: 'hidden',
            borderBottom: `4px solid ${colors.gold}`,
          }}
        >
          {[leftJob, rightJob].map((job, index) =>
            job ? (
              <Box
                key={job.code}
                component="img"
                src={resolveJobBustAssetPath(job.code) ?? ''}
                alt=""
                aria-hidden
                sx={{
                  position: 'absolute',
                  bottom: 0,
                  [index === 0 ? 'left' : 'right']: -60,
                  height: 200,
                  filter: isLocked ? 'brightness(0) opacity(0.5)' : 'saturate(0.85) brightness(0.85)',
                }}
              />
            ) : null,
          )}
          {shownJob ? (
            <Box
              component="img"
              src={resolveJobBustAssetPath(shownJob.code) ?? ''}
              alt={shownJob.displayName}
              sx={{
                position: 'absolute',
                left: '50%',
                bottom: 0,
                height: 260,
                transform: 'translateX(-50%)',
                filter: isLocked ? 'brightness(0) opacity(0.5)' : 'none',
              }}
            />
          ) : null}
          {selectedJob ? <Cursor sx={{ top: 12, fontSize: font.xxxl }} /> : null}
        </Box>
        <Box
          role="group"
          aria-label={locale.jobSelectLabel}
          sx={{
            display: 'flex',
            justifyContent: 'space-between',
            px: 2,
            pt: 2,
            pb: 2,
            borderBottom: `1px dotted ${colors.divider}`,
          }}
        >
          {baseJobs.map((job) => {
            const state = stateOf(job)
            const isSelected = selectedJob?.code === job.code
            return (
              <ButtonBase
                key={job.code}
                aria-label={job.displayName}
                aria-pressed={isSelected}
                disabled={state !== 'open'}
                onClick={() => setSelectedCode(job.code)}
                sx={{
                  position: 'relative',
                  width: 58,
                  height: 58,
                  borderRadius: '8px',
                  border: isSelected ? `3px solid ${colors.plateGold}` : `2px solid ${colors.divider}`,
                  boxShadow: isSelected ? `0 0 0 2px ${colors.bandDark}` : 'none',
                  backgroundColor: colors.soft,
                }}
              >
                <Box sx={{ width: '100%', height: '100%', borderRadius: '6px', overflow: 'hidden' }}>
                  <Box
                    component="img"
                    src={resolveJobBustAssetPath(job.code) ?? ''}
                    alt=""
                    sx={{
                      width: '100%',
                      height: '170%',
                      objectFit: 'cover',
                      objectPosition: 'top',
                      display: 'block',
                      filter: state === 'locked' ? 'brightness(0) opacity(0.5)' : 'none',
                    }}
                  />
                </Box>
                {state === 'current' ? (
                  <Box
                    component="span"
                    sx={{
                      position: 'absolute',
                      left: '50%',
                      bottom: -9,
                      transform: 'translateX(-50%)',
                      fontSize: font.xxs,
                      color: palette.white,
                      backgroundColor: colors.bandDark,
                      borderRadius: '4px',
                      px: 0.75,
                      whiteSpace: 'nowrap',
                    }}
                  >
                    {locale.currentBadge}
                  </Box>
                ) : null}
              </ButtonBase>
            )
          })}
        </Box>
        {footer}
      </GamePanel>
    </Box>
  )
}

function Cursor({ sx }: { sx?: object }) {
  return (
    <Box
      component="span"
      aria-hidden
      sx={{
        position: 'absolute',
        left: '50%',
        transform: 'translateX(-50%)',
        zIndex: 5,
        lineHeight: 1,
        fontSize: font.xxxl,
        color: colors.plateGold,
        textShadow: `0 2px 0 ${colors.bandDark}`,
        ...sx,
      }}
    >
      ▼
    </Box>
  )
}

// 舞台の上の 1 人。上半身の絵を舞台の下端にそろえ、選んでいる人は大きく前に出す
function StageActor({
  job,
  state,
  isSelected,
  isMastered,
  onSelect,
}: {
  job: PlayerJob
  state: ActorState
  isSelected: boolean
  isMastered: boolean
  onSelect: () => void
}) {
  const filter =
    state === 'locked' ? 'brightness(0) opacity(0.5)' : isSelected ? 'none' : 'saturate(0.85) brightness(0.88)'

  return (
    <ButtonBase
      aria-pressed={isSelected}
      aria-label={job.displayName}
      disabled={state !== 'open'}
      onClick={onSelect}
      sx={{
        // z-index を付けない（名札を隣の人の絵より前に出すため、重なり順は中の要素で決める）
        position: 'relative',
        height: '100%',
        minWidth: 0,
        display: 'flex',
        flexDirection: 'column',
        justifyContent: 'flex-end',
        alignItems: 'center',
        fontFamily: 'inherit',
        '&.Mui-disabled': { pointerEvents: 'none' },
      }}
    >
      <Box
        component="span"
        sx={{
          position: 'relative',
          // 舞台が縦に長い画面でも大きくなりすぎないように上限を付ける
          height: isSelected ? 'min(100%, 520px)' : 'min(82%, 430px)',
          width: '100%',
          display: 'flex',
          justifyContent: 'center',
          alignItems: 'flex-end',
        }}
      >
        {isSelected ? <Cursor sx={{ top: -6 }} /> : null}
        <Box
          component="img"
          src={resolveJobBustAssetPath(job.code) ?? ''}
          alt=""
          sx={{ position: 'relative', zIndex: isSelected ? 2 : 1, height: '100%', width: 'auto', flex: 'none', filter }}
        />
        <Box
          component="span"
          sx={{
            position: 'absolute',
            left: 0,
            right: 0,
            bottom: 14,
            zIndex: 4,
            display: 'flex',
            flexDirection: 'column',
            alignItems: 'center',
            gap: 0.75,
          }}
        >
          <Box
            component="span"
            sx={{
              backgroundColor: colors.plate,
              border: `3px solid ${isSelected ? colors.plateGold : withAlpha(palette.white, 0.9)}`,
              borderRadius: '6px',
              px: 2.5,
              py: 0.5,
              fontSize: isSelected ? font.xxl : font.xl,
              color: isSelected ? colors.plateGold : palette.white,
              whiteSpace: 'nowrap',
            }}
          >
            {job.displayName}
          </Box>
          <Box component="span" sx={{ display: 'flex', gap: 0.75, minHeight: 24 }}>
            {state === 'current' ? <PlateChip>{locale.currentBadge}</PlateChip> : null}
            {isMastered ? <PlateChip>{locale.mastered}</PlateChip> : null}
            {state === 'locked' ? <PlateChip>{locale.lockedTag}</PlateChip> : null}
          </Box>
        </Box>
      </Box>
    </ButtonBase>
  )
}

function SelectFooter({
  job,
  isSubmitting,
  onChange,
  compact,
  buttonId,
}: {
  job: PlayerJob
  isSubmitting: boolean
  onChange: () => void
  compact: boolean
  buttonId?: string
}) {
  const button = (
    <Button
      id={buttonId}
      onClick={onChange}
      disabled={isSubmitting}
      sx={{
        flex: 'none',
        width: compact ? '100%' : 'auto',
        height: compact ? 56 : 60,
        px: 4,
        borderRadius: '12px',
        backgroundColor: colors.band,
        color: palette.white,
        fontFamily: 'inherit',
        fontSize: compact ? font.xl : font.xxl,
        boxShadow: `0 4px 0 ${colors.bandDark}`,
        '&.Mui-disabled': { backgroundColor: palette.gray300, color: palette.white },
      }}
    >
      {isSubmitting ? locale.changing : locale.changeTo.replace('{{jobName}}', job.displayName)}
    </Button>
  )

  return (
    <Box sx={{ flex: 'none', display: 'flex', flexDirection: 'column', gap: 1.5, px: compact ? 2 : 3, py: 2.5 }}>
      <Box
        sx={{
          display: 'flex',
          flexDirection: compact ? 'column' : 'row',
          alignItems: compact ? 'stretch' : 'center',
          gap: compact ? 1 : 4,
        }}
      >
        <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.75 }}>
          <Typography sx={{ fontSize: compact ? font.xxl : font.xxl, color: colors.text }}>
            {job.displayName}
          </Typography>
          <Typography sx={{ fontSize: compact ? font.sm : font.md, lineHeight: 1.75, color: colors.label }}>
            {job.description}
          </Typography>
        </Box>
        {button}
      </Box>
    </Box>
  )
}

function LockedFooter() {
  return (
    <Box
      sx={{
        flex: 'none',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        gap: 2.5,
        px: 3,
        py: 3,
        fontSize: font.lg,
        color: colors.label,
      }}
    >
      <Typography component="span" sx={{ fontSize: 'inherit' }}>
        {locale.changeCondition}
      </Typography>
      <Box component={Link} to="/training" sx={{ color: colors.band }}>
        {locale.toTraining}
      </Box>
    </Box>
  )
}

function useLevelInfo(player: GetPlayerResponse, isMastered: boolean) {
  const isMaxLevel = player.isMaxLevel === true
  const progress = formatExpProgress(player.exp ?? 0, player.requiredExpForNextLevel ?? 1)
  return {
    jobLevel: isMastered ? locale.mastered : `Lv.${player.jobLevel}`,
    expText: isMaxLevel
      ? locale.expMax
      : `${progress.current.toLocaleString()} / ${progress.required.toLocaleString()}`,
    ratio: isMaxLevel ? 100 : progress.ratio,
  }
}

function MasteredChips({ player }: { player: GetPlayerResponse }) {
  const mastered = player.masteredJobs ?? []
  return (
    <Box sx={{ display: 'flex', flexWrap: 'wrap', gap: 0.75 }}>
      {mastered.length > 0 ? (
        mastered.map((job) => (
          <InfoChip key={job.code} tone="gold">
            {job.displayName}
          </InfoChip>
        ))
      ) : (
        <InfoChip tone="mute">{locale.masteredNone}</InfoChip>
      )}
    </Box>
  )
}

// PC の「現在の職業」：アーチ窓の中の絵、名前・説明、レベル、経験値、マスター済みジョブ、所持ゴールド
function CurrentJobDetail({ player, isMastered }: { player: GetPlayerResponse; isMastered: boolean }) {
  const info = useLevelInfo(player, isMastered)
  const dotted = `1px dotted ${colors.divider}`

  return (
    <Box sx={{ display: 'flex', flexDirection: 'column', gap: 2 }}>
      <Box sx={{ display: 'flex', justifyContent: 'center' }}>
        <Box
          sx={{
            width: 200,
            height: 270,
            borderRadius: '999px 999px 10px 10px',
            border: `3px solid ${colors.gold}`,
            backgroundColor: colors.soft,
            display: 'flex',
            alignItems: 'flex-end',
            justifyContent: 'center',
            overflow: 'hidden',
          }}
        >
          <Box
            component="img"
            src={resolveJobAssetPath(player.job.code) ?? ''}
            alt={player.job.displayName}
            sx={{ width: '100%', height: '92%', objectFit: 'contain', objectPosition: 'bottom', display: 'block' }}
          />
        </Box>
      </Box>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.75 }}>
        <Typography sx={{ fontSize: font.xxxl, textAlign: 'center', color: colors.text }}>
          {player.job.displayName}
        </Typography>
        <Typography sx={{ fontSize: font.sm, lineHeight: 1.7, color: colors.label }}>
          {player.job.description}
        </Typography>
      </Box>
      <Box
        sx={{
          display: 'grid',
          gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
          borderTop: dotted,
          borderBottom: dotted,
        }}
      >
        {[
          { label: locale.levelLabel, value: `Lv.${player.level}`, color: colors.text },
          { label: locale.jobLevelLabel, value: info.jobLevel, color: isMastered ? colors.goldText : colors.text },
        ].map((item, index) => (
          <Box
            key={item.label}
            sx={{
              py: 1.5,
              display: 'flex',
              flexDirection: 'column',
              alignItems: 'center',
              borderLeft: index === 0 ? 'none' : dotted,
            }}
          >
            <Typography sx={{ fontSize: font.sm, color: colors.label }}>{item.label}</Typography>
            <Typography sx={{ fontSize: font.xxl, color: item.color, letterSpacing: '0.04em' }}>
              {item.value}
            </Typography>
          </Box>
        ))}
      </Box>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 0.75 }}>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: font.sm, color: colors.label }}>
          <span>{locale.expLabel}</span>
          <Box component="span" sx={{ color: colors.text }}>
            {info.expText}
          </Box>
        </Box>
        <ExpBar ratio={info.ratio} label={locale.expLabel} />
      </Box>
      <Box sx={{ display: 'flex', flexDirection: 'column', gap: 1 }}>
        <Typography sx={{ fontSize: font.sm, color: colors.label }}>{locale.masteredJobsLabel}</Typography>
        <MasteredChips player={player} />
      </Box>
      <Box
        sx={{
          display: 'flex',
          justifyContent: 'space-between',
          alignItems: 'baseline',
          borderTop: dotted,
          pt: 1.5,
        }}
      >
        <Typography sx={{ fontSize: font.sm, color: colors.label }}>{locale.goldLabel}</Typography>
        <Typography sx={{ fontSize: font.lg, color: colors.text }}>{player.gold.toLocaleString()} G</Typography>
      </Box>
    </Box>
  )
}

// スマホの「現在の職業」：小さいアーチ窓の横に名前・レベル・経験値・マスター済みジョブ
function CurrentJobCompact({ player, isMastered }: { player: GetPlayerResponse; isMastered: boolean }) {
  const info = useLevelInfo(player, isMastered)

  return (
    <Box sx={{ display: 'flex', gap: 2, alignItems: 'center' }}>
      <Box
        sx={{
          width: 88,
          height: 124,
          flex: 'none',
          borderRadius: '999px 999px 8px 8px',
          border: `3px solid ${colors.gold}`,
          backgroundColor: colors.soft,
          display: 'flex',
          alignItems: 'flex-end',
          justifyContent: 'center',
          overflow: 'hidden',
        }}
      >
        <Box
          component="img"
          src={resolveJobAssetPath(player.job.code) ?? ''}
          alt={player.job.displayName}
          sx={{ width: '100%', height: '92%', objectFit: 'contain', objectPosition: 'bottom', display: 'block' }}
        />
      </Box>
      <Box sx={{ flex: 1, minWidth: 0, display: 'flex', flexDirection: 'column', gap: 0.75 }}>
        <Typography sx={{ fontSize: font.xxl, color: colors.text }}>{player.job.displayName}</Typography>
        <Box
          sx={{
            display: 'flex',
            flexWrap: 'wrap',
            columnGap: 1.5,
            rowGap: 0.25,
            fontSize: font.sm,
            color: colors.label,
          }}
        >
          <span>
            {locale.levelLabel}{' '}
            <Box component="b" sx={{ fontWeight: 400, fontSize: font.md, color: colors.text }}>
              Lv.{player.level}
            </Box>
          </span>
          <span>
            {locale.jobLevelLabel}{' '}
            <Box
              component="b"
              sx={{
                fontWeight: 400,
                fontSize: font.md,
                color: isMastered ? colors.goldText : colors.text,
                letterSpacing: '0.04em',
              }}
            >
              {info.jobLevel}
            </Box>
          </span>
        </Box>
        <Box sx={{ display: 'flex', justifyContent: 'space-between', fontSize: font.sm, color: colors.label }}>
          <span>{locale.expLabel}</span>
          <Box component="span" sx={{ color: colors.text }}>
            {info.expText}
          </Box>
        </Box>
        <ExpBar ratio={info.ratio} label={locale.expLabel} />
        <MasteredChips player={player} />
      </Box>
    </Box>
  )
}
