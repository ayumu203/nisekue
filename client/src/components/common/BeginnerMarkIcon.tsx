import SvgIcon from '@mui/material/SvgIcon'
import type { SvgIconProps } from '@mui/material/SvgIcon'

/** 初心者マーク（若葉マーク）。左が黄、右が緑の矢羽型。 */
export default function BeginnerMarkIcon(props: SvgIconProps) {
  return (
    <SvgIcon {...props} viewBox="0 0 24 24">
      <path fill="#f5c400" stroke="#ffffff" strokeWidth={1} strokeLinejoin="round" d="M3 2.5 L12 7 L12 22 L3 16.5 Z" />
      <path
        fill="#1f9d55"
        stroke="#ffffff"
        strokeWidth={1}
        strokeLinejoin="round"
        d="M21 2.5 L12 7 L12 22 L21 16.5 Z"
      />
    </SvgIcon>
  )
}
