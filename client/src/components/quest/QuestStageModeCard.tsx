import { Box, Button, Card, Stack, Typography } from '@mui/material'
import { homePanelColors, innerSurfaceSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import locale from '../../../locale/quest/QuestRoom.json'
import { font, palette, withAlpha } from '@/theme/tokens'

type QuestStageModeCardProps = {
  stage: GetQuestStagesResponse[number]
  // 名鑑の祠ではステージのアイコンに自分のキャラ画像を出す。
  playerImagePath?: string | null
  isSelected: boolean
  selectedMode: CreateQuestRoomRequest['mode']
  onSelect: (stageId: number, mode: CreateQuestRoomRequest['mode']) => void
  // スマホ用の小さいカード（訓練場の相手カードと同じ作り：上に背景と敵、下の白地に名前とボタン）
  compact?: boolean
}

const modeButtonSx = {
  minWidth: 88,
  minHeight: 36,
  borderRadius: 999,
  fontWeight: 700,
  px: 2,
} as const

export default function QuestStageModeCard({
  stage,
  playerImagePath,
  isSelected,
  selectedMode,
  onSelect,
  compact = false,
}: QuestStageModeCardProps) {
  const isEndless = stage.progressionType === 'Endless'
  // 名鑑の祠：ソロ限定・推奨レベルは伏せる・アイコンは自分
  const isMirror = stage.progressionType === 'Mirror'
  const iconImageSrc = isMirror
    ? resolveCharacterAssetPath(playerImagePath)
    : stage.previewEnemyImagePath
      ? resolvePublicAssetPath(stage.previewEnemyImagePath)
      : null
  const isSoloSelected = isSelected && selectedMode === 'Solo'
  const isMultiSelected = isSelected && selectedMode === 'Multi'
  const battlefieldImageSrc = resolvePublicAssetPath(stage.battlefieldImagePath)

  const badge = isEndless ? locale.endless.badge : isMirror ? locale.mirror.badge : null
  const modeButton = (mode: CreateQuestRoomRequest['mode'], selected: boolean, label: string) => (
    <Button
      variant={selected ? 'contained' : 'outlined'}
      onClick={() => onSelect(stage.stageId, mode)}
      disableRipple
      sx={{
        ...modeButtonSx,
        ...(compact ? { flex: 1, minWidth: 0, minHeight: 36, px: 1, fontSize: font.sm } : null),
        '&&': {
          backgroundColor: selected ? homePanelColors.band : compact ? palette.white : withAlpha(palette.white, 0.9),
          borderColor: compact ? homePanelColors.band : selected ? homePanelColors.band : palette.white,
          color: selected ? palette.white : homePanelColors.band,
          boxShadow: 'none',
          transition: 'none',
        },
      }}
    >
      {label}
    </Button>
  )

  if (compact) {
    return (
      <Box
        sx={{
          height: '100%',
          display: 'flex',
          flexDirection: 'column',
          overflow: 'hidden',
          borderRadius: '12px',
          backgroundColor: palette.white,
          boxShadow: `0 1px 0 ${palette.brown200}`,
          outline: isSelected ? `3px solid ${homePanelColors.band}` : 'none',
          outlineOffset: -3,
        }}
      >
        <Box
          sx={{
            position: 'relative',
            height: 120,
            flex: 'none',
            display: 'grid',
            placeItems: 'center',
            backgroundImage: `url(${battlefieldImageSrc})`,
            backgroundSize: 'cover',
            backgroundPosition: 'center',
          }}
        >
          {iconImageSrc ? (
            <Box
              component="img"
              src={iconImageSrc}
              alt=""
              sx={{
                height: 96,
                maxWidth: '70%',
                objectFit: 'contain',
                imageRendering: isMirror ? 'auto' : 'pixelated',
              }}
            />
          ) : null}
          {badge ? (
            <Box
              component="span"
              sx={{
                position: 'absolute',
                top: 6,
                left: 6,
                px: 1,
                borderRadius: 999,
                fontSize: font.sm,
                fontWeight: 800,
                color: isEndless ? palette.blue900 : palette.sky,
                backgroundColor: isEndless ? palette.gold300 : palette.purple,
              }}
            >
              {badge}
            </Box>
          ) : null}
        </Box>
        <Stack spacing={0.5} sx={{ p: 1.5, flex: 1 }}>
          <Typography sx={{ fontWeight: 800, fontSize: font.md, lineHeight: 1.3, color: homePanelColors.text }}>
            {stage.name}
          </Typography>
          <Stack direction="row" spacing={1.5} sx={{ fontSize: font.sm, color: homePanelColors.label }}>
            <span>{`${locale.recommendedLevel} ${isMirror ? locale.mirror.hiddenRecommendedLevel : stage.recommendedLevel}`}</span>
            <span>{`${locale.minRequiredLevel} ${stage.minimumEntryLevel}`}</span>
          </Stack>
          <Box sx={{ flex: 1 }} />
          <Stack direction="row" spacing={0.75} sx={{ pt: 0.5 }}>
            {modeButton('Solo', isSoloSelected, locale.modeSolo)}
            {isEndless || isMirror ? null : modeButton('Multi', isMultiSelected, locale.modeMulti)}
          </Stack>
        </Stack>
      </Box>
    )
  }

  return (
    <Card
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        position: 'relative',
        overflow: 'hidden',
        height: '100%',
        borderRadius: 3,
        borderWidth: isSelected ? 3 : 1,
        color: palette.sky,
        backgroundColor: palette.blue900,
        // 選んでいるステージは白の枠で示す
        borderColor: isSelected ? palette.white : withAlpha(palette.blue300, 0.25),
        boxShadow: isSelected ? `0 10px 24px ${withAlpha(palette.blue900, 0.25)}` : 'none',
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          backgroundImage: `linear-gradient(135deg, ${withAlpha(palette.navy, 0.75)}, ${withAlpha(palette.blue900, 0.75)}), url(${battlefieldImageSrc})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
          opacity: isSelected ? 1 : 0.95,
        },
      }}
    >
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        alignItems={{ xs: 'flex-start', sm: 'center' }}
        sx={{ position: 'relative', zIndex: 1, p: 2 }}
      >
        <Box
          sx={{
            width: 84,
            height: 84,
            borderRadius: '50%',
            overflow: 'hidden',
            flexShrink: 0,
            display: 'grid',
            placeItems: 'center',
            background: `radial-gradient(circle at 30% 30%, ${palette.sky} 0%, ${palette.blue300} 100%)`,
            border: `2px solid ${palette.blue300}`,
          }}
        >
          {iconImageSrc ? (
            <Box
              component="img"
              src={iconImageSrc}
              alt={stage.name}
              sx={{
                width: '100%',
                height: '100%',
                objectFit: 'cover',
              }}
            />
          ) : (
            <Typography variant="caption" fontWeight={700} color="text.secondary">
              {locale.noImage}
            </Typography>
          )}
        </Box>

        <Stack spacing={1.5} sx={{ flex: 1, width: '100%' }}>
          <Stack spacing={0.5}>
            <Stack direction="row" spacing={1} alignItems="center" useFlexGap flexWrap="wrap">
              <Typography variant="h6" sx={{ lineHeight: 1.2, fontWeight: 900, color: palette.white }}>
                {stage.name}
              </Typography>
              {isEndless ? (
                <Box
                  component="span"
                  sx={{
                    px: 1,
                    py: 0.25,
                    borderRadius: 999,
                    fontSize: font.sm,
                    fontWeight: 800,
                    color: palette.blue900,
                    backgroundColor: palette.gold300,
                    letterSpacing: '0.04em',
                  }}
                >
                  {locale.endless.badge}
                </Box>
              ) : null}
              {isMirror ? (
                <Box
                  component="span"
                  sx={{
                    px: 1,
                    py: 0.25,
                    borderRadius: 999,
                    fontSize: font.sm,
                    fontWeight: 800,
                    color: palette.sky,
                    backgroundColor: palette.purple,
                    letterSpacing: '0.04em',
                  }}
                >
                  {locale.mirror.badge}
                </Box>
              ) : null}
            </Stack>
            <Typography variant="body2" sx={{ color: withAlpha(palette.sky, 0.75) }}>
              {`${locale.recommendedLevel} ${isMirror ? locale.mirror.hiddenRecommendedLevel : stage.recommendedLevel}`}
            </Typography>
            <Typography variant="body2" sx={{ color: withAlpha(palette.sky, 0.75) }}>
              {`${locale.minRequiredLevel} ${stage.minimumEntryLevel}`}
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} alignItems="center" justifyContent="flex-end" sx={{ width: '100%' }}>
            <Button
              variant={isSoloSelected ? 'contained' : 'outlined'}
              onClick={() => onSelect(stage.stageId, 'Solo')}
              disableRipple
              sx={{
                ...modeButtonSx,
                '&&': {
                  backgroundColor: isSoloSelected ? homePanelColors.band : withAlpha(palette.white, 0.9),
                  borderColor: isSoloSelected ? homePanelColors.band : palette.white,
                  color: isSoloSelected ? palette.white : homePanelColors.band,
                  boxShadow: 'none',
                  transition: 'none',
                },
              }}
            >
              {locale.modeSolo}
            </Button>
            {isEndless || isMirror ? null : (
              <Button
                variant={isMultiSelected ? 'contained' : 'outlined'}
                onClick={() => onSelect(stage.stageId, 'Multi')}
                disableRipple
                sx={{
                  ...modeButtonSx,
                  '&&': {
                    backgroundColor: isMultiSelected ? homePanelColors.band : withAlpha(palette.white, 0.9),
                    borderColor: isMultiSelected ? homePanelColors.band : palette.white,
                    color: isMultiSelected ? palette.white : homePanelColors.band,
                    boxShadow: 'none',
                    transition: 'none',
                  },
                }}
              >
                {locale.modeMulti}
              </Button>
            )}
          </Stack>
        </Stack>
      </Stack>
    </Card>
  )
}
