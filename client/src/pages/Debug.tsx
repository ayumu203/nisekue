import { Alert, CircularProgress, Container, Paper, Stack, Tab, Tabs, Typography } from '@mui/material'
import { useState } from 'react'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import DebugItemsSection from '@/components/debug/DebugItemsSection'
import DebugJobChangeSection from '@/components/debug/DebugJobChangeSection'
import DebugJobsSection from '@/components/debug/DebugJobsSection'
import DebugLevelSection from '@/components/debug/DebugLevelSection'
import DebugMovesSection from '@/components/debug/DebugMovesSection'
import DebugStatusSection from '@/components/debug/DebugStatusSection'
import DebugTutorialSection from '@/components/debug/DebugTutorialSection'
import DebugTemplateSection from '@/components/debug/DebugTemplateSection'
import { gameTabSx, gameTabsSx, outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
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

type DebugTab = 'player' | 'moves' | 'items' | 'others'

const DEBUG_TABS: readonly DebugTab[] = ['player', 'moves', 'items', 'others']

function DebugContent() {
  const [tab, setTab] = useState<DebugTab>('player')
  const { player, error, isLoading, accessToken, refresh } = useDebugPlayer()
  const { session } = useAuth()
  const userId = session?.user.id ?? null

  if (isLoading) {
    return <Loading text={locale.playerLoading} />
  }

  if (error || !player || !accessToken) {
    return <Alert severity="error">{error?.message ?? locale.forbidden}</Alert>
  }

  const sectionProps = { player, accessToken, onChanged: refresh }

  return (
    <Stack spacing={0}>
      <Tabs
        value={tab}
        onChange={(_, value: DebugTab) => setTab(value)}
        variant="fullWidth"
        sx={gameTabsSx}
        slotProps={{ indicator: { sx: { display: 'none' } } }}
      >
        {DEBUG_TABS.map((value) => (
          <Tab key={value} label={locale.tabs[value]} value={value} sx={gameTabSx} />
        ))}
      </Tabs>
      <Stack spacing={2} sx={{ pt: 2 }}>
        {tab === 'player' && (
          <>
            <DebugTemplateSection accessToken={accessToken} onChanged={refresh} />
            <DebugLevelSection {...sectionProps} />
            <DebugJobChangeSection {...sectionProps} />
            <DebugStatusSection key={`status-${player.level}`} {...sectionProps} />
            <DebugJobsSection {...sectionProps} />
          </>
        )}
        {tab === 'moves' && <DebugMovesSection {...sectionProps} />}
        {tab === 'items' && <DebugItemsSection accessToken={accessToken} onChanged={refresh} />}
        {tab === 'others' && userId && <DebugTutorialSection userId={userId} />}
      </Stack>
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
