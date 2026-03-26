import { useState } from 'react'
import { Box, Collapse, IconButton, Paper, Stack, Typography } from '@mui/material'
import { greenBadgeSx, greenBadgeTextSx, innerSurfaceSx } from '@/constants/styles'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/home/Home.json'
import StatusStatRow from '@/components/home/StatusStatRow'
import { resolveCharacterAssetPath, resolveStatusAssetPath } from '@/lib/assets'

type StatValue = number | string

type StatItem = {
  key: string
  label: string
  value: StatValue
  normalized: number
}

type StatusProps = {
  player: GetPlayerResponse | undefined
  compactTrainingMobile?: boolean
}

function toStatValue(value: number | undefined, fallback: string): StatValue {
  return typeof value === 'number' ? value : fallback
}

function toNormalized(value: number | undefined, maxValue: number): number {
  if (typeof value !== 'number' || maxValue <= 0) {
    return 0
  }

  return Math.max(0, Math.min(100, (value / maxValue) * 100))
}

function formatExpProgress(exp: number | undefined, level: number | undefined, fallback: string): string {
  if (typeof exp !== 'number' || typeof level !== 'number') {
    return `${fallback} / ${fallback}`
  }

  if (!Number.isFinite(exp) || !Number.isFinite(level) || level <= 0) {
    return `${fallback} / ${fallback}`
  }

  const requiredExp = level * 10
  return `${exp} / ${requiredExp}`
}

