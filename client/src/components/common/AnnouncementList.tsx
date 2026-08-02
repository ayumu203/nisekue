import { Box, Stack, Typography } from '@mui/material'
import { announcementEmptyMessage, announcements, formatAnnouncementDate } from '@/lib/announcements'

export default function AnnouncementList() {
  return (
    <>
      {announcements.length === 0 ? (
        <Typography variant="body2">{announcementEmptyMessage}</Typography>
      ) : (
        <Stack spacing={2}>
          {announcements.map((item) => (
            <Box
              key={item.id}
              sx={{ backgroundColor: '#2f8b3f', color: '#ffffff', borderRadius: 2, px: { xs: 2, sm: 2.5 }, py: 2 }}
            >
              <Box
                sx={{
                  display: 'flex',
                  alignItems: 'baseline',
                  justifyContent: 'space-between',
                  gap: 1.5,
                  pb: 1,
                  borderBottom: '2px dotted rgba(255, 255, 255, 0.5)',
                }}
              >
                <Typography sx={{ fontSize: { xs: '1.05rem', sm: '1.15rem' }, fontWeight: 700, lineHeight: 1.3 }}>
                  {item.title}
                </Typography>
                <Typography
                  variant="body2"
                  sx={{
                    flexShrink: 0,
                    color: 'rgba(255, 255, 255, 0.85)',
                    fontVariantNumeric: 'tabular-nums',
                  }}
                >
                  {formatAnnouncementDate(item.date)}
                </Typography>
              </Box>
              <Typography
                sx={{ mt: 1.5, fontSize: { xs: '0.9rem', sm: '0.95rem' }, lineHeight: 1.7, whiteSpace: 'pre-line' }}
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
