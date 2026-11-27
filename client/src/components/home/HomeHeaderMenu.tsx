import { Box, Button, ClickAwayListener, IconButton, SvgIcon, type SvgIconProps } from '@mui/material'
import { Link } from 'react-router-dom'
import type { HomeMenuGroup, HomeMenuGroupKey, HomeMenuItem } from '@/components/home/homeMenu'
import { GearIcon, SignOutIcon } from '@/components/home/StatusIcons'
import { topNavigationIconButtonSx } from '@/constants/styles'
import { useSignOut } from '@/hooks/useSignOut'
import homeLocale from '../../../locale/home/Home.json'
import { font, palette, withAlpha } from '@/theme/tokens'

type HomeHeaderMenuProps = {
  primary: HomeMenuItem[]
  groups: HomeMenuGroup[]
  openGroup: HomeMenuGroupKey | null
  // チュートリアルで開いたままにしているグループ。これがある間は閉じない
  lockedGroup: HomeMenuGroupKey | null
  onToggleGroup: (key: HomeMenuGroupKey) => void
  onCloseGroup: () => void
  gearIconId?: string
  onGearClick?: () => void
  // 今いる画面の項目（ホーム以外の画面で使う）。その項目（グループの中ならグループ）を帯の色で塗って示す
  currentKey?: string
  currentColor?: string
}

function ChevronDownIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z" />
    </SvgIcon>
  )
}

const BAR_TEXT_COLOR = palette.brown900
const BAR_DIVIDER_COLOR = withAlpha(palette.brown900, 0.25)

// 白いバーにくっつけて並べるメニューの項目。項目どうしは細い縦線で区切る
const headerButtonSx = {
  minHeight: 56,
  px: 2.5,
  borderRadius: 0,
  borderRight: `1px solid ${BAR_DIVIDER_COLOR}`,
  color: BAR_TEXT_COLOR,
  whiteSpace: 'nowrap',
  fontWeight: 700,
  fontSize: font.md,
  // ホバーや押したときの色の変化・波紋のアニメーションは付けない
  transition: 'none',
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
  '&[aria-expanded="true"]': {
    backgroundColor: withAlpha(palette.brown900, 0.1),
  },
  '& .MuiButton-startIcon': {
    color: palette.green400,
  },
  // 16px の文字に合わせてアイコンを 22px にする
  '& .MuiButton-startIcon > *:nth-of-type(1), & .MuiButton-endIcon > *:nth-of-type(1)': {
    fontSize: font.xxl,
  },
} as const

// 今いる画面の項目。帯の色で塗り、文字とアイコンを白にする
function currentHeaderButtonSx(color: string) {
  return {
    ...headerButtonSx,
    color: palette.white,
    backgroundColor: color,
    '&[aria-expanded="true"]': {
      backgroundColor: color,
    },
    '& .MuiButton-startIcon': {
      color: palette.white,
    },
  } as const
}

const dropdownItemSx = {
  justifyContent: 'flex-start',
  minHeight: 44,
  px: 2.5,
  borderRadius: 0,
  color: palette.brown900,
  fontWeight: 700,
  whiteSpace: 'nowrap',
  transition: 'none',
  '& .MuiTouchRipple-root': {
    display: 'none',
  },
} as const

function itemLinkProps(item: HomeMenuItem) {
  if (item.to) {
    return { component: Link, to: item.to }
  }

  if (item.href) {
    return { component: 'a', href: item.href, target: '_blank', rel: 'noopener noreferrer' }
  }

  return { disabled: item.disabled }
}

