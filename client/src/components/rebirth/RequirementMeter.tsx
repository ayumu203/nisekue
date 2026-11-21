import CheckCircleRoundedIcon from '@mui/icons-material/CheckCircleRounded'
import { Box, LinearProgress, Stack, Typography } from '@mui/material'

type RequirementMeterProps = {
  current: number
  required: number
  /** 数値の前に付ける文字（例：Lv.） */
  prefix?: string
  /** 数値の後ろに付ける文字（例：Gold） */
  suffix?: string
  tone: 'blue' | 'gold'
}

const toneColors = {
  blue: {
    bar: 'linear-gradient(90deg, #8fd3ff 0%, #5fb6f5 100%)',
    metBorder: '#a8ddff',
    metBg: 'rgba(237, 249, 255, 0.95)',
    metText: '#20618f',
  },
  gold: {
    bar: 'linear-gradient(90deg, #ffe58a 0%, #f0c43c 100%)',
    metBorder: '#f0db7f',
    metBg: 'rgba(255, 251, 233, 0.95)',
    metText: '#8a6a00',
  },
} as const

const numberFormat = new Intl.NumberFormat('ja-JP')

export function RequirementMeter({ current, required, prefix = '', suffix = '', tone }: RequirementMeterProps) {
  const isMet = current >= required
  const colors = toneColors[tone]
  const progress = required > 0 ? Math.min(100, (current / required) * 100) : 100

  return (
    <Box
      sx={{
        px: 1.75,
        py: 1.25,
        borderRadius: 2.5,
        border: '1px solid',
        borderColor: isMet ? colors.metBorder : '#dbe7f2',
        bgcolor: isMet ? colors.metBg : 'rgba(246, 249, 252, 0.95)',
        boxShadow: isMet ? '0 6px 16px rgba(214, 186, 92, 0.14)' : 'none',
        transition: 'all 0.2s ease',
      }}
    >
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
        <Typography
          sx={{
            fontWeight: 800,
            fontVariantNumeric: 'tabular-nums',
            color: isMet ? colors.metText : '#4f6275',
            fontSize: { xs: '1rem', sm: '1.05rem' },
          }}
        >
          {prefix}
          {numberFormat.format(current)}
          <Box component="span" sx={{ color: '#90a2b4', fontWeight: 600, mx: 0.5 }}>
            /
          </Box>
          {numberFormat.format(required)}
          {suffix ? (
            <Box component="span" sx={{ ml: 0.5, fontSize: '0.85em', fontWeight: 700 }}>
              {suffix}
            </Box>
          ) : null}
        </Typography>
        <CheckCircleRoundedIcon
          fontSize="small"
          sx={{ color: isMet ? '#47b881' : '#d3dde7', transition: 'color 0.2s ease' }}
        />
      </Stack>
      <LinearProgress
        variant="determinate"
        value={progress}
        sx={{
          mt: 1,
          height: 6,
          borderRadius: 999,
          bgcolor: 'rgba(206, 220, 233, 0.6)',
          '& .MuiLinearProgress-bar': {
            borderRadius: 999,
            background: colors.bar,
          },
        }}
      />
    </Box>
  )
}
