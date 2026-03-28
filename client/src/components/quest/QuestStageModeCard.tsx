import { Box, Button, Card, Stack, Typography } from '@mui/material'
import { innerSurfaceSx } from '@/constants/styles'
import { resolvePublicAssetPath } from '@/lib/assets'
import type { CreateQuestRoomRequest, GetQuestStagesResponse } from '@/schema/quest'
import locale from '../../../locale/quest/QuestRoom.json'

type QuestStageModeCardProps = {
  stage: GetQuestStagesResponse[number]
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

export default function QuestStageModeCard({ stage, isSelected, selectedMode, onSelect }: QuestStageModeCardProps) {
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
        borderWidth: isSelected ? 2 : 1,
        color: '#f4f7ff',
        backgroundColor: '#243654',
        borderColor: isSelected ? '#90b4ea' : 'rgba(152, 192, 255, 0.26)',
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
          {stage.previewEnemyImagePath ? (
            <Box
              component="img"
              src={resolvePublicAssetPath(stage.previewEnemyImagePath)}
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
            <Typography variant="h6" sx={{ lineHeight: 1.2, fontWeight: 900, color: '#ffffff' }}>
              {stage.name}
            </Typography>
            <Typography variant="body2" sx={{ color: 'rgba(222, 236, 255, 0.78)' }}>
              {`${locale.recommendedLevel} ${stage.recommendedLevel}`}
            </Typography>
          </Stack>

          <Stack direction="row" spacing={1} justifyContent="flex-end" sx={{ width: '100%' }}>
            <Button
              variant={isSoloSelected ? 'contained' : 'outlined'}
              onClick={() => onSelect(stage.stageId, 'Solo')}
              disableRipple
              sx={{
                ...modeButtonSx,
                '&&': {
                  backgroundColor: isSoloSelected ? '#7ab4ff' : 'rgba(240, 246, 255, 0.92)',
                  borderColor: '#7ab4ff',
                  color: isSoloSelected ? '#13253f' : '#365a90',
                  boxShadow: 'none',
                  transition: 'none',
                },
              }}
            >
              {locale.modeSolo}
            </Button>
            <Button
              variant={isMultiSelected ? 'contained' : 'outlined'}
              onClick={() => onSelect(stage.stageId, 'Multi')}
              disableRipple
              sx={{
                ...modeButtonSx,
                '&&': {
                  backgroundColor: isMultiSelected ? '#9ed8ce' : 'rgba(239, 252, 248, 0.92)',
                  borderColor: '#9ed8ce',
                  color: isMultiSelected ? '#173a34' : '#2e6e64',
                  boxShadow: 'none',
                  transition: 'none',
                },
              }}
            >
              {locale.modeMulti}
            </Button>
          </Stack>
        </Stack>
      </Stack>
    </Card>
  )
}