// PC のホームで使う、上部のヘッダーメニュー
export default function HomeHeaderMenu({
  primary,
  groups,
  openGroup,
  lockedGroup,
  onToggleGroup,
  onCloseGroup,
  gearIconId,
  onGearClick,
  currentKey,
  currentColor,
}: HomeHeaderMenuProps) {
  const { signOut, isSigningOut } = useSignOut()
  const visibleGroup = lockedGroup ?? openGroup

  return (
    <Box>
      {/* 画面の上端と左右の端にくっつけた白いバー */}
      <Box
        sx={{
          backgroundColor: palette.white,
          borderBottom: `1px solid ${BAR_DIVIDER_COLOR}`,
          display: 'flex',
          alignItems: 'stretch',
          minHeight: 56,
        }}
      >
        <Box component="nav" sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'stretch', flex: '1 1 auto' }}>
          {primary.map((item) => (
            <Button
              key={item.key}
              {...itemLinkProps(item)}
              id={item.id}
              startIcon={item.icon}
              onClick={item.onClick}
              aria-current={item.key === currentKey ? 'page' : undefined}
              sx={item.key === currentKey && currentColor ? currentHeaderButtonSx(currentColor) : headerButtonSx}
            >
              {/* 「ホーム」は画面ごとに文言が違うと幅が変わってほかの項目がずれるので、ここでそろえる */}
              {item.key === 'home' ? homeLocale.homeMenuLabel : item.label}
            </Button>
          ))}
          {groups.map((group) => {
            const isOpen = visibleGroup === group.key
            // 今いる画面がグループの中の項目なら、グループのボタンを帯の色で塗る
            const isCurrentGroup = currentKey != null && group.items.some((item) => item.key === currentKey)

            return (
              <ClickAwayListener
                key={group.key}
                onClickAway={() => {
                  if (isOpen && !lockedGroup) {
                    onCloseGroup()
                  }
                }}
              >
                <Box
                  sx={{ position: 'relative', display: 'flex' }}
                  onKeyDown={(event) => {
                    if (event.key === 'Escape' && isOpen && !lockedGroup) {
                      onCloseGroup()
                    }
                  }}
                >
                  <Button
                    startIcon={group.icon}
                    endIcon={<ChevronDownIcon />}
                    aria-expanded={isOpen}
                    aria-haspopup="true"
                    onClick={() => {
                      if (!lockedGroup) {
                        onToggleGroup(group.key)
                      }
                    }}
                    sx={isCurrentGroup && currentColor ? currentHeaderButtonSx(currentColor) : headerButtonSx}
                  >
                    {group.label}
                  </Button>
                  {/* チュートリアルのスポットライトが位置を測れるように、Popper ではなく通常の要素として描画する */}
                  {isOpen && (
                    <Box
                      sx={{
                        position: 'absolute',
                        top: 'calc(100% + 4px)',
                        left: 0,
                        zIndex: 1200,
                        minWidth: 180,
                        py: 0.5,
                        display: 'flex',
                        flexDirection: 'column',
                        backgroundColor: palette.white,
                        borderRadius: '10px',
                        boxShadow: `0 4px 14px ${withAlpha(palette.brown900, 0.1)}`,
                      }}
                    >
                      {group.items.map((item) => (
                        <Button
                          key={item.key}
                          {...itemLinkProps(item)}
                          id={item.id}
                          onClick={() => {
                            item.onClick?.()
                            if (!lockedGroup) {
                              onCloseGroup()
                            }
                          }}
                          sx={dropdownItemSx}
                        >
                          {item.label}
                        </Button>
                      ))}
                    </Box>
                  )}
                </Box>
              </ClickAwayListener>
            )
          })}
        </Box>
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1, px: 2 }}>
          <IconButton
            id={gearIconId}
            component={Link}
            to="/player-setting"
            aria-label="プレイヤー設定へ移動"
            onClick={onGearClick}
            sx={topNavigationIconButtonSx}
          >
            <GearIcon />
          </IconButton>
          <IconButton
            onClick={() => void signOut()}
            disabled={isSigningOut}
            aria-label="ログアウト"
            sx={{
              ...topNavigationIconButtonSx,
              '&.Mui-disabled': {
                color: withAlpha(palette.white, 0.5),
                backgroundColor: withAlpha(palette.brown500, 0.5),
              },
            }}
          >
            <SignOutIcon />
          </IconButton>
        </Box>
      </Box>
    </Box>
  )
}
