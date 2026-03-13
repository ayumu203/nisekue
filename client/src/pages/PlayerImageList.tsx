import { Alert, Box, Button, Container, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'
import { outerPagePaperSx } from '@/constants/styles'
import { resolveCharacterAssetPath } from '@/lib/assets'
import { PLAYER_IMAGE_OPTIONS } from '@/lib/playerImages'
import locale from '../../locale/player-image-list/PlayerImageList.json'

export default function PlayerImageList() {
  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={2}>
          <Stack
            direction={{ xs: 'column', sm: 'row' }}
            spacing={1.5}
            justifyContent="space-between"
            alignItems={{ xs: 'stretch', sm: 'center' }}
          >
            <Typography variant="h4">{locale.title}</Typography>
            <Stack direction={{ xs: 'column', sm: 'row' }} spacing={1}>
              <Button component={Link} to="/player-setting" variant="outlined">
                {locale.backToSettings}
              </Button>
              <Button component={Link} to="/" variant="outlined">
                {locale.backToHome}
              </Button>
            </Stack>
          </Stack>

          <Alert severity="warning">{locale.rightsNotice}</Alert>

          <Box
            sx={{
              display: 'grid',
              gridTemplateColumns: {
                xs: 'repeat(2, minmax(0, 1fr))',
                sm: 'repeat(3, minmax(0, 1fr))',
                md: 'repeat(4, minmax(0, 1fr))',
              },
              gap: 2,
            }}
          >
            {PLAYER_IMAGE_OPTIONS.map((option) => (
              <Paper
                key={option.imageNo}
                variant="outlined"
                sx={{
                  p: 1.5,
                  borderRadius: 3,
                  display: 'grid',
                  gap: 1,
                }}
              >
                <Typography variant="subtitle2">
                  {locale.imageNoLabel} {option.imageNo}
                </Typography>
                <Box
                  sx={{
                    width: '100%',
                    aspectRatio: '338 / 350',
                    borderRadius: 2,
                    bgcolor: 'rgba(255,255,255,0.72)',
                    border: '1px solid',
                    borderColor: 'divider',
                    overflow: 'hidden',
                    display: 'grid',
                    placeItems: 'center',
                  }}
                >
                  <Box
                    component="img"
                    src={resolveCharacterAssetPath(option.fileName)}
                    alt={`${locale.imageNoLabel} ${option.imageNo}`}
                    sx={{
                      width: '100%',
                      height: '100%',
                      objectFit: 'contain',
                      objectPosition: 'center bottom',
                      display: 'block',
                    }}
                  />
                </Box>
                <Typography variant="caption" color="text.secondary" sx={{ wordBreak: 'break-all' }}>
                  {option.fileName}
                </Typography>
              </Paper>
            ))}
          </Box>
        </Stack>
      </Paper>
    </Container>
  )
}
