import { Box, LinearProgress, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { jobChangePanelColors as colors } from '@/constants/styles'
import { font, palette } from '@/theme/tokens'

// 舞台の上の札（現在の職業・MASTER・未解放）
export function PlateChip({ children }: { children: ReactNode }) {
  return (
    <Box
      component="span"
      sx={{
        fontSize: font.sm,
        lineHeight: 1.5,
        color: colors.plateGold,
        backgroundColor: colors.plate,
        border: `2px solid ${colors.plateGold}`,
        borderRadius: '4px',
        px: 1,
        whiteSpace: 'nowrap',
      }}
    >
      {children}
    </Box>
  )
}

// 白い箱の中の札。gold：マスター済み・解放可能、band：解放済み、mute：なし
export function InfoChip({
  children,
  tone = 'soft',
}: {
  children: ReactNode
  tone?: 'soft' | 'gold' | 'band' | 'mute'
}) {
  const toneSx = {
    soft: { color: colors.bandDark, backgroundColor: colors.soft },
    gold: { color: colors.goldText, backgroundColor: colors.goldSoft, border: `1px solid ${colors.gold}` },
    band: { color: palette.white, backgroundColor: colors.band },
    mute: { color: colors.label, backgroundColor: colors.locked },
  }[tone]

  return (
    <Box
      component="span"
      sx={{ fontSize: font.sm, lineHeight: 1.6, borderRadius: 999, px: 1.5, whiteSpace: 'nowrap', ...toneSx }}
    >
      {children}
    </Box>
  )
}

// 点線で区切った「項目名と値」の 1 行
export function InfoRow({ label, value, strong = false }: { label: ReactNode; value: ReactNode; strong?: boolean }) {
  return (
    <Box sx={{ display: 'flex', justifyContent: 'space-between', alignItems: 'baseline', gap: 1 }}>
      <Typography component="span" sx={{ fontSize: font.md, color: colors.label }}>
        {label}
      </Typography>
      <Typography
        component="span"
        sx={{ fontSize: strong ? font.xl : font.md, color: colors.text, whiteSpace: 'nowrap', textAlign: 'right' }}
      >
        {value}
      </Typography>
    </Box>
  )
}

// 見出し付きの区切り（詳細の欄の中）
export function InfoSection({ title, children }: { title: ReactNode; children: ReactNode }) {
  return (
    <Box
      sx={{
        display: 'flex',
        flexDirection: 'column',
        gap: 1.5,
        borderTop: `1px dotted ${colors.divider}`,
        pt: 2,
      }}
    >
      <Typography component="h3" sx={{ m: 0, fontSize: font.md, color: colors.label }}>
        {title}
      </Typography>
      {children}
    </Box>
  )
}

export function ExpBar({ ratio, label }: { ratio: number; label: string }) {
  return (
    <LinearProgress
      variant="determinate"
      value={ratio}
      aria-label={label}
      sx={{
        height: 10,
        borderRadius: 999,
        backgroundColor: palette.cream,
        '& .MuiLinearProgress-bar': { backgroundColor: colors.gold, borderRadius: 999 },
      }}
    />
  )
}
