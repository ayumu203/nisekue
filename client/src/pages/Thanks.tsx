import { Button, Container, Link as MuiLink, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'

export default function Thanks() {
  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={{ p: { xs: 2, sm: 4 } }}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Typography variant="h4">サンクス</Typography>
          <Typography variant="body2" color="text.secondary">
            利用させていただいた画像素材の提供元です。
          </Typography>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: { xs: 2, sm: 2.5 } }}>
            <Stack spacing={0.5}>
              <Typography variant="subtitle1" fontWeight={700}>
                モンスター &amp; マテリアルズ II
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

          <Button component={Link} to="/" variant="outlined">
            ホームへ戻る
          </Button>
        </Stack>
      </Paper>
    </Container>
  )
}
