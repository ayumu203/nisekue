import { IconButton, SvgIcon, type SvgIconProps } from '@mui/material'
import { Link } from 'react-router-dom'
import { topNavigationIconButtonSx } from '@/constants/styles'

type HomeNavIconButtonProps = {
  ariaLabel: string
  id?: string
}

function HomeIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path d="M12 3.5 3 10.5V21h6.5v-5.5h5V21H21V10.5zm0 2.53 7 5.44V19h-2.5v-5.5h-9V19H5v-7.53z" />
    </SvgIcon>
  )
}

export default function HomeNavIconButton({ ariaLabel, id }: HomeNavIconButtonProps) {
  return (
    <IconButton id={id} component={Link} to="/" aria-label={ariaLabel} sx={topNavigationIconButtonSx}>
      <HomeIcon />
    </IconButton>
  )
}
