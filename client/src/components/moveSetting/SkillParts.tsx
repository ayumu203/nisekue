import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { skillPanelColors as colors } from '@/constants/styles'
import { elementStyles, type MoveElementKey } from '@/components/moveSetting/moveDisplay'
import locale from '../../../locale/player-setting/PlayerSetting.json'
import { font, palette } from '@/theme/tokens'

// 属性ごとの白いシルエット（24×24）。単色の丸の上に置く
const elementIcons: Record<MoveElementKey, ReactNode> = {
  Strike: (
    <>
      <path d="M9.8 2.5h9.5v7.5H9.8z" transform="rotate(45 14.5 6.2)" />
      <path d="M11.1 11.6 3.4 19.3a1.7 1.7 0 0 0 2.4 2.4l7.7-7.7z" />
    </>
  ),
  Slash: (
    <>
      <path d="M3 15.5 17.5 2.5l-12 15.2z" />
      <path d="M4.5 21.5 21 7.5 7.2 23z" />
      <path d="M12 22.5l9.5-9.2-7.6 10.4z" />
    </>
  ),
  Pierce: (
    <>
      <path d="M22 2c-.5 4.3-2.4 7.6-5.3 9.7l-4.4-4.4C14.4 4.4 17.7 2.5 22 2z" />
      <path d="M11 8.6l4.4 4.4" stroke="currentColor" strokeWidth="2.6" strokeLinecap="round" />
      <path d="M13.2 10.8 3 21" stroke="currentColor" strokeWidth="2.6" strokeLinecap="round" />
    </>
  ),
  Fire: (
    <path d="M12.6 2c.4 3.4 5.9 6.1 5.9 12a6.5 6.5 0 0 1-13 0c0-3 1.6-5.1 3.4-6.4-.1 2.3.7 4 2.2 4.6-.6-3.9-.3-7.2 1.5-10.2z" />
  ),
  Water: <path d="M12 2.2c2.6 3.6 7 8.3 7 12.6a7 7 0 0 1-14 0C5 10.5 9.4 5.8 12 2.2z" />,
  Earth: <path d="M1.5 20.5 9 7.5l3.4 5.8 2.8-4.3 7.3 11.5z" />,
  Wind: (
    <g fill="none" stroke="currentColor" strokeWidth="2.8" strokeLinecap="round">
      <path d="M2.5 8.5h11a3.2 3.2 0 1 0-3.2-3.2" />
      <path d="M2.5 13h15.2a3.2 3.2 0 1 1-3.2 3.2" />
      <path d="M2.5 17.5h6" />
    </g>
  ),
  Holy: (
    <>
      <path d="M12 1.5 13.9 7.4 19.4 4.6 16.6 10.1 22.5 12 16.6 13.9 19.4 19.4 13.9 16.6 12 22.5 10.1 16.6 4.6 19.4 7.4 13.9 1.5 12 7.4 10.1 4.6 4.6 10.1 7.4z" />
      <circle cx="12" cy="12" r="2.6" fill={elementStyles.Holy.color} />
    </>
  ),
  None: (
    <path fillRule="evenodd" d="M12 3a9 9 0 1 1 0 18 9 9 0 0 1 0-18zm0 4.6a4.4 4.4 0 1 0 0 8.8 4.4 4.4 0 0 0 0-8.8z" />
  ),
  NoElement: <path d="M12 1.5 14.6 9.4 22.5 12 14.6 14.6 12 22.5 9.4 14.6 1.5 12 9.4 9.4z" />,
}

export function ElementEmblem({ element, size }: { element: MoveElementKey | null; size: number }) {
  if (!element) {
    // 未設定のスロットは点線の丸だけ
    return (
      <Box
        aria-hidden="true"
        sx={{ width: size, height: size, flexShrink: 0, borderRadius: '50%', border: `2px dashed ${colors.line}` }}
      />
    )
  }

  const iconSize = Math.round(size * 0.58)
  return (
    <Box
      aria-hidden="true"
      sx={{
        width: size,
        height: size,
        flexShrink: 0,
        borderRadius: '50%',
        backgroundColor: elementStyles[element].color,
        color: palette.white,
        display: 'grid',
        placeItems: 'center',
      }}
    >
      <svg width={iconSize} height={iconSize} viewBox="0 0 24 24" fill="currentColor">
        {elementIcons[element]}
      </svg>
    </Box>
  )
}

// 消費MP：水色の結晶と数字
export function MpCost({ mp, size, fontSize }: { mp: number; size: number; fontSize: number }) {
  return (
    <Box
      component="span"
      aria-label={locale.mpCostAria.replace('{{mp}}', String(mp))}
      sx={{
        display: 'inline-flex',
        alignItems: 'center',
        gap: `${Math.max(4, Math.round(size / 6))}px`,
        flexShrink: 0,
      }}
    >
      <svg width={size} height={size} viewBox="0 0 24 24" aria-hidden="true">
        <path d="M7 3h10l5 6-10 13L2 9z" fill={colors.gem} />
        <path
          d="M2 9h20M7 3l5 6 5-6M12 9v13"
          fill="none"
          stroke={colors.paper}
          strokeWidth="1.2"
          strokeLinejoin="round"
        />
      </svg>
      <Box component="span" aria-hidden="true" sx={{ fontSize, color: colors.mpText, lineHeight: 1 }}>
        {mp}
      </Box>
    </Box>
  )
}

// 種類・属性の小さい札
export function SkillChip({ label, color, outlined = false }: { label: string; color: string; outlined?: boolean }) {
  return (
    <Typography
      component="span"
      sx={{
        display: 'inline-block',
        fontSize: font.sm,
        lineHeight: 1.4,
        px: 1,
        py: 0.25,
        borderRadius: '3px',
        whiteSpace: 'nowrap',
        color: outlined ? colors.sub : palette.white,
        backgroundColor: outlined ? 'transparent' : color,
        border: outlined ? `1px solid ${colors.line}` : 'none',
      }}
    >
      {label}
    </Typography>
  )
}
