import { Box, ButtonBase, Stack, SvgIcon, type SvgIconProps, Typography } from '@mui/material'
import { useState } from 'react'
import type { ReactNode } from 'react'
import { homePanelColors } from '@/constants/styles'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/home/Home.json'
import { resolveCharacterAssetPath, resolveStatusAssetPath } from '@/lib/assets'
import { resolveRankColor } from '@/lib/playerRank'
import {
  expProgressPercent,
  formatEquippedItemWithPlus,
  formatExpProgress,
  formatStatusValue,
} from '@/lib/playerStatus'
import { font } from '@/theme/tokens'

type HomeStatusPanelProps = {
  player: GetPlayerResponse | undefined
  // スマホ用：画像を小さくして HP・MP を下に 2 列で並べ、能力値と装備は ▾ で開いたときだけ出す
  compact?: boolean
}

function StatLine({ label, value, rank }: { label: string; value: ReactNode; rank?: string }) {
  const rankColor = resolveRankColor(rank)

  return (
    <Stack
      direction="row"
      alignItems="center"
      spacing={1.5}
      sx={{ py: 1, borderBottom: `1px dotted ${homePanelColors.divider}` }}
    >
      <Typography sx={{ flex: 1, fontSize: font.sm, color: homePanelColors.label }}>{label}</Typography>
      <Typography sx={{ fontSize: font.md, fontWeight: 700, color: homePanelColors.text, whiteSpace: 'nowrap' }}>
        {value}
      </Typography>
      {rank ? (
        <Box
          component="span"
          sx={{
            flex: 'none',
            minWidth: 34,
            textAlign: 'center',
            fontSize: font.sm,
            fontWeight: 900,
            lineHeight: 1.5,
            border: '1.5px solid',
            borderColor: rankColor,
            color: rankColor,
            borderRadius: '6px',
            px: 0.75,
          }}
        >
          {rank}
        </Box>
      ) : null}
    </Stack>
  )
}

function ChevronIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z" />
    </SvgIcon>
  )
}

function SmallStat({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={1}>
      <Typography sx={{ fontSize: font.sm, color: homePanelColors.label }}>{label}</Typography>
      <Typography sx={{ fontSize: font.md, fontWeight: 700, color: homePanelColors.text, whiteSpace: 'nowrap' }}>
        {value}
      </Typography>
    </Stack>
  )
}

