import type { ReactNode } from 'react'
import { Box } from '@mui/material'
import { gamePageBackground } from '@/constants/styles'

type GamePageShellProps = {
  children: ReactNode
  /** 画面全体に塗る背景（色・グラデーション）。省略時はゲーム共通の黄色 */
  background?: string
}

/** ゲーム画面の外枠。背景を画面いっぱいに塗り、中身も横幅いっぱいに広げる */
export default function GamePageShell({ children, background = gamePageBackground }: GamePageShellProps) {
  return (
    <Box
      sx={{
        minHeight: '100dvh',
        width: '100%',
        boxSizing: 'border-box',
        background,
        px: { xs: 1.5, sm: 3, md: 4 },
        py: { xs: 1.5, sm: 3 },
      }}
    >
      {children}
    </Box>
  )
}
