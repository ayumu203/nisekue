import { Box, Chip, Radio, Stack, Typography } from '@mui/material'
import { trainingPanelColors } from '@/constants/styles'
import {
  getTrainingBackgroundPath,
  trainingLevelChipSx,
  trainingSelectCardSx,
  trainingSelectRadioSx,
} from './trainingSelectStyles'
import { font, palette } from '@/theme/tokens'

type TrainingSelectCardProps = {
  id?: string
  radioName: string
  name: string
  imageSrc: string | null
  levelLabel: string
  // PLAYER のときだけ職業を出す
  subLabel?: string
  backgroundKey: string | number
  isSelected: boolean
  isDisabled: boolean
  // 戦闘結果の下に並べるときは画像と文字を小さくする
  compact?: boolean
  onSelect: () => void
}

// 訓練相手のカード。背景画像の上に相手の全身を出し、下に推奨レベルと名前を並べる（NPC と PLAYER で共通）
export default function TrainingSelectCard({
  id,
  radioName,
  name,
  imageSrc,
  levelLabel,
  subLabel,
  backgroundKey,
  isSelected,
  isDisabled,
  compact = false,
  onSelect,
}: TrainingSelectCardProps) {
  const imageHeight = compact ? 104 : 168

  return (
    <Box component="label" id={id} sx={trainingSelectCardSx(isSelected, isDisabled)}>
      <Radio
        name={radioName}
        checked={isSelected}
        disabled={isDisabled}
        onChange={onSelect}
        inputProps={{ 'aria-label': name }}
        size="small"
        sx={trainingSelectRadioSx}
      />
      <Box
        sx={{
          height: imageHeight,
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: palette.sky,
          backgroundImage: `url(${getTrainingBackgroundPath(backgroundKey)})`,
          backgroundSize: 'cover',
          backgroundPosition: 'center',
        }}
      >
        {imageSrc ? (
          <Box
            component="img"
            src={imageSrc}
            alt=""
            sx={{ maxHeight: imageHeight - 12, maxWidth: '86%', objectFit: 'contain' }}
          />
        ) : null}
      </Box>
      <Stack
        spacing={compact ? 0.5 : 0.75}
        alignItems="center"
        sx={{ px: 1, pt: compact ? 1 : 1.5, pb: compact ? 1.5 : 2 }}
      >
        <Chip label={levelLabel} size="small" sx={trainingLevelChipSx} />
        <Typography
          fontWeight={800}
          textAlign="center"
          sx={{
            color: trainingPanelColors.text,
            fontSize: compact ? font.md : font.md,
            lineHeight: 1.3,
            overflowWrap: 'anywhere',
          }}
        >
          {name}
        </Typography>
        {subLabel ? (
          <Typography variant="caption" sx={{ color: trainingPanelColors.label }}>
            {subLabel}
          </Typography>
        ) : null}
      </Stack>
    </Box>
  )
}
