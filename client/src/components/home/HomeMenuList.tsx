import { Button, Collapse, Stack } from '@mui/material'
import { Link } from 'react-router-dom'
import type { HomeMenuGroup, HomeMenuGroupKey, HomeMenuItem } from '@/components/home/homeMenu'
import { menuButtonSx, softGreenButtonSx } from '@/constants/styles'

type HomeMenuListProps = {
  primary: HomeMenuItem[]
  groups: HomeMenuGroup[]
  openGroups: Set<HomeMenuGroupKey>
  onToggleGroup: (key: HomeMenuGroupKey) => void
}

function MenuItemButton({ item, variant }: { item: HomeMenuItem; variant: 'contained' | 'outlined' }) {
  const sx = variant === 'contained' ? { ...menuButtonSx, ...softGreenButtonSx } : menuButtonSx
  const linkProps = item.to
    ? { component: Link, to: item.to }
    : { component: 'a', href: item.href, target: '_blank', rel: 'noopener noreferrer' }

  return (
    <Button {...linkProps} id={item.id} variant={variant} startIcon={item.icon} sx={sx} onClick={item.onClick}>
      {item.label}
    </Button>
  )
}

// スマホのホームで使う、縦に並んだメニュー
export default function HomeMenuList({ primary, groups, openGroups, onToggleGroup }: HomeMenuListProps) {
  return (
    <Stack spacing={1.5}>
      {primary.map((item) => (
        <MenuItemButton key={item.key} item={item} variant="contained" />
      ))}
      {groups.map((group) => (
        <Stack key={group.key} spacing={0}>
          <Button
            variant={group.variant}
            startIcon={group.icon}
            onClick={() => onToggleGroup(group.key)}
            aria-expanded={openGroups.has(group.key)}
            sx={group.variant === 'contained' ? { ...menuButtonSx, ...softGreenButtonSx } : menuButtonSx}
          >
            {group.label}
          </Button>
          <Collapse in={openGroups.has(group.key)}>
            <Stack spacing={1} sx={{ pl: 1, pr: 1, pt: 1 }}>
              {group.items.map((item) => (
                <MenuItemButton key={item.key} item={item} variant={group.variant} />
              ))}
            </Stack>
          </Collapse>
        </Stack>
      ))}
    </Stack>
  )
}
