import { Box, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { homePanelColors } from '@/constants/styles'

type HomePanelProps = {
  // 帯の中央に出す見出し
  title: ReactNode
  // 帯の右端に置く操作（チャットの切り替えなど）
  action?: ReactNode
  children: ReactNode
  'aria-label'?: string
  // 箱の高さを親に合わせ、中身が多いときは箱の中でスクロールする（PC）。false なら中身の高さに合わせる（スマホ）
  fillHeight?: boolean
}

// ホームで使う、上に緑の帯の見出しが付いた白い箱
export default function HomePanel({
  title,
  action,
  children,
  'aria-label': ariaLabel,
  fillHeight = true,
}: HomePanelProps) {
  return (
    <Box
      component="section"
      aria-label={ariaLabel}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: 0,
        height: fillHeight ? '100%' : 'auto',
        backgroundColor: '#ffffff',
        borderRadius: '12px',
        overflow: 'hidden',
      }}
    >
      <Box
        sx={{
          position: 'relative',
          flex: 'none',
          height: 44,
          px: 6,
          display: 'flex',
          alignItems: 'center',
          justifyContent: 'center',
          backgroundColor: homePanelColors.band,
          color: '#ffffff',
        }}
      >
        <Typography component="h2" noWrap sx={{ fontSize: '1rem', fontWeight: 700 }}>
          {title}
        </Typography>
        {action ? (
          <Box sx={{ position: 'absolute', right: 2, top: 0, bottom: 0, display: 'flex', alignItems: 'center' }}>
            {action}
          </Box>
        ) : null}
      </Box>
      <Box sx={{ flex: 1, minHeight: 0, overflowY: fillHeight ? 'auto' : 'visible', p: { xs: 2, sm: 2.5 } }}>
        {children}
      </Box>
    </Box>
  )
}
