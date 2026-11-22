import { Alert, Box, Button, ClickAwayListener, IconButton, Paper, SvgIcon, type SvgIconProps } from '@mui/material'
import { Link } from 'react-router-dom'
import type { HomeMenuGroup, HomeMenuGroupKey, HomeMenuItem } from '@/components/home/homeMenu'
import { GearIcon, SignOutIcon } from '@/components/home/StatusIcons'
import { innerSurfaceSx, softGreenButtonSx, topNavigationIconButtonSx } from '@/constants/styles'
import { useSignOut } from '@/hooks/useSignOut'

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
}

function ChevronDownIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z" />
    </SvgIcon>
  )
}

const headerButtonSx = {
  ...softGreenButtonSx,
  minHeight: 44,
  px: 2,
  whiteSpace: 'nowrap',
  fontWeight: 700,
} as const

const dropdownItemSx = {
  justifyContent: 'flex-start',
  minHeight: 44,
  px: 2.25,
  borderRadius: 0,
  color: '#3a2f1e',
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '&:hover': {
    backgroundColor: '#fff4d6',
  },
} as const

function itemLinkProps(item: HomeMenuItem) {
  return item.to
    ? { component: Link, to: item.to }
    : { component: 'a', href: item.href, target: '_blank', rel: 'noopener noreferrer' }
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
}: HomeHeaderMenuProps) {
  const { signOut, isSigningOut, signOutError } = useSignOut()
  const visibleGroup = lockedGroup ?? openGroup

  return (
    <Box>
      <Paper
        variant="outlined"
        sx={{
          ...innerSurfaceSx,
          borderRadius: 3,
          px: 1.5,
          py: 1.25,
          display: 'flex',
          flexWrap: 'wrap',
          alignItems: 'center',
          gap: 1,
        }}
      >
        <Box component="nav" sx={{ display: 'flex', flexWrap: 'wrap', alignItems: 'center', gap: 1, flex: '1 1 auto' }}>
          {primary.map((item) => (
            <Button
              key={item.key}
              {...itemLinkProps(item)}
              id={item.id}
              variant="contained"
              startIcon={item.icon}
              onClick={item.onClick}
              sx={headerButtonSx}
            >
              {item.label}
            </Button>
          ))}
          {groups.map((group) => {
            const isOpen = visibleGroup === group.key

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
                  sx={{ position: 'relative' }}
                  onKeyDown={(event) => {
                    if (event.key === 'Escape' && isOpen && !lockedGroup) {
                      onCloseGroup()
                    }
                  }}
                >
                  <Button
                    variant="contained"
                    startIcon={group.icon}
                    endIcon={<ChevronDownIcon />}
                    aria-expanded={isOpen}
                    aria-haspopup="true"
                    onClick={() => {
                      if (!lockedGroup) {
                        onToggleGroup(group.key)
                      }
                    }}
                    sx={headerButtonSx}
                  >
                    {group.label}
                  </Button>
                  {/* チュートリアルのスポットライトが位置を測れるように、Popper ではなく通常の要素として描画する */}
                  {isOpen && (
                    <Box
                      sx={{
                        position: 'absolute',
                        top: 'calc(100% + 6px)',
                        left: 0,
                        zIndex: 1200,
                        minWidth: 180,
                        py: 0.5,
                        display: 'flex',
                        flexDirection: 'column',
                        backgroundColor: '#ffffff',
                        borderRadius: '10px',
                        boxShadow: '0 4px 14px rgba(60, 40, 0, 0.16)',
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
        <Box sx={{ display: 'flex', alignItems: 'center', gap: 1 }}>
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
                color: 'rgba(255,255,255,0.56)',
                backgroundColor: 'rgba(122, 77, 25, 0.62)',
              },
            }}
          >
            <SignOutIcon />
          </IconButton>
        </Box>
      </Paper>
      {signOutError ? (
        <Alert severity="error" sx={{ mt: 1 }}>
          {signOutError}
        </Alert>
      ) : null}
    </Box>
  )
}