export default function Status({ player, compactTrainingMobile = false }: StatusProps) {
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
  const [isMobileOpen, setIsMobileOpen] = useState(false)
  const maxResourceValue = Math.max(player?.status.maxHp ?? 0, player?.status.maxMp ?? 0, 1)
  const maxAttributeValue = Math.max(
    player?.status.strength ?? 0,
    player?.status.defense ?? 0,
    player?.status.intelligence ?? 0,
    player?.status.luck ?? 0,
    player?.status.speed ?? 0,
    1,
  )

  const resourceItems: StatItem[] = [
    {
      key: 'maxHp',
      label: locale.labels.maxHp,
      value: toStatValue(player?.status.maxHp, locale.unknownValue),
      normalized: toNormalized(player?.status.maxHp, maxResourceValue),
    },
    {
      key: 'maxMp',
      label: locale.labels.maxMp,
      value: toStatValue(player?.status.maxMp, locale.unknownValue),
      normalized: toNormalized(player?.status.maxMp, maxResourceValue),
    },
  ]

  const attributeItems: StatItem[] = [
    {
      key: 'strength',
      label: locale.labels.strength,
      value: toStatValue(player?.status.strength, locale.unknownValue),
      normalized: toNormalized(player?.status.strength, maxAttributeValue),
    },
    {
      key: 'defense',
      label: locale.labels.defense,
      value: toStatValue(player?.status.defense, locale.unknownValue),
      normalized: toNormalized(player?.status.defense, maxAttributeValue),
    },
    {
      key: 'intelligence',
      label: locale.labels.intelligence,
      value: toStatValue(player?.status.intelligence, locale.unknownValue),
      normalized: toNormalized(player?.status.intelligence, maxAttributeValue),
    },
    {
      key: 'luck',
      label: locale.labels.luck,
      value: toStatValue(player?.status.luck, locale.unknownValue),
      normalized: toNormalized(player?.status.luck, maxAttributeValue),
    },
    {
      key: 'speed',
      label: locale.labels.speed,
      value: toStatValue(player?.status.speed, locale.unknownValue),
      normalized: toNormalized(player?.status.speed, maxAttributeValue),
    },
  ]

  const characterImageSrc =
    player?.imagePath && player.imagePath !== failedImagePath ? resolveCharacterAssetPath(player.imagePath) : null
  const characterBackgroundSrc = resolveStatusAssetPath('back-image.jpg')
  const currentJobLevelLabel =
    typeof player?.jobLevel === 'number'
      ? `${player?.job.displayName ?? locale.unknownValue} Lv.${player.jobLevel}`
      : (player?.job.displayName ?? locale.unknownValue)

  const statusPaper = (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: 2.5,
      }}
    >
      <Stack spacing={2}>
        <Stack spacing={2}>
          <Box
            sx={{
              width: '100%',
              maxWidth: 237,
              aspectRatio: '338 / 350',
              alignSelf: 'center',
              borderRadius: 2,
              border: '1px solid',
              borderColor: 'divider',
              backgroundImage: `url(${characterBackgroundSrc})`,
              backgroundSize: 'cover',
              backgroundPosition: 'center',
              backgroundRepeat: 'no-repeat',
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
            ) : (
              <Stack alignItems="center" justifyContent="center" sx={{ width: '100%', height: '100%', px: 2 }}>
                <Typography variant="body2" color="text.secondary">
                  {locale.notSet}
                </Typography>
              </Stack>
            )}
          </Box>

          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            alignItems={{ xs: 'flex-start', sm: 'center' }}
            justifyContent="space-between"
          >
            {compactTrainingMobile ? null : (
              <Box>
                <Typography variant="h6" fontWeight={700}>
                  {player?.userName ?? locale.notSet}
                </Typography>
                <Typography variant="body2" color="text.secondary">
                  {currentJobLevelLabel}
                </Typography>
              </Box>
            )}
            <Box sx={greenBadgeSx}>
              <Typography variant="subtitle2" sx={greenBadgeTextSx}>
                {locale.labels.level}: {player?.level ?? locale.unknownValue}
              </Typography>
            </Box>
          </Stack>

          <Box>
            <Box
              sx={{
                display: 'grid',
                gridTemplateColumns: { xs: '1fr', sm: 'repeat(2, minmax(0, 1fr))' },
                gap: 1.5,
              }}
            >
              {resourceItems.map((item) => (
                <StatusStatRow key={item.key} label={item.label} value={item.value} normalized={item.normalized} />
              ))}
            </Box>
          </Box>

          {compactTrainingMobile ? (
            <Stack spacing={1.5}>
              <StatusStatRow
                label={locale.labels.exp}
                value={formatExpProgress(player?.exp, player?.level, locale.unknownValue)}
                normalized={0}
                hideGauge
              />
              <StatusStatRow
                label={locale.labels.jobExp}
                value={formatExpProgress(player?.jobExp, player?.jobLevel, locale.unknownValue)}
                normalized={0}
                hideGauge
              />
            </Stack>
          ) : (
            <Box>
              <Stack spacing={1.5}>
                {attributeItems.map((item) => (
                  <StatusStatRow
                    key={item.key}
                    label={item.label}
                    value={item.value}
                    normalized={item.normalized}
                    hideGauge
                  />
                ))}
                <StatusStatRow
                  label={locale.labels.exp}
                  value={formatExpProgress(player?.exp, player?.level, locale.unknownValue)}
                  normalized={0}
                  hideGauge
                />
                <StatusStatRow
                  label={locale.labels.jobExp}
                  value={formatExpProgress(player?.jobExp, player?.jobLevel, locale.unknownValue)}
                  normalized={0}
                  hideGauge
                />
              </Stack>
            </Box>
          )}
        </Stack>
      </Stack>
    </Paper>
  )

  return (
    <Box>
      <Box sx={{ display: { xs: 'flex', sm: 'none' }, justifyContent: 'flex-end', mb: 0.5 }}>
        <IconButton
          onClick={() => setIsMobileOpen((current) => !current)}
          aria-label={isMobileOpen ? 'ステータスを閉じる' : 'ステータスを開く'}
          sx={{
            width: 44,
            height: 44,
            border: '2px solid #ffffff',
            color: '#ffffff',
            backgroundColor: 'rgba(122, 77, 25, 0.9)',
            '&:hover': {
              backgroundColor: 'rgba(110, 68, 21, 0.94)',
            },
          }}
        >
          <Typography component="span" fontSize="1.1rem" fontWeight={900}>
            {isMobileOpen ? '−' : '+'}
          </Typography>
        </IconButton>
      </Box>

      <Box sx={{ display: { xs: 'none', sm: 'block' } }}>{statusPaper}</Box>

      <Collapse in={isMobileOpen} sx={{ display: { xs: 'block', sm: 'none' } }}>
        {statusPaper}
      </Collapse>
    </Box>
  )
}
