import {
  Alert,
  Badge,
  Box,
  CircularProgress,
  Pagination,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  Typography,
  useMediaQuery,
  useTheme,
} from '@mui/material'
import { useEffect, useEffectEvent, useRef, useState } from 'react'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import {
  deleteChatMessage,
  deleteGlobalChatMessage,
  getChatRoom,
  getGlobalChatRoom,
  markChatMessagesAlerted,
  postChatMessage,
  postGlobalChatMessage,
} from '@/api/chat'
import { getThreadAlerts, markThreadRepliesAlerted } from '@/api/thread'
import { useAuth } from '@/contexts/useAuth'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { useSignOut } from '@/hooks/useSignOut'
import { useAnnouncements } from '@/hooks/useAnnouncements'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/home/Home.json'
import ChatMessages from '@/components/chat/ChatMessages'
import ChatForm from '@/components/chat/ChatForm'
import AnnouncementList from '@/components/common/AnnouncementList'
import Status from '@/components/home/Status'
import HomeHeaderMenu from '@/components/home/HomeHeaderMenu'
import HomeMenuList from '@/components/home/HomeMenuList'
import { buildHomeMenu, getTutorialOpenGroup, type HomeMenuGroupKey } from '@/components/home/homeMenu'
import { gameTabSx, gameTabsSx, twoColumnContentGridSx } from '@/constants/styles'
import { announcementTabLabel } from '@/lib/announcements'
import { hasUnreadAnnouncement, markAnnouncementsSeen } from '@/lib/announcementStorage'
import {
  getTutorialStep,
  isRebirthTutorialShown,
  isSurveyGuideShown,
  setRebirthTutorialShown,
  setSurveyGuideShown,
  setTutorialStep,
} from '@/lib/tutorial'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'
import GamePageShell from '@/components/common/GamePageShell'

// チャットのタブ。PC では文字の長さに合わせた幅にする
// タブの下に引く線の色（選択中のタブと同じ濃い緑）
const chatTabLineColor = '#2f8b3f'

const chatTabSx = {
  ...gameTabSx,
  px: { xs: 0.5, sm: 3 },
} as const

// アンケートを案内するレベル
const SURVEY_GUIDE_LEVEL = 10