// ホームのステータス欄の中身
// 上：キャラ画像と、職業・レベル・経験値・HP/MP、中：能力値（ランク付き）、下：装備
// PC は畳まずに全部出す。スマホ（compact）は能力値と装備を ▾ で開け閉めする
export default function HomeStatusPanel({ player, compact = false }: HomeStatusPanelProps) {
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const showDetail = !compact || isDetailOpen
  const unknown = locale.unknownValue
  const characterImageSrc =
    player?.imagePath && player.imagePath !== failedImagePath ? resolveCharacterAssetPath(player.imagePath) : null
  const isCurrentJobMastered =
    player?.job?.code != null && player.masteredJobs.some((job) => job.code === player.job.code)
  const jobLabel = isCurrentJobMastered
    ? `${player?.job.displayName ?? unknown} マスター`
    : typeof player?.jobLevel === 'number'
      ? `${player?.job.displayName ?? unknown} Lv.${player.jobLevel}`
      : (player?.job.displayName ?? unknown)
  const expPercent = expProgressPercent(player?.exp, player?.requiredExpForNextLevel, player?.isMaxLevel ?? false)
  const equippedWeapon = player?.equipments.find(
    (equipment) => equipment.equipmentType === 'Weapon' && equipment.status === 'Equipped',
  )
  const equippedArmor = player?.equipments.find(
    (equipment) => equipment.equipmentType === 'Armor' && equipment.status === 'Equipped',
  )
  const attributes = [
    { key: 'strength', label: locale.labels.strength },
    { key: 'defense', label: locale.labels.defense },
    { key: 'intelligence', label: locale.labels.intelligence },
    { key: 'luck', label: locale.labels.luck },
    { key: 'speed', label: locale.labels.speed },
  ] as const
  const hpMp = (
    <>
      <SmallStat
        label={locale.labels.maxHp}
        value={formatStatusValue(player?.baseStatus?.maxHp, player?.status.maxHp, unknown)}
      />
      <SmallStat
        label={locale.labels.maxMp}
        value={formatStatusValue(player?.baseStatus?.maxMp, player?.status.maxMp, unknown)}
      />
    </>
  )

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={2} alignItems="stretch">
        <Box
          sx={{
            width: compact ? 96 : 120,
            height: compact ? 112 : 140,
            flex: 'none',
            borderRadius: 2,
            border: '1px solid',
            borderColor: 'divider',
            backgroundImage: `url(${resolveStatusAssetPath('back-image.jpg')})`,
            backgroundSize: 'cover',
            backgroundPosition: 'center',
            overflow: 'hidden',
          }}
        >
          {characterImageSrc ? (
            <Box
              component="img"
              src={characterImageSrc}
              alt={player?.userName ? `${player.userName} character` : 'player character'}
              onError={() => setFailedImagePath(player?.imagePath ?? null)}
              sx={{
                width: '100%',
                height: '100%',
                objectFit: 'contain',
                objectPosition: 'center bottom',
                display: 'block',
              }}
            />
          ) : null}
        </Box>
        <Stack sx={{ flex: 1, minWidth: 0 }} justifyContent="space-between" spacing={0.75}>
          <Typography noWrap sx={{ fontSize: font.md, fontWeight: 700, color: homePanelColors.text }}>
            {jobLabel}
          </Typography>
          <Stack direction="row" alignItems="baseline" spacing={1}>
            <Typography sx={{ fontSize: font.sm, color: homePanelColors.label }}>{locale.labels.level}</Typography>
            <Typography sx={{ fontSize: font.xl, fontWeight: 800, lineHeight: 1, color: homePanelColors.text }}>
              {player?.level ?? unknown}
            </Typography>
          </Stack>
          <Box>
            <Stack direction="row" justifyContent="space-between" spacing={1}>
              <Typography sx={{ fontSize: font.sm, color: homePanelColors.label }}>{locale.labels.exp}</Typography>
              <Typography
                sx={{ fontSize: font.md, fontWeight: 700, color: homePanelColors.text, whiteSpace: 'nowrap' }}
              >
                {formatExpProgress(player?.exp, player?.requiredExpForNextLevel, unknown)}
              </Typography>
            </Stack>
            <Box
              role="progressbar"
              aria-label={locale.labels.exp}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-valuenow={Math.round(expPercent)}
              sx={{ mt: 0.5, height: 8, borderRadius: 999, backgroundColor: '#efe4c8', overflow: 'hidden' }}
            >
              <Box sx={{ width: `${expPercent}%`, height: '100%', borderRadius: 999, backgroundColor: '#e6a800' }} />
            </Box>
          </Box>
          {compact ? null : hpMp}
        </Stack>
      </Stack>
      {compact ? (
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', columnGap: 2 }}>{hpMp}</Box>
      ) : null}

      {showDetail ? (
        <>
          <Box>
            {attributes.map((item) => (
              <StatLine
                key={item.key}
                label={item.label}
                value={formatStatusValue(player?.baseStatus?.[item.key], player?.status[item.key], unknown)}
                rank={player?.statusRanks?.[item.key]}
              />
            ))}
          </Box>

          <Box>
            <StatLine label={locale.labels.weapon} value={formatEquippedItemWithPlus(equippedWeapon, locale.notSet)} />
            <StatLine label={locale.labels.armor} value={formatEquippedItemWithPlus(equippedArmor, locale.notSet)} />
          </Box>
        </>
      ) : null}
      {compact ? (
        <ButtonBase
          aria-expanded={isDetailOpen}
          aria-label={isDetailOpen ? locale.statusDetailsClose : locale.statusDetailsOpen}
          onClick={() => setIsDetailOpen((open) => !open)}
          sx={{
            // 箱の左右と下の余白を打ち消して、箱の下端いっぱいに置く
            mx: -2,
            mb: -2,
            height: 40,
            borderTop: '1px solid #f0e6cf',
            color: homePanelColors.band,
          }}
        >
          <ChevronIcon sx={{ fontSize: font.xxl, transform: isDetailOpen ? 'rotate(180deg)' : 'none' }} />
        </ButtonBase>
      ) : null}
    </Stack>
  )
}
