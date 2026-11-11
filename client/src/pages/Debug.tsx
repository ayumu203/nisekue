import { Alert, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import DebugItemsSection from '@/components/debug/DebugItemsSection'
import DebugLevelSection from '@/components/debug/DebugLevelSection'
import DebugMovesSection from '@/components/debug/DebugMovesSection'
import DebugStatusSection from '@/components/debug/DebugStatusSection'
import { outerPagePaperSx } from '@/constants/styles'
import { useDebugPlayer } from '@/hooks/useDebugPlayer'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import locale from '../../locale/debug/Debug.json'

function Loading({ text }: { text: string }) {
  return (
    <Stack direction="row" spacing={1} alignItems="center">
      <CircularProgress size={20} />
      <Typography>{text}</Typography>
    </Stack>
  )
}

function DebugContent() {
  const { player, error, isLoading, accessToken, refresh } = useDebugPlayer()

  if (isLoading) {
    return <Loading text={locale.playerLoading} />
  }

  if (error || !player || !accessToken) {
    return <Alert severity="error">{error?.message ?? locale.forbidden}</Alert>
  }

  const sectionProps = { player, accessToken, onChanged: refresh }

  return (
    <Stack spacing={2}>
      <Alert severity="warning">{locale.description}</Alert>
      <DebugLevelSection {...sectionProps} />
      <DebugStatusSection key={`status-${player.level}`} {...sectionProps} />
      <DebugMovesSection {...sectionProps} />
      <DebugItemsSection accessToken={accessToken} onChanged={refresh} />
    </Stack>
  )
}

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
            <Loading text={locale.loading} />
          ) : !canUseDebug ? (
            <Alert severity="error">{locale.forbidden}</Alert>
          ) : (
            <DebugContent />
          )}
        </Stack>
      </Paper>
    </Container>
  )
}
