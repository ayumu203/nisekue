import { Box, Button, Stack, SvgIcon, type SvgIconProps } from '@mui/material'
import { Link } from 'react-router-dom'
import type { HomeMenuGroup, HomeMenuGroupKey, HomeMenuItem } from '@/components/home/homeMenu'
import { softGreenButtonSx } from '@/constants/styles'

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

const tileSx = {
  ...softGreenButtonSx,
  minHeight: 64,
  borderRadius: '14px',
  fontSize: '0.95rem',
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '& .MuiButton-startIcon > *:nth-of-type(1)': {
    fontSize: 22,
  },
} as const

const groupButtonSx = {
  ...softGreenButtonSx,
  minHeight: 44,
  minWidth: 0,
  px: 1,
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '& .MuiButton-endIcon': {
    ml: 0.25,
  },
} as const

// 開いたメニューの 1 マス。モバイル用なのでホバーで色を変えない
const cellSx = {
  minHeight: 48,
  px: 1.5,
  borderRadius: 0,
  backgroundColor: '#ffffff',
  color: '#3a2f1e',
  fontWeight: 700,
  whiteSpace: 'nowrap',
  '&:hover': {
    backgroundColor: '#ffffff',
  },
} as const

function itemLinkProps(item: HomeMenuItem) {
  return item.to
    ? { component: Link, to: item.to }
    : { component: 'a', href: item.href, target: '_blank', rel: 'noopener noreferrer' }
}

// スマホのホームで使うメニュー。よく使う 4 つは 2×2 のタイル、残りはグループごとに開いて 2 列のマス目で出す
export default function HomeMenuList({ primary, groups, openGroup, lockedGroup, onToggleGroup }: HomeMenuListProps) {
  const visibleGroup = groups.find((group) => group.key === (lockedGroup ?? openGroup)) ?? null
  const cells: (HomeMenuItem | null)[] = visibleGroup ? [...visibleGroup.items] : []
  // 項目が奇数のときは空きマスを足して、区切り線が途切れないようにする
  if (cells.length % 2 === 1) {
    cells.push(null)
  }

  return (
    <Stack spacing={1.5}>
      <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(2, minmax(0, 1fr))', gap: 1 }}>
        {primary.map((item) => (
          <Button
            key={item.key}
            {...itemLinkProps(item)}
            id={item.id}
            variant="contained"
            startIcon={item.icon}
            onClick={item.onClick}
            sx={tileSx}
          >
            {item.label}
          </Button>
        ))}
      </Box>
      <Stack spacing={1}>
        <Box sx={{ display: 'grid', gridTemplateColumns: 'repeat(3, minmax(0, 1fr))', gap: 1 }}>
          {groups.map((group) => {
            const isOpen = visibleGroup?.key === group.key

            return (
              <Button
                key={group.key}
                variant="contained"
                endIcon={<ChevronIcon up={isOpen} />}
                aria-expanded={isOpen}
                onClick={() => {
                  if (!lockedGroup) {
                    onToggleGroup(group.key)
                  }
                }}
                sx={groupButtonSx}
              >
                {group.label}
              </Button>
            )
          })}
        </Box>
        {/* チュートリアルのスポットライトが位置を測れるように、通常の要素として描画する */}
        {visibleGroup && (
          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: 'repeat(2, minmax(0, 1fr))',
              gap: '1px',
              backgroundColor: '#eadcbf',
              border: '1px solid #d9c49a',
              borderRadius: '12px',
              overflow: 'hidden',
              boxShadow: '0 4px 14px rgba(60, 40, 0, 0.16)',
            }}
          >
            {cells.map((item, index) =>
              item ? (
                <Button key={item.key} {...itemLinkProps(item)} id={item.id} onClick={item.onClick} sx={cellSx}>
                  {item.label}
                </Button>
              ) : (
                <Box key={`empty-${index}`} sx={{ backgroundColor: '#ffffff' }} />
              ),
            )}
          </Box>
        )}
      </Stack>
    </Stack>
  )
}
