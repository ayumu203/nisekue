import { Alert, Box, CircularProgress, Container, Paper, Stack, Typography } from '@mui/material'
import { useEffect, useState } from 'react'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import BeginnerGuide from '@/components/common/BeginnerGuide'
import HomeNavIconButton from '@/components/common/HomeNavIconButton'
import MoveItemBox from '@/components/moveSetting/MoveItemBox'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import { outerPagePaperSx } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { beginnerGuides } from '@/lib/beginnerGuides'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { getTutorialStep, setTutorialStep } from '@/lib/tutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import locale from '../../locale/player-setting/PlayerSetting.json'

export default function MoveSetting() {
  const { session, isLoading } = useAuth()
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))

  useEffect(() => {
    if (!userId) {
      return
    }

    setTutorialStepState(getTutorialStep(userId))
  }, [userId])

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }

  const playerSWRKey = session?.user.id ? (['move-setting', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
    mutate: mutatePlayer,
  } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error(locale.sessionMissing)
    }

    try {
      return await getPlayer(session.access_token)
    } catch (error) {
      const message = error instanceof Error ? error.message : ''
      if (!message.includes('プレイヤーが見つかりません')) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.loadingAuth}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Stack direction="row" justifyContent="space-between" alignItems="center" spacing={1.5}>
            <HomeNavIconButton
              id={tutorialStep === 'home-treasure-map' ? 'tutorial-home-btn-move' : undefined}
              ariaLabel={locale.backToHome}
              onClick={() => {
                if (userId && tutorialStep === 'home-treasure-map') {
                  setTutorialStep(userId, 'home-treasure-map')
                }
              }}
            />
            <BeginnerGuide
              userId={session?.user.id}
              guide={beginnerGuides.moveSetting}
              triggerSx={{
                minHeight: 44,
                height: 44,
                px: 1.5,
                alignSelf: 'center',
              }}
            />
          </Stack>

          {isPlayerLoading ? (
            <Stack direction="row" spacing={1} alignItems="center">
              <CircularProgress size={16} />
              <Typography variant="body2">{locale.loadingPlayer}</Typography>
            </Stack>
          ) : playerError ? (
            <Alert severity="warning">{playerError.message}</Alert>
          ) : !player ? (
            <Alert severity="warning">{locale.loadingPlayer}</Alert>
          ) : (
            <Box id={tutorialStep === 'move-setting-info' ? 'tutorial-move-item-box' : undefined}>
              <MoveItemBox
                player={player}
                accessToken={session?.access_token ?? ''}
                onSaved={async () => {
                  await mutatePlayer()
                }}
              />
            </Box>
          )}
        </Stack>
      </Paper>
      {tutorialStep === 'move-setting-info' && (
        <SpotlightTutorial
          targetId="tutorial-move-item-box"
          message={tutorialLocale.steps.moveSettingInfo.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.moveSettingInfo.dismissLabel}
          onDismiss={() => advanceTutorial('home-treasure-map')}
        />
      )}
      {tutorialStep === 'home-treasure-map' && (
        <SpotlightTutorial
          targetId="tutorial-home-btn-move"
          message={tutorialLocale.steps.moveSettingToHome.message}
        />
      )}
    </Container>
  )
}
