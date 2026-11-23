import { Box, Button, Card, Stack, Typography } from '@mui/material'
import { homePanelColors, innerSurfaceSx } from '@/constants/styles'
import { resolveCharacterAssetPath, resolvePublicAssetPath } from '@/lib/assets'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import locale from '../../../locale/quest/QuestRoom.json'

type QuestStageModeCardProps = {
  stage: GetQuestStagesResponse[number]
  // 名鑑の祠ではステージのアイコンに自分のキャラ画像を出す。
  playerImagePath?: string | null
  isSelected: boolean
  selectedMode: CreateQuestRoomRequest['mode']
  onSelect: (stageId: number, mode: CreateQuestRoomRequest['mode']) => void
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
        color: '#f4f7ff',
        backgroundColor: '#243654',
        // 選んでいるステージは黄色の枠で示す
        borderColor: isSelected ? '#ffcc00' : 'rgba(152, 192, 255, 0.26)',
        boxShadow: isSelected ? '0 10px 24px rgba(22, 39, 73, 0.22)' : 'none',
        '&::before': {
          content: '""',
          position: 'absolute',
          inset: 0,
          backgroundImage: `linear-gradient(135deg, rgba(18, 29, 48, 0.74), rgba(32, 49, 80, 0.66)), url(${battlefieldImageSrc})`,
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
            background: 'radial-gradient(circle at 30% 30%, #eef4ff 0%, #9cbbe6 100%)',
            border: '2px solid #a9c3eb',
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
              <Typography variant="h6" sx={{ lineHeight: 1.2, fontWeight: 900, color: '#ffffff' }}>
                {stage.name}
              </Typography>
              {isEndless ? (
                <Box
                  component="span"
                  sx={{
                    px: 1,
                    py: 0.25,
                    borderRadius: 999,
                    fontSize: '0.7rem',
                    fontWeight: 800,
                    color: '#1b2c4a',
                    backgroundColor: '#ffd66e',
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
                    fontSize: '0.7rem',
                    fontWeight: 800,
                    color: '#f4f7ff',
                    backgroundColor: '#6b4fa8',
                    letterSpacing: '0.04em',
                  }}
                >
                  {locale.mirror.badge}
                </Box>
              ) : null}
            </Stack>
            <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.78)' }}>
              {`${locale.recommendedLevel} ${isMirror ? locale.mirror.hiddenRecommendedLevel : stage.recommendedLevel}`}
            </Typography>
            <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.78)' }}>
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
                  backgroundColor: isSoloSelected ? homePanelColors.band : 'rgba(255, 255, 255, 0.92)',
                  borderColor: isSoloSelected ? homePanelColors.band : '#ffffff',
                  color: isSoloSelected ? '#ffffff' : homePanelColors.band,
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
                    backgroundColor: isMultiSelected ? homePanelColors.band : 'rgba(255, 255, 255, 0.92)',
                    borderColor: isMultiSelected ? homePanelColors.band : '#ffffff',
                    color: isMultiSelected ? '#ffffff' : homePanelColors.band,
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
