import { Box, Stack, Typography } from '@mui/material'
import { useAnnouncements } from '@/hooks/useAnnouncements'
import { homePanelColors } from '@/constants/styles'
import {
  announcementEmptyMessage,
  announcementErrorMessage,
  announcementLoadingMessage,
  formatAnnouncementDate,
} from '@/lib/announcements'
import { font, palette, withAlpha } from '@/theme/tokens'

type AnnouncementListProps = {
  // card：緑のカードで並べる（スマホのホームなど）／list：点線で区切った一覧（PC のホームの白い箱の中）
  variant?: 'card' | 'list'
}

export default function AnnouncementList({ variant = 'card' }: AnnouncementListProps) {
  const { announcements, error, isLoading } = useAnnouncements()

  if (isLoading) {
    return <Typography variant="body2">{announcementLoadingMessage}</Typography>
  }

  if (error && announcements.length === 0) {
    return (
      <Typography variant="body2" color="error">
        {announcementErrorMessage}
      </Typography>
    )
  }

  return (
    <>
      {announcements.length === 0 ? (
        <Typography variant="body2">{announcementEmptyMessage}</Typography>
      ) : variant === 'list' ? (
        <Box>
          {announcements.map((item) => (
            <Box
              key={item.id}
              component="article"
              sx={{ py: 2, borderBottom: `2px dotted ${homePanelColors.divider}`, '&:first-of-type': { pt: 0 } }}
            >
              <Box sx={{ display: 'flex', alignItems: 'baseline', justifyContent: 'space-between', gap: 1.5 }}>
                <Typography sx={{ fontSize: font.lg, fontWeight: 700, lineHeight: 1.5, color: homePanelColors.band }}>
                  {item.title}
                </Typography>
                <Typography
                  sx={{
                    flexShrink: 0,
                    fontSize: font.sm,
                    color: homePanelColors.label,
                    fontVariantNumeric: 'tabular-nums',
                  }}
                >
                  {formatAnnouncementDate(item.date)}
                </Typography>
              </Box>
              <Typography
                sx={{
                  mt: 0.75,
                  fontSize: font.md,
                  lineHeight: 1.7,
                  color: homePanelColors.text,
                  whiteSpace: 'pre-line',
                }}
              >
                {item.body}
              </Typography>
            </Box>
          ))}
        </Box>
      ) : (
        <Stack spacing={2}>
          {announcements.map((item) => (
            <Box
              key={item.id}
              sx={{
                backgroundColor: palette.green400,
                color: palette.white,
                borderRadius: 2,
                px: { xs: 2, sm: 2.5 },
                py: 2,
              }}
            >
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'baseline',
                  justifyContent: 'space-between',
                  gap: 1.5,
                  pb: 1,
                  borderBottom: `2px dotted ${withAlpha(palette.white, 0.5)}`,
                }}
              >
                <Typography sx={{ fontSize: { xs: font.md, sm: font.lg }, fontWeight: 700, lineHeight: 1.3 }}>
                  {item.title}
                </Typography>
                <Typography
                  variant="body2"
                  sx={{
                    flexShrink: 0,
                    color: withAlpha(palette.white, 0.9),
                    fontVariantNumeric: 'tabular-nums',
                  }}
                >
                  {formatAnnouncementDate(item.date)}
                </Typography>
              </Box>
              <Typography
                sx={{ mt: 1.5, fontSize: { xs: font.sm, sm: font.md }, lineHeight: 1.7, whiteSpace: 'pre-line' }}
              >
                {item.body}
              </Typography>
            </Box>
          ))}
        </Stack>
      )}
    </>
  )
}
