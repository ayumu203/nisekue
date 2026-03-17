import { Button, Container, Link as MuiLink, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'
import { outerPagePaperSx } from '@/constants/styles'

export default function Thanks() {
  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Typography variant="h4">スペシャルサンクス</Typography>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle1" fontWeight={700}>
                モンスター &amp; マテリアルズ II様
              </Typography>
              <MuiLink
                href="https://darts-x.sakura.ne.jp/m/"
                target="_blank"
                rel="noopener noreferrer"
                underline="hover"
              >
                https://darts-x.sakura.ne.jp/m/
              </MuiLink>
            </Stack>
          </Paper>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle1" fontWeight={700}>
                Mokemo様
              </Typography>
              <MuiLink
                href="https://mokemo-factory.booth.pm"
                target="_blank"
                rel="noopener noreferrer"
                underline="hover"
              >
                https://mokemo-factory.booth.pm
              </MuiLink>
            </Stack>
          </Paper>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle1" fontWeight={700}>
                MNO様
              </Typography>
              <MuiLink
                href="https://www.tumblr.com/sanoya0u0"
                target="_blank"
                rel="noopener noreferrer"
                underline="hover"
              >
                https://www.tumblr.com/sanoya0u0
              </MuiLink>
            </Stack>
          </Paper>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle1" fontWeight={700}>
                三日月アルペジオ 様
              </Typography>
              <MuiLink
                href="https://roughsketch.en-grey.com/"
                target="_blank"
                rel="noopener noreferrer"
                underline="hover"
              >
                https://roughsketch.en-grey.com/
              </MuiLink>
            </Stack>
          </Paper>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle1" fontWeight={700}>
                らぬきの立ち絵保管庫
              </Typography>
              <MuiLink href="https://ranuking.ko-me.com/" target="_blank" rel="noopener noreferrer" underline="hover">
                https://ranuking.ko-me.com/
              </MuiLink>
            </Stack>
          </Paper>

          <Button component={Link} to="/" variant="outlined">
            ホームへ戻る
          </Button>
        </Stack>
      </Paper>
    </Container>
  )
}
