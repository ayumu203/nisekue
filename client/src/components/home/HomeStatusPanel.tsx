import { Box, Stack, Typography } from '@mui/material'
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

type HomeStatusPanelProps = {
  player: GetPlayerResponse | undefined
}

function StatLine({ label, value, rank }: { label: string; value: ReactNode; rank?: string }) {
  const rankColor = resolveRankColor(rank)

  return (
    <Stack
      direction="row"
      alignItems="center"
      spacing={1.25}
      sx={{ py: 1, borderBottom: `1px dotted ${homePanelColors.divider}` }}
    >
      <Typography sx={{ flex: 1, fontSize: '0.875rem', color: homePanelColors.label }}>{label}</Typography>
      <Typography sx={{ fontSize: '1rem', fontWeight: 700, color: homePanelColors.text, whiteSpace: 'nowrap' }}>
        {value}
      </Typography>
      {rank ? (
        <Box
          component="span"
          sx={{
            flex: 'none',
            minWidth: 34,
            textAlign: 'center',
            fontSize: '0.8125rem',
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

function SmallStat({ label, value }: { label: string; value: ReactNode }) {
  return (
    <Stack direction="row" justifyContent="space-between" spacing={1}>
      <Typography sx={{ fontSize: '0.875rem', color: homePanelColors.label }}>{label}</Typography>
      <Typography sx={{ fontSize: '1rem', fontWeight: 700, color: homePanelColors.text, whiteSpace: 'nowrap' }}>
        {value}
      </Typography>
    </Stack>
  )
}

// PC のホームのステータス欄の中身。畳まずに全部出す
// 上：キャラ画像と、職業・レベル・経験値・HP/MP、中：能力値（ランク付き）、下：装備
export default function HomeStatusPanel({ player }: HomeStatusPanelProps) {
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
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

  return (
    <Stack spacing={2}>
      <Stack direction="row" spacing={1.75} alignItems="stretch">
        <Box
          sx={{
            width: 120,
            height: 140,
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
          <Typography noWrap sx={{ fontSize: '1rem', fontWeight: 700, color: homePanelColors.text }}>
            {jobLabel}
          </Typography>
          <Stack direction="row" alignItems="baseline" spacing={1}>
            <Typography sx={{ fontSize: '0.875rem', color: homePanelColors.label }}>{locale.labels.level}</Typography>
            <Typography sx={{ fontSize: '1.25rem', fontWeight: 800, lineHeight: 1, color: homePanelColors.text }}>
              {player?.level ?? unknown}
            </Typography>
          </Stack>
          <Box>
            <Stack direction="row" justifyContent="space-between" spacing={1}>
              <Typography sx={{ fontSize: '0.875rem', color: homePanelColors.label }}>{locale.labels.exp}</Typography>
              <Typography sx={{ fontSize: '1rem', fontWeight: 700, color: homePanelColors.text, whiteSpace: 'nowrap' }}>
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
          <SmallStat
            label={locale.labels.maxHp}
            value={formatStatusValue(player?.baseStatus?.maxHp, player?.status.maxHp, unknown)}
          />
          <SmallStat
            label={locale.labels.maxMp}
            value={formatStatusValue(player?.baseStatus?.maxMp, player?.status.maxMp, unknown)}
          />
        </Stack>
      </Stack>

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
    </Stack>
  )
}
