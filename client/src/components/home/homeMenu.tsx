import type { ReactNode } from 'react'
import locale from '../../../locale/home/Home.json'
import {
  DebugIcon,
  ItemsIcon,
  JobChangeIcon,
  MoveSettingIcon,
  OthersGroupIcon,
  PetsIcon,
  QuestIcon,
  RankingIcon,
  RebirthIcon,
  ReportIcon,
  SocialGroupIcon,
  SpecialThanksIcon,
  SurveyIcon,
  ThreadsIcon,
  TrainingGroupIcon,
  TrainingIcon,
  TreasureMapIcon,
  VisitPlayersIcon,
} from '@/components/home/HomeIcons'
import { SignOutIcon } from '@/components/home/StatusIcons'
import { setTutorialStep, type TutorialStep } from '@/lib/tutorial'

const SURVEY_URL =
  'https://docs.google.com/forms/d/e/1FAIpQLScQGhSmvuy99jOyKKvHIYtKcbZOGpYucamzrza5CRKezS054A/viewform?usp=dialog'
const REPORT_URL =
  'https://docs.google.com/forms/d/e/1FAIpQLSduEn4lqqHBp4zuaXKehG3w4DQBXWmp2GdkMNU1tqO5_VAQUw/viewform?usp=publish-editor'

export type HomeMenuGroupKey = 'enhance' | 'social' | 'others'

export type HomeMenuItem = {
  key: string
  label: string
  icon: ReactNode
  // アプリ内のページへ移動するとき
  to?: string
  // 外部のページを新しいタブで開くとき
  href?: string
  // チュートリアルのスポットライトが探す要素の id
  id?: string
  onClick?: () => void
  // ログアウトのように、気をつけて押してほしい項目
  danger?: boolean
  disabled?: boolean
}

export type HomeMenuGroup = {
  key: HomeMenuGroupKey
  label: string
  icon: ReactNode
  items: HomeMenuItem[]
}

type BuildHomeMenuParams = {
  userId: string | null
  tutorialStep: TutorialStep | null
  showRebirthTutorial: boolean
  showSurveyGuide: boolean
  onSurveyClick: () => void
  canUseDebug: boolean
  // 渡したときだけ「その他」にログアウトを入れる（スマホ用。PC はヘッダーにある）
  signOut?: { onClick: () => void; disabled: boolean }
}

// ホームのメニュー。PC のヘッダーメニューとスマホの縦並びメニューで同じ定義を使う
export function buildHomeMenu({
  userId,
  tutorialStep,
  showRebirthTutorial,
  showSurveyGuide,
  onSurveyClick,
  canUseDebug,
  signOut,
}: BuildHomeMenuParams): { primary: HomeMenuItem[]; groups: HomeMenuGroup[] } {
  // 指定したステップのときだけ、クリックで次のステップへ進める
  const advanceOn = (step: TutorialStep, next: TutorialStep) => () => {
    if (userId && tutorialStep === step) {
      setTutorialStep(userId, next)
    }
  }

  const primary: HomeMenuItem[] = [
    {
      key: 'quest',
      label: locale.quest,
      icon: <QuestIcon />,
      to: '/quest',
      id: tutorialStep === 'home-quest' ? 'tutorial-quest-btn' : undefined,
      onClick: advanceOn('home-quest', 'quest-info'),
    },
    {
      key: 'training',
      label: locale.training,
      icon: <TrainingIcon />,
      to: '/training',
      id: 'tutorial-training-btn',
      onClick: advanceOn('home-training', 'training-fight'),
    },
    {
      key: 'treasureMap',
      label: locale.treasureMap,
      icon: <TreasureMapIcon />,
      to: '/treasure-map',
      id: tutorialStep === 'home-treasure-map' ? 'tutorial-treasure-map-btn' : undefined,
      onClick: advanceOn('home-treasure-map', 'treasure-map-info'),
    },
    { key: 'items', label: locale.items, icon: <ItemsIcon />, to: '/items' },
  ]

  const othersItems: HomeMenuItem[] = [
    { key: 'specialThanks', label: locale.specialThanks, icon: <SpecialThanksIcon />, to: '/thanks' },
    {
      key: 'survey',
      label: locale.survey,
      icon: <SurveyIcon />,
      href: SURVEY_URL,
      id: showSurveyGuide ? 'tutorial-survey-btn' : undefined,
      onClick: () => {
        if (showSurveyGuide) {
          onSurveyClick()
        }
      },
    },
    { key: 'report', label: locale.report, icon: <ReportIcon />, href: REPORT_URL },
  ]

  if (canUseDebug) {
    othersItems.push({ key: 'debug', label: locale.debug, icon: <DebugIcon />, to: '/debug' })
  }

  if (signOut) {
    othersItems.push({
      key: 'signOut',
      label: locale.signOut,
      icon: <SignOutIcon />,
      onClick: signOut.onClick,
      disabled: signOut.disabled,
      danger: true,
    })
  }

  const groups: HomeMenuGroup[] = [
    {
      key: 'enhance',
      label: locale.trainingGroup,
      icon: <TrainingGroupIcon />,
      items: [
        {
          key: 'moveSetting',
          label: locale.moveSetting,
          icon: <MoveSettingIcon />,
          to: '/move-setting',
          id: tutorialStep === 'home-move-setting' ? 'tutorial-move-setting-btn' : undefined,
          onClick: advanceOn('home-move-setting', 'move-setting-info'),
        },
        {
          key: 'jobChange',
          label: locale.jobChange,
          icon: <JobChangeIcon />,
          to: '/job-change',
          id: 'tutorial-job-change-btn',
          onClick: advanceOn('home-job-change', 'job-change-info'),
        },
        {
          key: 'rebirth',
          label: locale.rebirth,
          icon: <RebirthIcon />,
          to: '/rebirth',
          id: showRebirthTutorial ? 'tutorial-rebirth-btn' : undefined,
        },
        { key: 'pets', label: locale.pets, icon: <PetsIcon />, to: '/pets' },
      ],
    },
    {
      key: 'social',
      label: locale.socialGroup,
      icon: <SocialGroupIcon />,
      items: [
        { key: 'visitPlayers', label: locale.visitPlayers, icon: <VisitPlayersIcon />, to: '/players' },
        { key: 'threads', label: locale.threads, icon: <ThreadsIcon />, to: '/threads' },
        { key: 'ranking', label: locale.ranking, icon: <RankingIcon />, to: '/ranking' },
      ],
    },
    { key: 'others', label: locale.others, icon: <OthersGroupIcon />, items: othersItems },
  ]

  return { primary, groups }
}

// チュートリアル中に開いたままにしておくグループ。案内している項目がグループの中にあるため
export function getTutorialOpenGroup(
  tutorialStep: TutorialStep | null,
  showRebirthTutorial: boolean,
  showSurveyGuide: boolean,
): HomeMenuGroupKey | null {
  if (tutorialStep === 'home-job-change' || tutorialStep === 'home-move-setting' || showRebirthTutorial) {
    return 'enhance'
  }

  if (showSurveyGuide) {
    return 'others'
  }

  return null
}