function Home() {
  const { session, isLoading, isAnonymous } = useAuth()
  const [toastQueue, setToastQueue] = useState<string[]>([])
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const [isRebirthTutorialDismissed, setIsRebirthTutorialDismissed] = useState(false)
  const [isSurveyGuideDismissed, setIsSurveyGuideDismissed] = useState(false)

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }
  const [chatTab, setChatTab] = useState<'personal' | 'global' | 'announcement'>('personal')
  const [globalChatPage, setGlobalChatPage] = useState(1)
  // メニューで開いているグループ（強化・交流・その他のうち 1 つだけ）
  const [openMenuGroup, setOpenMenuGroup] = useState<HomeMenuGroupKey | null>(null)
  const theme = useTheme()
  // 最初の描画から正しい側を出す（チュートリアルのスポットライトが最初に測った要素を使い続けるため）
  const isDesktop = useMediaQuery(theme.breakpoints.up('sm'), { noSsr: true })
  const { canUseDebug } = useDebugStatus()
  const { signOut, isSigningOut, signOutError } = useSignOut()
  const { latestAnnouncementDate } = useAnnouncements()
  const handledChatIdsRef = useRef<Set<number>>(new Set())
  const handledReplyIdsRef = useRef<Set<string>>(new Set())
  const enqueueToast = useEffectEvent((message: string) => {
    setToastQueue((current) => [...current, message])
  })
  const playerSWRKey = session?.user.id ? (['player', session.user.id] as const) : null
  const {
    data: player,
    error: playerError,
    isLoading: isPlayerLoading,
  } = useSWR(playerSWRKey, async () => {
    if (!session?.access_token) {
      throw new Error('セッションが無効です')
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
  const showRebirthTutorial =
    !isRebirthTutorialDismissed &&
    !!userId &&
    !!player &&
    player.level >= (player.rebirthRequiredLevel ?? 100) &&
    !isRebirthTutorialShown(userId)
  // Lv10 以上でチュートリアルを終えたプレイヤーに、アンケートを一度だけ案内する（転生の案内が出ているときは出さない）
  const showSurveyGuide =
    !isSurveyGuideDismissed &&
    !showRebirthTutorial &&
    !!userId &&
    !!player &&
    player.level >= SURVEY_GUIDE_LEVEL &&
    tutorialStep === 'completed' &&
    !isSurveyGuideShown(userId)

  function dismissSurveyGuide(): void {
    if (userId) {
      setSurveyGuideShown(userId)
    }
    setIsSurveyGuideDismissed(true)
  }

  const tutorialOpenGroup = getTutorialOpenGroup(tutorialStep, showRebirthTutorial, showSurveyGuide)
  const homeMenu = buildHomeMenu({
    userId,
    tutorialStep,
    showRebirthTutorial,
    showSurveyGuide,
    onSurveyClick: dismissSurveyGuide,
    canUseDebug,
    signOut: isDesktop ? undefined : { onClick: () => void signOut(), disabled: isSigningOut },
  })
  const toggleMenuGroup = (key: HomeMenuGroupKey) => setOpenMenuGroup((current) => (current === key ? null : key))
  const gearIconId = tutorialStep === 'home-player-setting' ? 'tutorial-gear-btn' : undefined
  const handleGearClick = () => {
    if (userId && tutorialStep === 'home-player-setting') {
      setTutorialStep(userId, 'player-setting-name')
    }
  }

  const chatSWRKey = session?.access_token && player?.userId ? (['chat-room', player.userId] as const) : null
  const {
    data: chatRoom,
    error: chatError,
    isLoading: isChatLoading,
    isValidating: isChatValidating,
    mutate: mutateChatRoom,
  } = useSWR(
    chatSWRKey,
    async () => {
      if (!session?.access_token || !player?.userId) {
        throw new Error(locale.chatFetchInfoMissing)
      }

      return getChatRoom({ ownerId: player.userId }, session.access_token)
    },
    { revalidateOnFocus: true },
  )
  const globalChatSWRKey = session?.access_token ? (['global-chat-room', globalChatPage] as const) : null
  const {
    data: globalChatRoom,
    error: globalChatError,
    isLoading: isGlobalChatLoading,
    isValidating: isGlobalChatValidating,
    mutate: mutateGlobalChatRoom,
  } = useSWR(
    globalChatSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getGlobalChatRoom({ page: globalChatPage }, session.access_token)
    },
    { revalidateOnFocus: true },
  )

  const threadAlertsSWRKey =
    session?.access_token && player?.userId ? (['thread-alerts', player.userId] as const) : null
  const { data: threadAlerts } = useSWR(
    threadAlertsSWRKey,
    async () => {
      if (!session?.access_token) {
        throw new Error(locale.sessionInfoMissing)
      }

      return getThreadAlerts(session.access_token)
    },
    { revalidateOnFocus: true },
  )

  useEffect(() => {
    if (!session?.access_token || !player?.userId || !chatRoom) {
      return
    }

    const unalertedMessages = chatRoom.messages.filter(
      (message) =>
        !message.isAlerted &&
        !handledChatIdsRef.current.has(message.chatId) &&
        (message.senderId == null || message.senderId !== player.userId),
    )

    if (unalertedMessages.length === 0) {
      return
    }

    unalertedMessages.forEach((message) => handledChatIdsRef.current.add(message.chatId))
    enqueueToast(locale.newMessageToast)
    void markChatMessagesAlerted(
      {
        ownerId: player.userId,
        chatIds: unalertedMessages.map((message) => message.chatId),
      },
      session.access_token,
    ).catch(() => {
      unalertedMessages.forEach((message) => handledChatIdsRef.current.delete(message.chatId))
    })
  }, [chatRoom, player?.userId, session?.access_token])

  useEffect(() => {
    if (!session?.access_token || !threadAlerts) {
      return
    }

    const pendingReplyIds = threadAlerts.items
      .flatMap((item) => item.replyIds)
      .filter((replyId) => !handledReplyIdsRef.current.has(replyId))

    if (pendingReplyIds.length === 0) {
      return
    }

    pendingReplyIds.forEach((replyId) => handledReplyIdsRef.current.add(replyId))
    enqueueToast(locale.newThreadReplyToast)
    void markThreadRepliesAlerted({ replyIds: pendingReplyIds }, session.access_token).catch(() => {
      pendingReplyIds.forEach((replyId) => handledReplyIdsRef.current.delete(replyId))
    })
  }, [session?.access_token, threadAlerts])

  // お知らせタブを開いている間は、後から取得した新しいお知らせもその場で既読にする
  useEffect(() => {
    if (chatTab === 'announcement') {
      markAnnouncementsSeen(userId, latestAnnouncementDate)
    }
  }, [chatTab, userId, latestAnnouncementDate])

  const hasUnreadAnnouncements = chatTab !== 'announcement' && hasUnreadAnnouncement(userId, latestAnnouncementDate)

  if (isLoading) {
    return (
      <Box minHeight="100vh" display="grid" sx={{ placeItems: 'center' }}>
        <Stack direction="row" spacing={1} alignItems="center">
          <CircularProgress size={20} />
          <Typography>{locale.authLoading}</Typography>
        </Stack>
      </Box>
    )
  }

  return (
    <GamePageShell>
      <Stack spacing={{ xs: 1.5, sm: 2 }}>
        {isDesktop && (
          <HomeHeaderMenu
            primary={homeMenu.primary}
            groups={homeMenu.groups}
            openGroup={openMenuGroup}
            lockedGroup={tutorialOpenGroup}
            onToggleGroup={toggleMenuGroup}
            onCloseGroup={() => setOpenMenuGroup(null)}
            gearIconId={gearIconId}
            onGearClick={handleGearClick}
          />
        )}
        {/* スマホではメニューとチャットのあいだを広めに空ける */}
        <Box sx={{ ...twoColumnContentGridSx, gap: { xs: 3, sm: 3 } }}>
          <Stack spacing={2}>
            {isPlayerLoading ? (
              <Stack direction="row" spacing={1} alignItems="center">
                <CircularProgress size={16} />
                <Typography variant="body2">{locale.playerLoading}</Typography>
              </Stack>
            ) : playerError ? (
              <Alert severity="warning">{playerError.message}</Alert>
            ) : (
              <Status
                player={player}
                actionAlign="start"
                showCollapseToggle={false}
                surface="plain"
                layout={isDesktop ? 'default' : 'compact'}
                showDesktopActions={false}
                gearIconId={isDesktop ? undefined : gearIconId}
                onGearClick={handleGearClick}
              />
            )}
            {!isDesktop && (
              <HomeMenuList
                primary={homeMenu.primary}
                groups={homeMenu.groups}
                openGroup={openMenuGroup}
                lockedGroup={tutorialOpenGroup}
                onToggleGroup={toggleMenuGroup}
              />
            )}
            {!isDesktop && signOutError ? <Alert severity="error">{signOutError}</Alert> : null}
          </Stack>

          {/* 枠では包まず、タブの下に線を引いてタブと中身をつなげる */}
          <Box sx={{ minWidth: 0 }}>
            <Tabs
              value={chatTab}
              onChange={(_, value: 'personal' | 'global' | 'announcement') => {
                setChatTab(value)
              }}
              variant={isDesktop ? 'standard' : 'fullWidth'}
              sx={{ ...gameTabsSx, borderBottom: `3px solid ${chatTabLineColor}` }}
              slotProps={{ indicator: { sx: { display: 'none' } } }}
            >
              <Tab label={locale.chatTabPersonal} value="personal" sx={chatTabSx} />
              <Tab label={locale.chatTabGlobal} value="global" sx={chatTabSx} />
              <Tab
                label={
                  <Badge variant="dot" color="error" invisible={!hasUnreadAnnouncements} sx={{ pr: 0.5 }}>
                    {announcementTabLabel}
                  </Badge>
                }
                value="announcement"
                sx={chatTabSx}
              />
            </Tabs>
            <Box sx={{ pt: { xs: 2, sm: 2.5 } }}>
              {chatTab === 'announcement' ? (
                <AnnouncementList />
              ) : chatTab === 'personal' ? (
                isChatLoading ? (
                  <Stack direction="row" spacing={1} alignItems="center">
                    <CircularProgress size={16} />
                    <Typography variant="body2">{locale.chatLoading}</Typography>
                  </Stack>
                ) : chatError ? (
                  <Alert severity="warning">{chatError.message}</Alert>
                ) : !player ? (
                  <Alert severity="warning">{locale.chatFetchInfoMissing}</Alert>
                ) : (
                  <Stack spacing={2}>
                    <ChatForm
                      isSubmitting={isChatValidating}
                      disabled={isAnonymous}
                      disabledReason={isAnonymous ? locale.anonymousPostingRestricted : null}
                      onSubmit={async (text) => {
                        if (!session?.access_token || !player?.userId) {
                          throw new Error(locale.sessionInfoMissing)
                        }

                        const updated = await postChatMessage(
                          {
                            ownerId: player.userId,
                            text,
                          },
                          session.access_token,
                        )
                        await mutateChatRoom(updated, { revalidate: false })
                      }}
                    />
                    <ChatMessages
                      messages={chatRoom?.messages ?? []}
                      currentPlayerId={player?.userId ?? ''}
                      onDeleteMessage={async (chatId) => {
                        if (!session?.access_token || !player?.userId) {
                          throw new Error(locale.sessionInfoMissing)
                        }

                        await deleteChatMessage({ ownerId: player.userId, chatId }, session.access_token)
                        await mutateChatRoom()
                      }}
                    />
                  </Stack>
                )
              ) : isGlobalChatLoading ? (
                <Stack direction="row" spacing={1} alignItems="center">
                  <CircularProgress size={16} />
                  <Typography variant="body2">{locale.globalChatLoading}</Typography>
                </Stack>
              ) : globalChatError ? (
                <Alert severity="warning">{globalChatError.message}</Alert>
              ) : (
                <Stack spacing={2}>
                  {globalChatPage === 1 && (
                    <ChatForm
                      isSubmitting={isGlobalChatValidating}
                      disabled={isAnonymous}
                      disabledReason={isAnonymous ? locale.anonymousPostingRestricted : null}
                      onSubmit={async (text) => {
                        if (!session?.access_token) {
                          throw new Error(locale.sessionInfoMissing)
                        }

                        const updated = await postGlobalChatMessage({ text }, session.access_token)
                        await mutateGlobalChatRoom(updated, { revalidate: false })
                      }}
                    />
                  )}
                  <ChatMessages
                    messages={globalChatRoom?.messages ?? []}
                    currentPlayerId={player?.userId ?? ''}
                    onDeleteMessage={async (chatId) => {
                      if (!session?.access_token) {
                        throw new Error(locale.sessionInfoMissing)
                      }

                      await deleteGlobalChatMessage({ chatId }, session.access_token)
                      await mutateGlobalChatRoom()
                    }}
                  />
                  {(globalChatRoom?.totalCount ?? 0) > 100 && (
                    <Pagination
                      count={Math.ceil((globalChatRoom?.totalCount ?? 0) / 100)}
                      page={globalChatPage}
                      onChange={(_, page) => setGlobalChatPage(page)}
                      size="small"
                      sx={{ alignSelf: 'center' }}
                    />
                  )}
                </Stack>
              )}
            </Box>
          </Box>
        </Box>
      </Stack>
      {tutorialStep === 'home-player-setting' && (
        <SpotlightTutorial targetId="tutorial-gear-btn" message={tutorialLocale.steps.homePlayerSetting.message} />
      )}
      {tutorialStep === 'home-training' && (
        <SpotlightTutorial targetId="tutorial-training-btn" message={tutorialLocale.steps.homeTraining.message} />
      )}
      {tutorialStep === 'home-job-change' && (
        <SpotlightTutorial targetId="tutorial-job-change-btn" message={tutorialLocale.steps.homeJobChange.message} />
      )}
      {tutorialStep === 'training-to-lv7' && (
        <SpotlightTutorial targetId="tutorial-training-btn" message={tutorialLocale.steps.backToTraining.message} />
      )}
      {tutorialStep === 'home-quest' && (
        <SpotlightTutorial targetId="tutorial-quest-btn" message={tutorialLocale.steps.homeQuest.message} />
      )}
      {tutorialStep === 'home-move-setting' && (
        <SpotlightTutorial
          targetId="tutorial-move-setting-btn"
          message={tutorialLocale.steps.homeMoveSetting.message}
        />
      )}
      {tutorialStep === 'home-treasure-map' && (
        <SpotlightTutorial
          targetId="tutorial-treasure-map-btn"
          message={tutorialLocale.steps.homeTreasureMap.message}
        />
      )}
      {tutorialStep === 'treasure-map-to-home' && (
        <SpotlightTutorial
          message={tutorialLocale.steps.tutorialComplete.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.tutorialComplete.dismissLabel}
          onDismiss={() => advanceTutorial('completed')}
        />
      )}
      {showSurveyGuide && (
        <SpotlightTutorial
          targetId="tutorial-survey-btn"
          message={tutorialLocale.steps.surveyGuide.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.surveyGuide.dismissLabel}
          onDismiss={dismissSurveyGuide}
        />
      )}
      {showRebirthTutorial && (
        <SpotlightTutorial
          guide="highPriest"
          targetId="tutorial-rebirth-btn"
          message={tutorialLocale.steps.rebirthGuide.message}
          showDismiss
          dismissLabel={tutorialLocale.steps.rebirthGuide.dismissLabel}
          onDismiss={() => {
            if (userId) {
              setRebirthTutorialShown(userId)
            }
            setIsRebirthTutorialDismissed(true)
          }}
        />
      )}
      <Snackbar
        open={toastQueue.length > 0}
        autoHideDuration={1500}
        onClose={(_, reason) => {
          if (reason === 'clickaway') {
            return
          }

          setToastQueue((current) => current.slice(1))
        }}
        anchorOrigin={{ vertical: 'bottom', horizontal: 'center' }}
      >
        <Alert
          severity="info"
          variant="filled"
          onClose={() => setToastQueue((current) => current.slice(1))}
          sx={{ width: '100%', alignItems: 'center' }}
        >
          {toastQueue[0] ?? ''}
        </Alert>
      </Snackbar>
    </GamePageShell>
  )
}

export default Home
