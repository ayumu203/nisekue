import { Box, useMediaQuery, useTheme } from '@mui/material'
import { useState, type ReactNode } from 'react'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey } from '@/components/home/homeMenu'
import { useAuth } from '@/contexts/useAuth'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import locale from '../../../locale/home/Home.json'

type GamePageFrameProps = {
  // ヘッダーメニューで今いる画面として示す項目（homeMenu の key）
  currentKey: string
  // ヘッダーメニューで今いる画面を塗る色
  currentColor: string
  // 画面全体の背景（今までの外枠の色やグラデーション）
  background: string
  // PC（md 以上）で中身を画面の高さに合わせる（箱の中でスクロールさせる画面用）。中身は縦の flex に並ぶ
  fillHeight?: boolean
  children: ReactNode
}

// 画面いっぱいに広げた外枠。PC（sm 以上）でログインしているときは上にホームと同じヘッダーメニューを出す。
// 中身の見た目は各画面のまま
export default function GamePageFrame({
  currentKey,
  currentColor,
  background,
  fillHeight = false,
  children,
}: GamePageFrameProps) {
  const { session } = useAuth()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)

  const homeMenu = buildHomeMenu({
    userId: session?.user.id ?? null,
    tutorialStep: null,
    showRebirthTutorial: false,
    showSurveyGuide: false,
    onSurveyClick: () => {},
    canUseDebug,
  })

  return (
    <Box sx={{ minHeight: '100dvh', display: 'flex', flexDirection: 'column', background }}>
      {isDesktop && session ? (
        <HomeHeaderMenu
          primary={[{ key: 'home', label: locale.backToHome, icon: <HomeIcon />, to: '/' }, ...homeMenu.primary]}
          groups={homeMenu.groups}
          openGroup={openMenuGroup}
          lockedGroup={null}
          onToggleGroup={(key) => setOpenMenuGroup((current) => (current === key ? null : key))}
          onCloseGroup={() => setOpenMenuGroup(null)}
          currentKey={currentKey}
          currentColor={currentColor}
        />
      ) : null}
      <Box
        sx={{
          flex: 1,
          boxSizing: 'border-box',
          px: { xs: 1.5, sm: 4 },
          pt: { xs: 1.5, sm: 3 },
          pb: { xs: 3, sm: 4 },
          ...(fillHeight
            ? {
                // 低すぎる画面ではページごとスクロール
                height: { md: 'calc(100dvh - 57px)' },
                minHeight: { md: 640 },
                display: 'flex',
                flexDirection: 'column',
              }
            : {}),
        }}
      >
        {children}
      </Box>
    </Box>
  )
}
