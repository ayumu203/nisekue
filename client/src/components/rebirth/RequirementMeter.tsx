import AutoAwesomeRoundedIcon from '@mui/icons-material/AutoAwesomeRounded'
import { Box, LinearProgress, Stack, Typography } from '@mui/material'
import { font, palette, withAlpha } from '@/theme/tokens'

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
    bar: `linear-gradient(90deg, ${palette.blue300} 0%, ${palette.blue300} 100%)`,
    metBorder: palette.blue300,
    metBg: palette.sky,
    metText: palette.teal700,
    icon: palette.blue300,
  },
  gold: {
    bar: `linear-gradient(90deg, ${palette.gold300} 0%, ${palette.yellow} 100%)`,
    metBorder: palette.gold300,
    metBg: palette.cream,
    metText: palette.gold700,
    icon: palette.yellow,
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
        px: 2,
        py: 1.5,
        borderRadius: 2.5,
        border: '1px solid',
        borderColor: isMet ? colors.metBorder : palette.sky,
        bgcolor: isMet ? colors.metBg : palette.white,
        boxShadow: isMet ? `0 6px 16px ${withAlpha(palette.gold300, 0.1)}` : 'none',
        transition: 'all 0.2s ease',
      }}
    >
      <Stack direction="row" alignItems="center" justifyContent="space-between" spacing={1}>
        <Typography
          sx={{
            fontWeight: 800,
            fontVariantNumeric: 'tabular-nums',
            color: isMet ? colors.metText : palette.teal700,
            fontSize: { xs: font.md, sm: font.md },
          }}
        >
          {prefix}
          {numberFormat.format(current)}
          <Box component="span" sx={{ color: palette.blue300, fontWeight: 600, mx: 0.5 }}>
            /
          </Box>
          {numberFormat.format(required)}
          {suffix ? (
            <Box component="span" sx={{ ml: 0.5, fontSize: '0.85em', fontWeight: 700 }}>
              {suffix}
            </Box>
          ) : null}
        </Typography>
        {isMet ? <AutoAwesomeRoundedIcon fontSize="small" sx={{ color: colors.icon }} /> : null}
      </Stack>
      <LinearProgress
        variant="determinate"
        value={progress}
        sx={{
          mt: 1,
          height: 6,
          borderRadius: 999,
          bgcolor: withAlpha(palette.sky, 0.5),
          '& .MuiLinearProgress-bar': {
            borderRadius: 999,
            background: colors.bar,
          },
        }}
      />
    </Box>
  )
}
