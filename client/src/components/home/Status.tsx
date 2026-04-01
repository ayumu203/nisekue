import { useState } from 'react'
import type { ReactNode } from 'react'
import {
  Alert,
  Box,
  Button,
  Collapse,
  IconButton,
  Paper,
  Stack,
  SvgIcon,
  Typography,
  type SvgIconProps,
} from '@mui/material'
import { Link } from 'react-router-dom'
import { greenBadgeSx, greenBadgeTextSx, innerSurfaceSx, topNavigationIconButtonSx } from '@/constants/styles'
import type { GetPlayerResponse } from '@/schema/player'
import signOutLocale from '../../../locale/auth/SignOut.json'
import locale from '../../../locale/home/Home.json'
import StatusStatRow from '@/components/home/StatusStatRow'
import { resolveCharacterAssetPath, resolveStatusAssetPath } from '@/lib/assets'
import { supabase } from '@/lib/supabase'

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
  showDesktopActions?: boolean
  topAction?: ReactNode
  actionAlign?: 'start' | 'end'
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

function formatStatusValue(
  baseValue: number | undefined,
  effectiveValue: number | undefined,
  fallback: string,
): StatValue {
  if (typeof baseValue !== 'number' || typeof effectiveValue !== 'number') {
    return fallback
  }

  const diff = effectiveValue - baseValue
  if (diff === 0) {
    return effectiveValue
  }

  return `${effectiveValue} (${diff > 0 ? '+' : ''}${diff})`
}

function formatEquippedItemWithDurability(
  equipment:
    | {
        name: string
        durability: number
        maxDurability: number
      }
    | undefined,
  fallback: string,
): string {
  if (!equipment) {
    return fallback
  }

  return `${equipment.name} (${equipment.durability}/${equipment.maxDurability})`
}

function SettingGearIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M19.14 12.94c.04-.31.06-.63.06-.94s-.02-.63-.06-.94l2.03-1.58a.5.5 0 0 0 .12-.64l-1.92-3.32a.5.5 0 0 0-.6-.22l-2.39.96a7.3 7.3 0 0 0-1.63-.94l-.36-2.54a.5.5 0 0 0-.5-.42H10.1a.5.5 0 0 0-.5.42l-.36 2.54c-.58.22-1.12.53-1.63.94l-2.39-.96a.5.5 0 0 0-.6.22L2.7 8.84a.5.5 0 0 0 .12.64l2.03 1.58c-.04.31-.06.63-.06.94s.02.63.06.94L2.82 14.52a.5.5 0 0 0-.12.64l1.92 3.32c.13.22.39.31.6.22l2.39-.96c.5.41 1.05.72 1.63.94l.36 2.54c.04.24.25.42.5.42h3.8c.25 0 .46-.18.5-.42l.36-2.54c.58-.22 1.12-.53 1.63-.94l2.39.96c.22.09.47 0 .6-.22l1.92-3.32a.5.5 0 0 0-.12-.64zM12 15.5A3.5 3.5 0 1 1 12 8.5a3.5 3.5 0 0 1 0 7" />
    </SvgIcon>
  )
}

function SignOutDoorIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M6 3h9a2 2 0 0 1 2 2v4h-2V5H6v14h9v-4h2v4a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2m9.59 4.59L21 13l-5.41 5.41L14.17 17 17.17 14H9v-2h8.17l-3-3z" />
    </SvgIcon>
  )
}

