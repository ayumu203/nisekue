import { Box, ButtonBase, Typography } from '@mui/material'
import type { ReactNode } from 'react'
import { homePanelColors } from '@/constants/styles'

export type GamePanelTab = {
  key: string
  label: ReactNode
  selected: boolean
  onClick: () => void
}

type GamePanelProps = {
  // 帯の中央に出す見出し（tabs を渡したときは使わない）
  title?: ReactNode
  // 帯を左右に分けて並べる切り替え（「作成｜募集中」など）。渡すと見出しの代わりに出す
  tabs?: GamePanelTab[]
  // 帯の右端に置く操作（チャットの切り替え・スマホのホームボタンなど）
  action?: ReactNode
  // 帯の左端に置く操作（スマホのホームボタンなど。見出しの帯でも、切り替えの帯でも使える）
  startAction?: ReactNode
  children: ReactNode
  'aria-label'?: string
  // 箱の下の影の色。画面ごとの地の色に合わせる
  shadowColor?: string
  // 中身の余白をなくす（バトル画面のように端まで使うとき）
  disablePadding?: boolean
  // 箱の高さを親に合わせ、中身が多いときは箱の中でスクロールする（PC）。false なら中身の高さに合わせる（スマホ）
  fillHeight?: boolean
  // fillHeight のとき、中身が少なければ箱を中身の高さまで縮める（白い面が画面いっぱいに広がらないように）
  fitContent?: boolean
}

// 上に緑の帯の見出しが付いた白い箱。PC のホームとクエストで使う
export default function GamePanel({
  title,
  tabs,
  action,
  startAction,
  children,
  'aria-label': ariaLabel,
  shadowColor = homePanelColors.shadow,
  disablePadding = false,
  fillHeight = true,
  fitContent = false,
}: GamePanelProps) {
  return (
    <Box
      component="section"
      aria-label={ariaLabel}
      sx={{
        display: 'flex',
        flexDirection: 'column',
        minHeight: 0,
        height: fillHeight && !fitContent ? '100%' : 'auto',
        maxHeight: fillHeight ? '100%' : 'none',
        backgroundColor: '#ffffff',
        borderRadius: '12px',
        boxShadow: `0 2px 0 ${shadowColor}`,
        overflow: 'hidden',
      }}
    >
      {tabs ? (
        <Box role="tablist" sx={{ flex: 'none', height: 44, display: 'flex' }}>
          {startAction ? (
            <Box
              sx={{
                flex: 'none',
                display: 'flex',
                alignItems: 'center',
                backgroundColor: homePanelColors.band,
                // ホームボタンと帯の中身の区切りをはっきり見せる白い線
                borderRight: '2px solid rgba(255, 255, 255, 0.85)',
              }}
            >
              {startAction}
            </Box>
          ) : null}
          {tabs.map((tab, index) => (
            <ButtonBase
              key={tab.key}
              role="tab"
              disableRipple
              aria-selected={tab.selected}
              onClick={tab.onClick}
              sx={{
                flex: 1,
                fontFamily: 'inherit',
                fontSize: '1rem',
                fontWeight: 700,
                color: tab.selected ? '#ffffff' : 'rgba(255, 255, 255, 0.82)',
                backgroundColor: tab.selected ? homePanelColors.bandSelected : homePanelColors.band,
                borderLeft: index === 0 || startAction ? 'none' : '1px solid rgba(255, 255, 255, 0.35)',
              }}
            >
              {tab.label}
            </ButtonBase>
          ))}
          {action ? (
            <Box
              sx={{
                flex: 'none',
                display: 'flex',
                alignItems: 'center',
                backgroundColor: homePanelColors.band,
                borderLeft: '1px solid rgba(255, 255, 255, 0.35)',
              }}
            >
              {action}
            </Box>
          ) : null}
        </Box>
      ) : (
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
          {startAction ? (
            <Box
              sx={{
                position: 'absolute',
                left: 0,
                top: 0,
                bottom: 0,
                display: 'flex',
                alignItems: 'center',
                borderRight: '2px solid rgba(255, 255, 255, 0.85)',
              }}
            >
              {startAction}
            </Box>
          ) : null}
          {action ? (
            <Box sx={{ position: 'absolute', right: 2, top: 0, bottom: 0, display: 'flex', alignItems: 'center' }}>
              {action}
            </Box>
          ) : null}
        </Box>
      )}
      <Box
        sx={{
          flex: 1,
          minHeight: 0,
          overflowY: fillHeight ? 'auto' : 'visible',
          p: disablePadding ? 0 : { xs: 2, sm: 2.5 },
          display: disablePadding ? 'flex' : 'block',
          flexDirection: 'column',
        }}
      >
        {children}
      </Box>
    </Box>
  )
}
