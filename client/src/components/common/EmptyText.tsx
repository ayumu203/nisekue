import { Typography } from '@mui/material'
import type { ReactNode } from 'react'

/** 一覧が空のときの文言。周りの文字色を引き継ぎ、少し薄くする */
export function EmptyText({ children }: { children: ReactNode }) {
  return (
    <Typography variant="body2" sx={{ color: 'inherit', opacity: 0.75, py: 1 }}>
      {children}
    </Typography>
  )
}
