import { Box, Button, Stack, SvgIcon, type SvgIconProps } from '@mui/material'
import { Link } from 'react-router-dom'
import type { HomeMenuGroup, HomeMenuGroupKey, HomeMenuItem } from '@/components/home/homeMenu'
import { font, palette, withAlpha } from '@/theme/tokens'

type HomeMenuListProps = {
  primary: HomeMenuItem[]
  groups: HomeMenuGroup[]
  openGroup: HomeMenuGroupKey | null
  // チュートリアルで開いたままにしているグループ。これがある間は閉じない
  lockedGroup: HomeMenuGroupKey | null
  onToggleGroup: (key: HomeMenuGroupKey) => void
}

function ChevronIcon({ up, ...props }: SvgIconProps & { up: boolean }) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path
        d={up ? 'M7.41 15.41 12 10.83l4.59 4.58L18 14l-6-6-6 6z' : 'M7.41 8.59 12 13.17l4.59-4.58L18 10l-6 6-6-6z'}
      />
    </SvgIcon>
  )
}

const MENU_TEXT_COLOR = palette.brown900
const MENU_DIVIDER_COLOR = withAlpha(palette.brown900, 0.25)

// 白い箱の中のタイル。区切りは細い線だけにして、ボタンの形は付けない
const tileSx = {
  minHeight: 72,
  borderRadius: 0,
  flexDirection: 'column',
  gap: 0.75,
  color: MENU_TEXT_COLOR,
  fontSize: font.md,
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '&:hover': {
    backgroundColor: palette.white,
  },
  '& .MuiButton-startIcon': {
    m: 0,
    color: palette.green400,
  },
  '& .MuiButton-startIcon > *:nth-of-type(1)': {
    fontSize: font.xxxl,
  },
} as const

const groupButtonSx = {
  minHeight: 48,
  minWidth: 0,
  px: 1,
  borderRadius: 0,
  color: MENU_TEXT_COLOR,
  fontSize: font.md,
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '&:hover': {
    backgroundColor: palette.white,
  },
  '&[aria-expanded="true"]': {
    backgroundColor: withAlpha(palette.brown900, 0.1),
  },
  '& .MuiButton-endIcon': {
    ml: 0.25,
  },
} as const

// 開いたメニューの 1 マス。モバイル用なのでホバーで色を変えない
const cellSx = {
  minHeight: 48,
  px: 1.5,
  borderRadius: 0,
  backgroundColor: palette.white,
  color: palette.brown900,
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '&:hover': {
    backgroundColor: palette.white,
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

// スマホのホームで使うメニュー。白い箱の中に、よく使う 4 つを 2×2 のタイル、残りのグループを横 1 列に並べる
// グループを開くと、箱の下に 2 列のマス目で項目を出す
export default function HomeMenuList({ primary, groups, openGroup, lockedGroup, onToggleGroup }: HomeMenuListProps) {
  const visibleGroup = groups.find((group) => group.key === (lockedGroup ?? openGroup)) ?? null
  const cells: (HomeMenuItem | null)[] = visibleGroup ? [...visibleGroup.items] : []
  // 項目が奇数のときは空きマスを足して、区切り線が途切れないようにする
  if (cells.length % 2 === 1) {
    cells.push(null)
  }

  return (
    <Stack spacing={1}>
      <Box component="nav" sx={{ backgroundColor: palette.white, borderRadius: '12px', overflow: 'hidden' }}>
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))' }}>
          {primary.map((item, index) => (
            <Button
              key={item.key}
              {...itemLinkProps(item)}
              id={item.id}
              startIcon={item.icon}
              onClick={item.onClick}
              sx={{
                ...tileSx,
                borderBottom: `1px solid ${MENU_DIVIDER_COLOR}`,
                borderRight: index % 2 === 0 ? `1px solid ${MENU_DIVIDER_COLOR}` : 'none',
              }}
            >
              {item.label}
            </Button>
          ))}
        </Box>
        <Box sx={{ display: 'grid', gridTemplateColumns: `repeat(${groups.length}, minmax(0, 1fr))` }}>
          {groups.map((group, index) => {
            const isOpen = visibleGroup?.key === group.key

            return (
              <Button
                key={group.key}
                endIcon={<ChevronIcon up={isOpen} />}
                aria-expanded={isOpen}
                onClick={() => {
                  if (!lockedGroup) {
                    onToggleGroup(group.key)
                  }
                }}
                sx={{
                  ...groupButtonSx,
                  borderRight: index < groups.length - 1 ? `1px solid ${MENU_DIVIDER_COLOR}` : 'none',
                }}
              >
                {group.label}
              </Button>
            )
          })}
        </Box>
      </Box>
      {/* チュートリアルのスポットライトが位置を測れるように、通常の要素として描画する */}
      {visibleGroup && (
        <Box
          sx={{
            display: 'grid',
            gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
            // 余白ではなく項目の区切り線（背景色を 1px 見せる）
            // eslint-disable-next-line no-restricted-syntax
            gap: '1px',
            backgroundColor: palette.brown200,
            border: `1px solid ${palette.brown200}`,
            borderRadius: '12px',
            overflow: 'hidden',
            boxShadow: `0 4px 14px ${withAlpha(palette.brown900, 0.1)}`,
          }}
        >
          {cells.map((item, index) =>
            item ? (
              <Button
                key={item.key}
                {...itemLinkProps(item)}
                id={item.id}
                onClick={item.onClick}
                sx={item.danger ? { ...cellSx, color: palette.red700 } : cellSx}
              >
                {item.label}
              </Button>
            ) : (
              <Box key={`empty-${index}`} sx={{ backgroundColor: palette.white }} />
            ),
          )}
        </Box>
      )}
    </Stack>
  )
}
