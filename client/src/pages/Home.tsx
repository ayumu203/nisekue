import {
  Alert,
  Box,
  Button,
  CircularProgress,
  Collapse,
  Container,
  Pagination,
  Paper,
  Snackbar,
  Stack,
  Tab,
  Tabs,
  Typography,
} from '@mui/material'
import { useEffect, useEffectEvent, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import useSWR from 'swr'
import { createPlayer, getPlayer } from '@/api/player'
import {
  getChatRoom,
  getGlobalChatRoom,
  markChatMessagesAlerted,
  postChatMessage,
  postGlobalChatMessage,
} from '@/api/chat'
import { getThreadAlerts, markThreadRepliesAlerted } from '@/api/thread'
import { useAuth } from '@/contexts/useAuth'
import { INITIAL_PLAYER_NAME } from '@/lib/player'
import locale from '../../locale/home/Home.json'
import ChatMessages from '@/components/chat/ChatMessages'
import ChatForm from '@/components/chat/ChatForm'
import BeginnerGuide from '@/components/common/BeginnerGuide'
import Status from '@/components/home/Status'
import {
  ItemsIcon,
  JobChangeIcon,
  MoveSettingIcon,
  QuestIcon,
  RebirthIcon,
  RankingIcon,
  OthersGroupIcon,
  ReportIcon,
  SpecialThanksIcon,
  SurveyIcon,
  ThreadsIcon,
  TrainingGroupIcon,
  TrainingIcon,
  TreasureMapIcon,
  VisitPlayersIcon,
  SocialGroupIcon,
} from '@/components/home/HomeIcons'
import {
  innerSurfaceSx,
  menuButtonSx,
  outerPagePaperSx,
  softGreenButtonSx,
  twoColumnContentGridSx,
} from '@/constants/styles'
import { beginnerGuides } from '@/lib/beginnerGuides'
import { getTutorialStep, isRebirthTutorialShown, setRebirthTutorialShown, setTutorialStep } from '@/lib/tutorial'
import SpotlightTutorial from '@/components/common/SpotlightTutorial'
import tutorialLocale from '../../locale/tutorial/Tutorial.json'

function Home() {
  const { session, isLoading, isAnonymous } = useAuth()
  const [toastQueue, setToastQueue] = useState<string[]>([])
  const userId = session?.user.id ?? null
  const [tutorialStep, setTutorialStepState] = useState(() => (userId ? getTutorialStep(userId) : null))
  const [isRebirthTutorialDismissed, setIsRebirthTutorialDismissed] = useState(false)

  function advanceTutorial(next: Parameters<typeof setTutorialStep>[1]): void {
    if (!userId) {
      return
    }

    setTutorialStep(userId, next)
    setTutorialStepState(next)
  }
  const [chatTab, setChatTab] = useState<'personal' | 'global'>('personal')
  const [globalChatPage, setGlobalChatPage] = useState(1)
  const [isTrainingGroupOpenByUser, setIsTrainingGroupOpenByUser] = useState(false)
  const [isSocialGroupOpen, setIsSocialGroupOpen] = useState(false)
  const [isOthersGroupOpen, setIsOthersGroupOpen] = useState(false)
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
    !isRebirthTutorialDismissed && !!userId && !!player && player.level >= 100 && !isRebirthTutorialShown(userId)
  const isTrainingGroupOpen =
    tutorialStep === 'home-job-change' ||
    tutorialStep === 'home-move-setting' ||
    tutorialStep === 'home-treasure-map' ||
    showRebirthTutorial ||
    isTrainingGroupOpenByUser

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
    <Container maxWidth="lg" sx={{ py: { xs: 2, sm: 8 } }}>
      <Paper elevation={2} sx={outerPagePaperSx}>
        <Stack spacing={{ xs: 1.5, sm: 2 }}>
          <Box sx={twoColumnContentGridSx}>
            <Stack spacing={{ xs: 1.5, sm: 2 }}>
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
                  gearIconId={tutorialStep === 'home-player-setting' ? 'tutorial-gear-btn' : undefined}
                  onGearClick={() => {
                    if (userId && tutorialStep === 'home-player-setting') {
                      setTutorialStep(userId, 'player-setting-random')
                    }
                  }}
                  topAction={
                    <BeginnerGuide
                      userId={session?.user.id}
                      guide={beginnerGuides.home}
                      triggerSx={{
                        minHeight: 44,
                        px: 2,
                        fontWeight: 700,
                      }}
                    />
                  }
                />
              )}
              <Button
                component={Link}
                to="/quest"
                variant="contained"
                startIcon={<QuestIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.quest}
              </Button>
              <Button
                id="tutorial-training-btn"
                component={Link}
                to="/training"
                variant="contained"
                startIcon={<TrainingIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                onClick={() => {
                  if (userId && tutorialStep === 'home-training') {
                    setTutorialStep(userId, 'training-fight')
                  }
                }}
              >
                {locale.training}
              </Button>
              <Button
                id={tutorialStep === 'home-treasure-map' ? 'tutorial-treasure-map-btn' : undefined}
                component={Link}
                to="/treasure-map"
                variant="contained"
                startIcon={<TreasureMapIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                onClick={() => {
                  if (userId && tutorialStep === 'home-treasure-map') {
                    setTutorialStep(userId, 'treasure-map-info')
                  }
                }}
              >
                {locale.treasureMap}
              </Button>
              <Button
                component={Link}
                to="/items"
                variant="contained"
                startIcon={<ItemsIcon />}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.items}
              </Button>
              <Button
                variant="contained"
                startIcon={<TrainingGroupIcon />}
                onClick={() => setIsTrainingGroupOpenByUser((prev) => !prev)}
                sx={{ ...menuButtonSx, ...softGreenButtonSx }}
              >
                {locale.trainingGroup}
              </Button>
              <Collapse in={isTrainingGroupOpen}>
                <Stack spacing={1} sx={{ pl: 1, pr: 1, pt: 1 }}>
                  <Button
                    id={tutorialStep === 'home-move-setting' ? 'tutorial-move-setting-btn' : undefined}
                    component={Link}
                    to="/move-setting"
                    variant="contained"
                    startIcon={<MoveSettingIcon />}
                    sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                    onClick={() => {
                      if (userId && tutorialStep === 'home-move-setting') {
                        setTutorialStep(userId, 'move-setting-info')
                      }
                    }}
                  >
                    {locale.moveSetting}
                  </Button>
                  <Button
                    id="tutorial-job-change-btn"
                    component={Link}
                    to="/job-change"
                    variant="contained"
                    startIcon={<JobChangeIcon />}
                    sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                    onClick={() => {
                      if (userId && tutorialStep === 'home-job-change') {
                        setTutorialStep(userId, 'job-change-info')
                      }
                    }}
                  >
                    {locale.jobChange}
                  </Button>
                  <Button
                    id={showRebirthTutorial ? 'tutorial-rebirth-btn' : undefined}
                    component={Link}
                    to="/rebirth"
                    variant="contained"
                    startIcon={<RebirthIcon />}
                    sx={{ ...menuButtonSx, ...softGreenButtonSx }}
                  >
                    {locale.rebirth}
                  </Button>
                </Stack>
              </Collapse>
              <Button
                variant="outlined"
                startIcon={<SocialGroupIcon />}
                onClick={() => setIsSocialGroupOpen((prev) => !prev)}
                sx={menuButtonSx}
              >
                {locale.socialGroup}
              </Button>
              <Collapse in={isSocialGroupOpen}>
                <Stack spacing={1} sx={{ pl: 1, pr: 1, pt: 1 }}>
                  <Button
                    component={Link}
                    to="/players"
                    variant="outlined"
                    startIcon={<VisitPlayersIcon />}
                    sx={menuButtonSx}
                  >
                    {locale.visitPlayers}
                  </Button>
                  <Button
                    component={Link}
                    to="/threads"
                    variant="outlined"
                    startIcon={<ThreadsIcon />}
                    sx={menuButtonSx}
                  >
                    {locale.threads}
                  </Button>
                  <Button
                    component={Link}
                    to="/ranking"
                    variant="outlined"
                    startIcon={<RankingIcon />}
                    sx={menuButtonSx}
                  >
                    {locale.ranking}
                  </Button>
                </Stack>
              </Collapse>
              <Button
                variant="outlined"
                startIcon={<OthersGroupIcon />}
                onClick={() => setIsOthersGroupOpen((prev) => !prev)}
                sx={menuButtonSx}
              >
                {locale.others}
              </Button>
              <Collapse in={isOthersGroupOpen}>
                <Stack spacing={1} sx={{ pl: 1, pr: 1, pt: 1 }}>
                  <Button
                    component={Link}
                    to="/thanks"
                    variant="outlined"
                    startIcon={<SpecialThanksIcon />}
                    sx={menuButtonSx}
                  >
                    {locale.specialThanks}
                  </Button>
                  <Button
                    component="a"
                    href="https://docs.google.com/forms/d/e/1FAIpQLScQGhSmvuy99jOyKKvHIYtKcbZOGpYucamzrza5CRKezS054A/viewform?usp=dialog"
                    target="_blank"
                    rel="noopener noreferrer"
                    variant="outlined"
                    startIcon={<SurveyIcon />}
                    sx={menuButtonSx}
                  >
                    {locale.survey}
                  </Button>
                  <Button
                    component="a"
                    href="https://docs.google.com/forms/d/e/1FAIpQLSduEn4lqqHBp4zuaXKehG3w4DQBXWmp2GdkMNU1tqO5_VAQUw/viewform?usp=publish-editor"
                    target="_blank"
                    rel="noopener noreferrer"
                    variant="outlined"
                    startIcon={<ReportIcon />}
                    sx={menuButtonSx}
                  >
                    {locale.report}
                  </Button>
                </Stack>
              </Collapse>
            </Stack>

            <Paper variant="outlined" sx={{ ...innerSurfaceSx, borderRadius: 3, p: { xs: 2, sm: 2.5 }, mt: '48px' }}>
              <Stack spacing={{ xs: 1.5, sm: 2 }}>
                <Tabs
                  value={chatTab}
                  onChange={(_, value: 'personal' | 'global') => setChatTab(value)}
                  variant="fullWidth"
                  sx={{ borderBottom: 1, borderColor: 'divider' }}
                >
                  <Tab label={locale.chatTabPersonal} value="personal" />
                  <Tab label={locale.chatTabGlobal} value="global" />
                </Tabs>
                {chatTab === 'personal' ? (
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
                      <ChatMessages messages={chatRoom?.messages ?? []} currentPlayerId={player?.userId ?? ''} />
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
                    <ChatMessages messages={globalChatRoom?.messages ?? []} currentPlayerId={player?.userId ?? ''} />
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
              </Stack>
            </Paper>
          </Box>
        </Stack>
      </Paper>
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
      {showRebirthTutorial && (
        <SpotlightTutorial
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
    </Container>
  )
}

export default Home
