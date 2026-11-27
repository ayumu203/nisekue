import { useState } from 'react'
import type { ReactNode } from 'react'
import { Box, Button, Collapse, IconButton, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'
import { greenBadgeSx, greenBadgeTextSx, innerSurfaceSx, topNavigationIconButtonSx } from '@/constants/styles'
import type { GetPlayerResponse } from '@/schema/player'
import locale from '../../../locale/home/Home.json'
import StatusStatRow from '@/components/home/StatusStatRow'
import { GearIcon, SignOutIcon } from '@/components/home/StatusIcons'
import { useSignOut } from '@/hooks/useSignOut'
import { resolveCharacterAssetPath, resolveStatusAssetPath } from '@/lib/assets'
import { formatEquippedItemWithPlus, formatExpProgress, formatStatusValue, toNormalized } from '@/lib/playerStatus'
import { font, palette, withAlpha } from '@/theme/tokens'

type StatValue = number | string

type StatItem = {
  key: string
  label: string
  value: StatValue
  normalized: number
  rank?: string
}

type StatusProps = {
  player: GetPlayerResponse | undefined
  compactTrainingMobile?: boolean
  showDesktopActions?: boolean
  topAction?: ReactNode
  actionAlign?: 'start' | 'end'
  gearIconId?: string
  onGearClick?: () => void
  // ステータス欄を畳むボタン（−/+）を出すか。ホームでは出さない
  showCollapseToggle?: boolean
  // compact：キャラ画像を小さくして横に並べる（スマホのホーム用）
  layout?: 'default' | 'compact'
}

export default function Status({
  player,
  compactTrainingMobile = false,
  showDesktopActions = true,
  topAction,
  actionAlign = 'end',
  gearIconId,
  onGearClick,
  showCollapseToggle = true,
  layout = 'default',
}: StatusProps) {
  const isCompact = layout === 'compact'
  const [failedImagePath, setFailedImagePath] = useState<string | null>(null)
  const [isOpen, setIsOpen] = useState(true)
  const [isDetailOpen, setIsDetailOpen] = useState(false)
  const { signOut, isSigningOut } = useSignOut()
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
      rank: player?.statusRanks?.strength,
    },
    {
      key: 'defense',
      label: locale.labels.defense,
      value: formatStatusValue(player?.baseStatus?.defense, player?.status.defense, locale.unknownValue),
      normalized: toNormalized(player?.status.defense, maxAttributeValue),
      rank: player?.statusRanks?.defense,
    },
    {
      key: 'intelligence',
      label: locale.labels.intelligence,
      value: formatStatusValue(player?.baseStatus?.intelligence, player?.status.intelligence, locale.unknownValue),
      normalized: toNormalized(player?.status.intelligence, maxAttributeValue),
      rank: player?.statusRanks?.intelligence,
    },
    {
      key: 'luck',
      label: locale.labels.luck,
      value: formatStatusValue(player?.baseStatus?.luck, player?.status.luck, locale.unknownValue),
      normalized: toNormalized(player?.status.luck, maxAttributeValue),
      rank: player?.statusRanks?.luck,
    },
    {
      key: 'speed',
      label: locale.labels.speed,
      value: formatStatusValue(player?.baseStatus?.speed, player?.status.speed, locale.unknownValue),
      normalized: toNormalized(player?.status.speed, maxAttributeValue),
      rank: player?.statusRanks?.speed,
    },
  ]

  const characterImageSrc =
    player?.imagePath && player.imagePath !== failedImagePath ? resolveCharacterAssetPath(player.imagePath) : null
  const characterBackgroundSrc = resolveStatusAssetPath('back-image.jpg')
  const isCurrentJobMastered =
    player?.job?.code != null && player.masteredJobs.some((job) => job.code === player.job.code)
  const currentJobLevelLabel = isCurrentJobMastered
    ? `${player?.job.displayName ?? locale.unknownValue} マスター`
    : typeof player?.jobLevel === 'number'
      ? `${player?.job.displayName ?? locale.unknownValue} Lv.${player.jobLevel}`
      : (player?.job.displayName ?? locale.unknownValue)

  const characterImage = (
    <Box
      sx={{
        ...(isCompact
          ? { width: 96, height: 112, flex: 'none' }
          : { width: '100%', maxWidth: 237, aspectRatio: '338 / 350', alignSelf: 'center' }),
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
  )

  const statusPaper = (
    <Paper
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        borderRadius: 3,
        p: isCompact ? 1.5 : 2.5,
      }}
    >
      <Stack spacing={2}>
        <Stack spacing={2}>
          {isCompact ? (
            // 横長表示：上に名前、その下の左にキャラ画像、右に職業・Lv・HP/MP
            <Stack spacing={1}>
              <Stack direction="row" alignItems="center" spacing={0.75} sx={{ minHeight: 36 }}>
                {/* 右の設定ボタンと同じ幅の空きを左に置いて、名前を真ん中に寄せる */}
                <Box sx={{ width: 36, flex: 'none' }} />
                <Typography
                  fontWeight={800}
                  sx={{
                    flex: 1,
                    minWidth: 0,
                    // 長い名前（最大 20 文字）は少し小さくして、2 行に収める
                    fontSize: (player?.userName?.length ?? 0) > 12 ? font.md : font.xl,
                    lineHeight: 1.3,
                    overflowWrap: 'anywhere',
                    textAlign: 'center',
                  }}
                >
                  {player?.userName ?? locale.notSet}
                </Typography>
                {/* 横長表示では、設定ボタンを名前の横に置く（ログアウトはメニューの「その他」にある） */}
                <IconButton
                  id={gearIconId}
                  component={Link}
                  to="/player-setting"
                  aria-label="プレイヤー設定へ移動"
                  onClick={onGearClick}
                  sx={{
                    width: 36,
                    height: 36,
                    flex: 'none',
                    color: palette.gold700,
                    '&&': { backgroundColor: palette.white },
                    border: `1px solid ${palette.brown200}`,
                  }}
                >
                  <GearIcon fontSize="small" />
                </IconButton>
              </Stack>
              <Stack direction="row" spacing={1.5} alignItems="flex-end">
                {characterImage}
                <Stack spacing={0.75} sx={{ flex: 1, minWidth: 0 }}>
                  <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
                    <Typography variant="body2" color="text.secondary" noWrap sx={{ minWidth: 0 }}>
                      {currentJobLevelLabel}
                    </Typography>
                    {/* 横長表示ではチップにせず、文字だけで出す */}
                    <Typography variant="subtitle2" fontWeight={700} sx={{ flex: 'none' }}>
                      Lv. {player?.level ?? locale.unknownValue}
                    </Typography>
                  </Stack>
                  {resourceItems.map((item) => (
                    <StatusStatRow
                      key={item.key}
                      label={item.label}
                      value={item.value}
                      normalized={item.normalized}
                      rank={item.rank}
                      compact
                      plain
                    />
                  ))}
                </Stack>
              </Stack>
            </Stack>
          ) : (
            <>
              {characterImage}
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
                    <StatusStatRow
                      key={item.key}
                      label={item.label}
                      value={item.value}
                      normalized={item.normalized}
                      rank={item.rank}
                      compact
                    />
                  ))}
                </Box>
              </Box>
            </>
          )}

          {compactTrainingMobile ? (
            <Stack spacing={1.5}>
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
                  backgroundColor: palette.green700,
                  color: palette.cream,
                  boxShadow: 'none',
                }}
              >
                {isDetailOpen ? locale.statusDetailsHide : locale.statusDetailsShow}
              </Button>
              <Collapse in={isDetailOpen}>
                <Stack spacing={1.5}>
                  <StatusStatRow
                    label={locale.labels.exp}
                    value={formatExpProgress(player?.exp, player?.requiredExpForNextLevel, locale.unknownValue)}
                    normalized={0}
                    hideGauge
                  />
                  <StatusStatRow
                    label={locale.labels.weapon}
                    value={formatEquippedItemWithPlus(equippedWeapon, locale.notSet)}
                    normalized={0}
                    hideGauge
                  />
                  <StatusStatRow
                    label={locale.labels.armor}
                    value={formatEquippedItemWithPlus(equippedArmor, locale.notSet)}
                    normalized={0}
                    hideGauge
                  />
                </Stack>
              </Collapse>
            </Stack>
          ) : (
            <Box>
              <Stack spacing={1.5}>
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
                    backgroundColor: palette.green700,
                    color: palette.cream,
                    boxShadow: 'none',
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
                        rank={item.rank}
                        hideGauge
                      />
                    ))}
                    <StatusStatRow
                      label={locale.labels.exp}
                      value={formatExpProgress(player?.exp, player?.requiredExpForNextLevel, locale.unknownValue)}
                      normalized={0}
                      hideGauge
                    />
                    <StatusStatRow
                      label={locale.labels.weapon}
                      value={formatEquippedItemWithPlus(equippedWeapon, locale.notSet)}
                      normalized={0}
                      hideGauge
                    />
                    <StatusStatRow
                      label={locale.labels.armor}
                      value={formatEquippedItemWithPlus(equippedArmor, locale.notSet)}
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
        id={gearIconId}
        component={Link}
        to="/player-setting"
        aria-label="プレイヤー設定へ移動"
        onClick={onGearClick}
        sx={{
          display: showDesktopActions ? 'inline-flex' : 'none',
          ...topNavigationIconButtonSx,
        }}
      >
        <GearIcon />
      </IconButton>
      <IconButton
        onClick={() => void signOut()}
        disabled={isSigningOut}
        aria-label="ログアウト"
        sx={{
          display: showDesktopActions ? 'inline-flex' : 'none',
          ...topNavigationIconButtonSx,
          '&.Mui-disabled': {
            color: withAlpha(palette.white, 0.5),
            backgroundColor: withAlpha(palette.brown500, 0.5),
          },
        }}
      >
        <SignOutIcon />
      </IconButton>
      {showCollapseToggle && (
        <IconButton
          onClick={() => setIsOpen((current) => !current)}
          aria-label={isOpen ? locale.statusMobileToggleCloseAriaLabel : locale.statusMobileToggleOpenAriaLabel}
          sx={{
            ...topNavigationIconButtonSx,
          }}
        >
          <Typography component="span" fontSize={font.lg} fontWeight={900}>
            {isOpen ? '−' : '+'}
          </Typography>
        </IconButton>
      )}
    </Box>
  )

  // 上の行に出すものが何もないときは、行ごと出さない（PC のホームでは設定・ログアウトをヘッダーに出す）
  const hasTopRow = topAction != null || showDesktopActions || showCollapseToggle

  return (
    <Box>
      {hasTopRow && (
        <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', gap: 1, mb: 0.5 }}>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minHeight: 44 }}>
            {actionAlign === 'start' ? statusActionButtons : topAction}
          </Box>
          <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, minHeight: 44 }}>
            {actionAlign === 'start' ? topAction : statusActionButtons}
          </Box>
        </Box>
      )}

      <Collapse in={isOpen || !showCollapseToggle}>{statusPaper}</Collapse>
    </Box>
  )
}
