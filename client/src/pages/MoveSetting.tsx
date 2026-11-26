import {
  Alert,
  Box,
  CircularProgress,
  Drawer,
  IconButton,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayerMoveSet } from '@/api/player'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeFilledIcon, HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey, type HomeMenuItem } from '@/components/home/homeMenu'
import SkillBook from '@/components/moveSetting/SkillBook'
import SkillDetail from '@/components/moveSetting/SkillDetail'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import { skillPanelColors as colors } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { getTutorialStep, setTutorialStep } from '@/lib/tutorial'
import type { PlayerMoveSlot } from '@/schema/player'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import locale from '../../locale/player-setting/PlayerSetting.json'

export default function MoveSetting() {
  const { session, isLoading } = useAuth()
  const theme = useTheme()
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  // 列をそろえた表と右の詳細を横に並べるのは、十分に幅があるときだけ
  const isWide = useMediaQuery(theme.breakpoints.up('lg'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const [prevUserId, setPrevUserId] = useState(userId)
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)
  const [editableSlots, setEditableSlots] = useState<PlayerMoveSlot[]>([])
  const [selectedIndex, setSelectedIndex] = useState(0)
  const [isSheetOpen, setIsSheetOpen] = useState(false)
  const [isSaving, setIsSaving] = useState(false)
  const [feedback, setFeedback] = useState<{ type: 'success' | 'error'; message: string } | null>(null)

  if (userId !== prevUserId) {
    setPrevUserId(userId)
    setTutorialStepState(userId ? getTutorialStep(userId) : null)
  }

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

  useEffect(() => {
    if (player) {
      setEditableSlots(player.moveSlots)
    }
  }, [player])

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

  const hasChanges = player
    ? editableSlots.some((slot, index) => slot.moveId !== player.moveSlots[index]?.moveId)
    : false
  const selectedSlot = editableSlots[selectedIndex] ?? null

  async function handleSave(): Promise<void> {
    if (!session?.access_token) {
      return
    }

    setIsSaving(true)
    setFeedback(null)
    try {
      const response = await updatePlayerMoveSet(
        { moveIds: editableSlots.map((slot) => slot.moveId) },
        session.access_token,
      )
      setFeedback({ type: 'success', message: response.message || locale.moveOrderSaved })
      await mutatePlayer()
    } catch (error) {
      setFeedback({
        type: 'error',
        message: error instanceof Error ? error.message : locale.moveOrderSaveFailed,
      })
    } finally {
      setIsSaving(false)
    }
  }

  function handleCancel(): void {
    if (player) {
      setEditableSlots(player.moveSlots)
    }
    setFeedback(null)
  }

  function handleReorder(next: PlayerMoveSlot[], fromIndex: number, toIndex: number): void {
    setEditableSlots(next)
    // 選んでいる行は、動かした先についていく
    setSelectedIndex((current) => {
      if (current === fromIndex) {
        return toIndex
      }
      if (fromIndex < current && current <= toIndex) {
        return current - 1
      }
      if (toIndex <= current && current < fromIndex) {
        return current + 1
      }
      return current
    })
  }

  function handleSelect(index: number): void {
    setSelectedIndex(index)
    if (!isWide && editableSlots[index]?.moveId != null) {
      setIsSheetOpen(true)
    }
  }

  const tutorialHomeButtonId = tutorialStep === 'home-treasure-map' ? 'tutorial-home-btn-move' : undefined

  // PC のヘッダーメニュー。ホームと同じ項目の先頭に「ホーム」を足し、今いるスキル（強化の中）を帯の色で示す
  const homeMenu = buildHomeMenu({
    userId,
    tutorialStep: null,
    showRebirthTutorial: false,
    showSurveyGuide: false,
    onSurveyClick: () => {},
    canUseDebug,
  })
  const homeMenuItem: HomeMenuItem = {
    key: 'home',
    label: locale.backToHome,
    icon: <HomeIcon />,
    to: '/',
    id: tutorialHomeButtonId,
  }

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        background: `linear-gradient(${colors.topBand}, ${colors.topBand}) top / 100% ${isDesktop ? 280 : 150}px no-repeat, ${colors.ground}`,
        fontFamily: 'inherit',
      }}
    >
      {isDesktop ? (
        <HomeHeaderMenu
          primary={[homeMenuItem, ...homeMenu.primary]}
          groups={homeMenu.groups}
          openGroup={openMenuGroup}
          lockedGroup={null}
          onToggleGroup={(key) => setOpenMenuGroup((current) => (current === key ? null : key))}
          onCloseGroup={() => setOpenMenuGroup(null)}
          currentKey="moveSetting"
          currentColor={colors.cover}
        />
      ) : null}

      <Box
        sx={{
          flex: 1,
          // PC は本の中でスクロールさせるため、画面の高さに合わせる（低すぎる画面ではページごとスクロール）
          height: { lg: 'calc(100dvh - 57px)' },
          minHeight: { lg: 760 },
          boxSizing: 'border-box',
          px: { xs: 1.25, sm: 4 },
          pt: { xs: 1.5, sm: 2.75 },
          pb: { xs: 3, sm: 3.75 },
          display: 'flex',
          flexDirection: 'column',
          gap: 1.5,
        }}
      >
        {feedback ? <Alert severity={feedback.type}>{feedback.message}</Alert> : null}
        {isPlayerLoading ? (
          <Stack direction="row" spacing={1} alignItems="center" sx={{ color: '#ffffff' }}>
            <CircularProgress size={16} color="inherit" />
            <Typography variant="body2">{locale.loadingPlayer}</Typography>
          </Stack>
        ) : playerError ? (
          <Alert severity="warning">{playerError.message}</Alert>
        ) : !player ? (
          <Alert severity="warning">{locale.loadingPlayer}</Alert>
        ) : (
          <Box
            sx={{
              flex: 1,
              minHeight: 0,
              display: 'grid',
              gap: 3.5,
              gridTemplateColumns: { xs: 'minmax(0, 1fr)', lg: 'minmax(0, 1fr) 560px', xl: 'minmax(0, 1fr) 600px' },
              alignContent: { xs: 'start', lg: 'stretch' },
            }}
          >
            <SkillBook
              id={tutorialStep === 'move-setting-info' ? 'tutorial-move-item-box' : undefined}
              slots={editableSlots}
              selectedIndex={selectedIndex}
              onSelect={handleSelect}
              onReorder={handleReorder}
              hasChanges={hasChanges}
              isSaving={isSaving}
              onSave={() => void handleSave()}
              onCancel={handleCancel}
              compact={!isWide}
              bandStartAction={
                isDesktop ? undefined : (
                  <IconButton
                    component={Link}
                    to="/"
                    id={tutorialHomeButtonId}
                    aria-label={locale.backToHome}
                    disableRipple
                    sx={{ color: '#ffffff', width: 44, height: 44 }}
                  >
                    <HomeFilledIcon />
                  </IconButton>
                )
              }
            />
            {isWide ? (
              <Box
                component="section"
                aria-label={locale.skillDetailLabel}
                sx={{
                  minHeight: 0,
                  minWidth: 0,
                  backgroundColor: colors.cover,
                  border: `2px solid ${colors.coverDark}`,
                  p: 1.75,
                  display: 'flex',
                  flexDirection: 'column',
                }}
              >
                <SkillDetail slot={selectedSlot} playerImagePath={player.imagePath ?? null} compact={false} />
              </Box>
            ) : null}
          </Box>
        )}
      </Box>

      {/* 狭い画面では、行を押すと詳細が下から開く */}
      <Drawer
        anchor="bottom"
        open={!isWide && isSheetOpen && selectedSlot?.moveId != null}
        onClose={() => setIsSheetOpen(false)}
        slotProps={{
          paper: {
            'aria-label': locale.skillDetailLabel,
            sx: {
              height: 'min(640px, 85dvh)',
              backgroundColor: colors.cover,
              borderRadius: '6px 6px 0 0',
              px: 1.25,
              pt: 1,
              pb: 1.75,
              display: 'flex',
              flexDirection: 'column',
              gap: 1,
            },
          },
        }}
      >
        <Box sx={{ position: 'relative', height: 28, display: 'grid', placeItems: 'center', flexShrink: 0 }}>
          <Box
            aria-hidden="true"
            sx={{ width: 48, height: 5, borderRadius: '999px', backgroundColor: 'rgba(255, 255, 255, 0.5)' }}
          />
          <IconButton
            aria-label={locale.closeDetail}
            onClick={() => setIsSheetOpen(false)}
            sx={{ position: 'absolute', right: 0, top: -8, width: 44, height: 44, color: colors.ink }}
          >
            <svg
              width="22"
              height="22"
              viewBox="0 0 24 24"
              fill="none"
              stroke="currentColor"
              strokeWidth="2.6"
              strokeLinecap="round"
              aria-hidden="true"
            >
              <path d="M6 6l12 12M18 6 6 18" />
            </svg>
          </IconButton>
        </Box>
        <SkillDetail slot={selectedSlot} playerImagePath={player?.imagePath ?? null} compact />
      </Drawer>

      {tutorialStep === 'move-setting-info' && (
        <SpotlightTutorial
          guide="mage"
          targetId="tutorial-move-item-box"
          message={tutorialLocale.steps.moveSettingInfo.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.moveSettingInfo.dismissLabel}
          onDismiss={() => advanceTutorial('home-treasure-map')}
        />
      )}
      {tutorialStep === 'home-treasure-map' && (
        <SpotlightTutorial
          guide="mage"
          targetId="tutorial-home-btn-move"
          message={tutorialLocale.steps.moveSettingToHome.message}
        />
      )}
    </Box>
  )
}
