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

export default function QuestStageModeCard({
  stage,
  isSelected,
  selectedMode,
  onSelect,
}: QuestStageModeCardProps) {
  const isSoloSelected = isSelected && selectedMode === 'Solo'
  const isMultiSelected = isSelected && selectedMode === 'Multi'

  return (
    <Card
      variant="outlined"
      sx={{
        ...innerSurfaceSx,
        height: '100%',
        borderRadius: 3,
        borderWidth: isSelected ? 2 : 1,
        borderColor: isSelected ? '#c58f2b' : innerSurfaceSx.borderColor,
        boxShadow: isSelected ? '0 10px 24px rgba(120, 88, 32, 0.14)' : 'none',
      }}
    >
      <Stack
        direction={{ xs: 'column', sm: 'row' }}
        spacing={2}
        alignItems={{ xs: 'flex-start', sm: 'center' }}
        sx={{ p: 2 }}
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
            background: 'radial-gradient(circle at 30% 30%, #fff6de 0%, #f0d5a0 100%)',
            border: '2px solid #e2bf7b',
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
            <Typography variant="h6" sx={{ lineHeight: 1.2 }}>
              {stage.name}
            </Typography>
            <Typography variant="body2" color="text.secondary">
              {`${locale.recommendedLevel} ${stage.recommendedLevel}`}
            </Typography>
          </Stack>

          <Stack
            direction="row"
            spacing={1}
            justifyContent="flex-end"
            sx={{ width: '100%' }}
          >
            <Button
              variant={isSoloSelected ? 'contained' : 'outlined'}
              onClick={() => onSelect(stage.stageId, 'Solo')}
              disableRipple
              sx={{
                ...modeButtonSx,
                '&&': {
                  backgroundColor: isSoloSelected ? '#3A66D6' : '#EEF3FF',
                  borderColor: '#3A66D6',
                  color: isSoloSelected ? '#ffffff' : '#3A66D6',
                  boxShadow: isSoloSelected ? '0 4px 12px rgba(58, 102, 214, 0.18)' : 'none',
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
                  backgroundColor: isMultiSelected ? '#C9485B' : '#FFF0F3',
                  borderColor: '#C9485B',
                  color: isMultiSelected ? '#ffffff' : '#C9485B',
                  boxShadow: isMultiSelected ? '0 4px 12px rgba(201, 72, 91, 0.18)' : 'none',
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
