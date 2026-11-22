import type { ReactNode } from 'react'
import { Box } from '@mui/material'
import { gamePageBackground } from '@/constants/styles'

type GamePageShellProps = {
  children: ReactNode
  /** 画面全体に塗る背景（色・グラデーション）。省略時はゲーム共通の黄色 */
  background?: string
  /** false にすると内側の余白をなくす（画面の中を場所ごとに塗り分けるページ用） */
  padded?: boolean
}

/** ゲーム画面の外枠。背景を画面いっぱいに塗り、中身も横幅いっぱいに広げる */
export default function GamePageShell({
  children,
  background = gamePageBackground,
  padded = true,
}: GamePageShellProps) {
  return (
    <Box
      sx={{
        minHeight: '100dvh',
        width: '100%',
        boxSizing: 'border-box',
        background,
        display: 'flex',
        flexDirection: 'column',
        ...(padded ? { px: { xs: 1.5, sm: 3, md: 4 }, py: { xs: 1.5, sm: 3 } } : {}),
      }}
    >
      {children}
    </Box>
  )
}
