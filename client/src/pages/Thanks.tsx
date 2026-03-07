import { Button, Container, Link as MuiLink, Paper, Stack, Typography } from '@mui/material'
import { Link } from 'react-router-dom'

export default function Thanks() {
  return (
    <Container maxWidth="sm" sx={{ py: 8 }}>
      <Paper elevation={2} sx={{ p: 4 }}>
        <Stack spacing={2}>
          <Typography variant="h4">サンクス</Typography>
          <Typography variant="body2" color="text.secondary">
            利用させていただいた画像素材の提供元です。
          </Typography>

          <Paper variant="outlined" sx={{ borderRadius: 2, p: 2 }}>
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
