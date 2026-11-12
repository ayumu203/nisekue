import { Alert, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import { outerPagePaperSx } from '@/constants/styles'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import locale from '../../locale/debug/Debug.json'

export default function Debug() {
  const { canUseDebug, isLoading } = useDebugStatus()

  return (
    <Container maxWidth="md" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2.5 }}>
          <Stack direction="row" alignItems="center" spacing={1}>
            <HomeNavIconButton ariaLabel={locale.backToHome} />
            <Typography variant="h5" component="h1" sx={{ fontWeight: 700 }}>
              {locale.title}
            </Typography>
          </Stack>

          {isLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={20} />
              <Typography>{locale.loading}</Typography>
            </Stack>
          ) : !canUseDebug ? (
            <Alert severity="error">{locale.forbidden}</Alert>
          ) : (
            <Stack spacing={2} />
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
