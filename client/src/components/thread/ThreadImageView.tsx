import { Box, ButtonBase, Stack, Typography } from '@mui/material'
import VisibilityOffOutlinedIcon from '@mui/icons-material/VisibilityOffOutlined'
import { useState } from 'react'
import locale from '../../../locale/threads/ThreadDetail.json'

type ThreadImageViewProps = {
  imageUrl: string
  isSensitive: boolean
}

/**
 * スレッドの画像。センシティブな画像はタップするまで読み込まない（閲覧前の確認であり、アクセス制限ではない）。
 */
export default function ThreadImageView({ imageUrl, isSensitive }: ThreadImageViewProps) {
  const [isRevealed, setIsRevealed] = useState(!isSensitive)

  if (!isRevealed) {
    return (
      <ButtonBase
        onClick={() => setIsRevealed(true)}
        sx={{
          width: '100%',
          minHeight: 180,
          borderRadius: 2,
          border: '1px dashed #94704c',
          backgroundColor: '#cbb594',
          color: '#4f392c',
        }}
      >
        <Stack spacing={1} alignItems="center" sx={{ p: 2 }}>
          <VisibilityOffOutlinedIcon />
          <Typography variant="body2" fontWeight={700}>
            {locale.image.sensitiveNotice}
          </Typography>
          <Typography variant="caption">{locale.image.sensitiveReveal}</Typography>
        </Stack>
      </ButtonBase>
    )
  }

  return (
    <Box
      component="a"
      href={imageUrl}
      target="_blank"
      rel="noopener noreferrer"
      aria-label={locale.image.openOriginal}
      sx={{ display: 'inline-block', maxWidth: '100%' }}
    >
      <Box
        component="img"
        src={imageUrl}
        alt={locale.image.alt}
        loading="lazy"
        sx={{
          display: 'block',
          maxWidth: '100%',
          maxHeight: 480,
          objectFit: 'contain',
          borderRadius: 2,
          border: '1px solid #94704c',
          backgroundColor: '#fff7ea',
        }}
      />
    </Box>
  )
}