export default function Status({
  player,
  compactTrainingMobile = false,
  showDesktopActions = true,
  topAction,
  actionAlign = 'end',
}: StatusProps) {
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(true)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const [isSigningOut, setIsSigningOut] = useState(false)
  const [signOutError, setSignOutError] = useState<string | null>(null)
  const equippedWeapon = player?.equipments.find(
    (equipment) => equipment.equipmentType === 'Weapon' && equipment.status === 'Equipped',
  )
  const equippedArmor = player?.equipments.find(
    (equipment) => equipment.equipmentType === 'Armor' && equipment.status === 'Equipped',
  )
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
      value: formatStatusValue(player?.baseStatus?.maxHp, player?.status.maxHp, locale.unknownValue),
      normalized: toNormalized(player?.status.maxHp, maxResourceValue),
    },
    {
      key: 'maxMp',
      label: locale.labels.maxMp,
      value: formatStatusValue(player?.baseStatus?.maxMp, player?.status.maxMp, locale.unknownValue),
      normalized: toNormalized(player?.status.maxMp, maxResourceValue),
    },
  ]

  const attributeItems: StatItem[] = [
    {
      key: 'strength',
      label: locale.labels.strength,
      value: formatStatusValue(player?.baseStatus?.strength, player?.status.strength, locale.unknownValue),
      normalized: toNormalized(player?.status.strength, maxAttributeValue),
    },
    {
      key: 'defense',
      label: locale.labels.defense,
      value: formatStatusValue(player?.baseStatus?.defense, player?.status.defense, locale.unknownValue),
      normalized: toNormalized(player?.status.defense, maxAttributeValue),
    },
    {
      key: 'intelligence',
      label: locale.labels.intelligence,
      value: formatStatusValue(player?.baseStatus?.intelligence, player?.status.intelligence, locale.unknownValue),
      normalized: toNormalized(player?.status.intelligence, maxAttributeValue),
    },
    {
      key: 'luck',
      label: locale.labels.luck,
      value: formatStatusValue(player?.baseStatus?.luck, player?.status.luck, locale.unknownValue),
      normalized: toNormalized(player?.status.luck, maxAttributeValue),
    },
    {
      key: 'speed',
      label: locale.labels.speed,
      value: formatStatusValue(player?.baseStatus?.speed, player?.status.speed, locale.unknownValue),
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

  const handleSignOut = async () => {
    setIsSigningOut(true)
    setSignOutError(null)

    const { error } = await supabase.auth.signOut()
    if (error) {
      setSignOutError(error.message || signOutLocale.toastFailed)
      setIsSigningOut(false)
      return
    }

    setIsSigningOut(false)
  }

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
            <Stack spacing={1.25}>
              <Button
                variant="contained"
                size="small"
                fullWidth
                onClick={() => setIsDetailOpen((current) => !current)}
                sx={{
                  alignSelf: 'stretch',
                  borderRadius: 1,
                  minHeight: 38,
                  fontWeight: 700,
                  backgroundColor: '#5f7f67',
                  color: '#fff8ea',
                  boxShadow: 'none',
                  '&:hover': {
                    backgroundColor: '#56755f',
                    boxShadow: 'none',
                  },
                }}
              >
                {isDetailOpen ? locale.statusDetailsHide : locale.statusDetailsShow}
              </Button>
              <Collapse in={isDetailOpen}>
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
                  <StatusStatRow
                    label={locale.labels.weapon}
                    value={formatEquippedItemWithDurability(equippedWeapon, locale.notSet)}
                    normalized={0}
                    hideGauge
                  />
                  <StatusStatRow
                    label={locale.labels.armor}
                    value={formatEquippedItemWithDurability(equippedArmor, locale.notSet)}
                    normalized={0}
                    hideGauge
                  />
                </Stack>
              </Collapse>
            </Stack>
          ) : (
            <Box>
              <Stack spacing={1.25}>
                <Button
                  variant="contained"
                  size="small"
                  fullWidth
                  onClick={() => setIsDetailOpen((current) => !current)}
                  sx={{
                    alignSelf: 'stretch',
                    borderRadius: 1,
                    minHeight: 38,
                    fontWeight: 700,
                    backgroundColor: '#5f7f67',
                    color: '#fff8ea',
                    boxShadow: 'none',
                    '&:hover': {
                      backgroundColor: '#56755f',
                      boxShadow: 'none',
                    },
                  }}
                >
                  {isDetailOpen ? locale.statusDetailsHide : locale.statusDetailsShow}
                </Button>
                <Collapse in={isDetailOpen}>
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
                    <StatusStatRow
                      label={locale.labels.weapon}
                      value={formatEquippedItemWithDurability(equippedWeapon, locale.notSet)}
                      normalized={0}
                      hideGauge
                    />
                    <StatusStatRow
                      label={locale.labels.armor}
                      value={formatEquippedItemWithDurability(equippedArmor, locale.notSet)}
                      normalized={0}
                      hideGauge
                    />
                  </Stack>
                </Collapse>
              </Stack>
            </Box>
          )}
        </Stack>
      </Stack>
    </Paper>
  )

  const statusActionButtons = (
    <Box sx={{ display: 'flex', justifyContent: 'flex-end', gap: 1 }}>
      <IconButton
        component={Link}
        to="/player-setting"
        aria-label="プレイヤー設定へ移動"
        sx={{
          display: showDesktopActions ? 'inline-flex' : 'none',
          ...topNavigationIconButtonSx,
        }}
      >
        <SettingGearIcon />
      </IconButton>
      <IconButton
        onClick={() => void handleSignOut()}
        disabled={isSigningOut}
        aria-label="ログアウト"
        sx={{
          display: showDesktopActions ? 'inline-flex' : 'none',
          ...topNavigationIconButtonSx,
          '&.Mui-disabled': {
            color: 'rgba(255,255,255,0.56)',
            backgroundColor: 'rgba(122, 77, 25, 0.62)',
          },
        }}
      >
        <SignOutDoorIcon />
      </IconButton>
      <IconButton
        onClick={() => setIsOpen((current) => !current)}
        aria-label={isOpen ? locale.statusMobileToggleCloseAriaLabel : locale.statusMobileToggleOpenAriaLabel}
        sx={{
          ...topNavigationIconButtonSx,
        }}
      >
        <Typography component="span" fontSize="1.1rem" fontWeight={900}>
          {isOpen ? '−' : '+'}
        </Typography>
      </IconButton>
    </Box>
  )

  return (
    <Box>
      <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 1, mb: 0.5 }}>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minHeight: 44 }}>
          {actionAlign === 'start' ? statusActionButtons : topAction}
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minHeight: 44 }}>
          {actionAlign === 'start' ? topAction : statusActionButtons}
        </Box>
      </Box>

      <Collapse in={isOpen}>{statusPaper}</Collapse>

      {signOutError ? (
        <Alert severity="error" sx={{ mt: 1 }}>
          {signOutError}
        </Alert>
      ) : null}
    </Box>
  )
}
