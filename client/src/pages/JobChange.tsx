import {
  Alert,
  Box,
  ButtonBase,
  CircularProgress,
  IconButton,
  Stack,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useEffect, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer, updatePlayerJob } from '@/api/player'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import { HomeFilledIcon, HomeIcon } from '@/components/home/HomeIcons'
import { buildHomeMenu, type HomeMenuGroupKey, type HomeMenuItem } from '@/components/home/homeMenu'
import JobChangeTab from '@/components/player-job/JobChangeTab'
import JobRoadmapTab from '@/components/player-job/JobRoadmapTab'
import { jobChangePanelColors as colors } from '@/constants/styles'
import { useAuth } from '@/contexts/useAuth'
import { useNotify } from '@/contexts/useNotify'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import { getTutorialStep, setTutorialStep } from '@/lib/tutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import locale from '../../locale/player-job/JobChange.json'
import { font, palette, withAlpha } from '@/theme/tokens'

export default function JobChange() {
  const { session, isLoading } = useAuth()
  const userId = session?.user.id ?? null
  const theme = useTheme()
  // sm 以上はヘッダーメニュー、md 以上は 2 列（左に現在の職業・右に舞台）
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  const isWide = useMediaQuery(theme.breakpoints.up('md'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const notify = useNotify()
  const [isSubmittingJobValue, setIsSubmittingJobValue] = useState<number | null>(null)
  const [tabIndex, setTabIndex] = useState(0)

  useEffect(() => {
    if (!userId) {
      return
    }

    const step = getTutorialStep(userId)
    if (step === 'home-job-change') {
      setTutorialStep(userId, 'job-change-info')
      setTutorialStepState('job-change-info')
    } else {
      setTutorialStepState(step)
    }
  }, [userId])

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }

  const playerSWRKey = session?.user.id ? (['job-change', session.user.id] as const) : null
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
      if (!message.includes(locale.playerNotFoundMessage)) {
        throw error
      }

      await createPlayer({ userName: INITIAL_PLAYER_NAME }, session.access_token)
      return getPlayer(session.access_token)
    }
  })

  async function handleChangeJob(nextJobValue: number): Promise<void> {
    if (!session?.access_token) {
      notify.error(locale.sessionMissing)
      return
    }

    setIsSubmittingJobValue(nextJobValue)

    try {
      await updatePlayerJob({ job: nextJobValue }, session.access_token)
      await mutatePlayer()
    } catch (error) {
      notify.error(error instanceof Error ? error.message : locale.changeFailed)
    } finally {
      setIsSubmittingJobValue(null)
    }
  }

  function selectTab(index: number): void {
    setTabIndex(index)
    if (index === 1 && tutorialStep === 'job-change-roadmap') {
      advanceTutorial('training-to-lv7')
    }
  }

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

  const tutorialHomeButtonId = tutorialStep === 'training-to-lv7' ? 'tutorial-home-btn' : undefined
  const roadmapTabId = tutorialStep === 'job-change-roadmap' ? 'tutorial-roadmap-tab' : undefined
  const tabLabels = [locale.title, locale.roadmapTab]

  // PC のヘッダーメニュー。先頭に「ホーム」を足し、今いる転職を含む「強化」を帯の色で示す
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

  // PC：「基本職｜ロードマップ」を、クエスト・アイテムと同じく隙間なく 1 本の帯につなげたタブにする。
  // 上部の帯（濃い藍）に溶け込まないよう、非選択は空色、選択中は藍
  const desktopTabs = (
    <Box
      role="tablist"
      aria-label={locale.roadmapTab}
      sx={{ display: 'flex', height: 48, flex: 'none', borderRadius: '12px', overflow: 'hidden' }}
    >
      {tabLabels.map((label, index) => {
        const selected = tabIndex === index
        return (
          <ButtonBase
            key={label}
            id={index === 1 ? roadmapTabId : undefined}
            role="tab"
            disableRipple
            aria-selected={selected}
            onClick={() => selectTab(index)}
            sx={{
              flex: 1,
              fontFamily: 'inherit',
              fontSize: font.lg,
              color: selected ? palette.white : palette.blue900,
              backgroundColor: selected ? colors.band : palette.blue300,
              borderLeft: index === 0 ? 'none' : `1px solid ${withAlpha(palette.white, 0.25)}`,
            }}
          >
            {label}
          </ButtonBase>
        )
      })}
    </Box>
  )

  // スマホ：帯の左端にホームボタン、右に 2 つのタブ。クエストと同じく、すぐ下の白い箱とつなげる
  const mobileTabs = (
    <Box
      role="tablist"
      aria-label={locale.roadmapTab}
      sx={{ display: 'flex', height: 44, flex: 'none', borderRadius: '12px 12px 0 0', overflow: 'hidden' }}
    >
      <Box
        sx={{
          flex: 'none',
          display: 'flex',
          alignItems: 'center',
          backgroundColor: colors.band,
          // ホームボタンと帯の中身の区切り（クエストの帯と同じ白い線）
          borderRight: `2px solid ${withAlpha(palette.white, 0.9)}`,
        }}
      >
        <IconButton
          id={tutorialHomeButtonId}
          component={Link}
          to="/"
          aria-label={locale.backToHome}
          disableRipple
          sx={{ color: palette.white, width: 44, height: 44 }}
        >
          <HomeFilledIcon />
        </IconButton>
      </Box>
      {tabLabels.map((label, index) => {
        const selected = tabIndex === index
        return (
          <ButtonBase
            key={label}
            id={index === 1 ? roadmapTabId : undefined}
            role="tab"
            disableRipple
            aria-selected={selected}
            onClick={() => selectTab(index)}
            sx={{
              flex: 1,
              fontFamily: 'inherit',
              fontSize: font.md,
              color: selected ? palette.white : palette.blue900,
              backgroundColor: selected ? colors.band : palette.blue300,
              borderLeft: index === 0 ? 'none' : `1px solid ${withAlpha(palette.white, 0.25)}`,
            }}
          >
            {label}
          </ButtonBase>
        )
      })}
    </Box>
  )

  return (
    <Box
      sx={{
        minHeight: '100dvh',
        display: 'flex',
        flexDirection: 'column',
        background: `linear-gradient(${colors.topBand}, ${colors.topBand}) top / 100% ${isDesktop ? 300 : 200}px no-repeat, ${colors.ground}`,
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
          currentKey="jobChange"
          currentColor={colors.topBand}
        />
      ) : null}
      <Box
        sx={{
          flex: 1,
          // PC は箱の中でスクロールさせるため、画面の高さに合わせる（低すぎる画面ではページごとスクロール）
          height: { md: 'calc(100dvh - 57px)' },
          minHeight: { md: 760 },
          boxSizing: 'border-box',
          px: { xs: 1.5, sm: 4 },
          pt: { xs: 1.5, sm: 2.5 },
          pb: { xs: 1.5, sm: 4 },
          display: 'flex',
          flexDirection: 'column',
        }}
      >
        {isDesktop ? <Box sx={{ mb: 2 }}>{desktopTabs}</Box> : mobileTabs}
        {isPlayerLoading ? (
          <Stack direction="row" spacing={1} alignItems="center" sx={{ color: palette.white }}>
            <CircularProgress size={16} color="inherit" />
            <Typography variant="body2">{locale.loadingPlayer}</Typography>
          </Stack>
        ) : playerError ? (
          <Alert severity="warning">{playerError.message}</Alert>
        ) : !player ? (
          <Alert severity="warning">{locale.loadingPlayer}</Alert>
        ) : tabIndex === 0 ? (
          <JobChangeTab
            key={player.job.code}
            player={player}
            isSubmittingJobValue={isSubmittingJobValue}
            onJobChange={handleChangeJob}
            isWide={isWide}
            attachToTabs={!isDesktop}
          />
        ) : (
          <JobRoadmapTab
            player={player}
            onPlayerChanged={() => mutatePlayer()}
            isWide={isWide}
            attachToTabs={!isDesktop}
          />
        )}
      </Box>
      {tutorialStep === 'training-to-lv7' && (
        <SpotlightTutorial
          guide="priest"
          targetId="tutorial-home-btn"
          message={tutorialLocale.steps.jobChangeToTraining.message}
        />
      )}
      {tutorialStep === 'job-change-info' && (
        <SpotlightTutorial
          guide="priest"
          message={tutorialLocale.steps.jobChangeInfo.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.jobChangeInfo.dismissLabel}
          onDismiss={() => advanceTutorial('job-change-roadmap')}
        />
      )}
      {tutorialStep === 'job-change-roadmap' && (
        <SpotlightTutorial
          guide="priest"
          targetId="tutorial-roadmap-tab"
          message={tutorialLocale.steps.jobChangeRoadmap.message}
          showDismiss
          onDismiss={() => advanceTutorial('training-to-lv7')}
        />
      )}
    </Box>
  )
}
