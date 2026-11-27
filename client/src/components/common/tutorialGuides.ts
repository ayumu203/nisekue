import tutorialLocale from '../../../locale/tutorial/Tutorial.json'
import { palette } from '@/theme/tokens'

export type TutorialGuideId =
  | 'receptionist'
  | 'instructor'
  | 'priest'
  | 'mage'
  | 'adventurer'
  | 'captain'
  | 'highPriest'

type TutorialGuide = {
  name: string
  image: string
  // 顔の中心の位置（画像の横幅を 1 としたときの x, y）
  faceX: number
  faceY: number
  // 立ち絵の枠の横幅に対する画像の横幅の倍率
  zoom: number
  // 名前の色（キャラの髪色などに合わせる）
  nameColor: string
}

export const TUTORIAL_GUIDES: Record<TutorialGuideId, TutorialGuide> = {
  receptionist: {
    name: tutorialLocale.guides.receptionist,
    image: 'image/job/11_watermage.png',
    faceX: 0.52,
    faceY: 0.4,
    zoom: 1.35,
    nameColor: palette.sky,
  },
  instructor: {
    name: tutorialLocale.guides.instructor,
    image: 'image/job/08_trickster.png',
    faceX: 0.47,
    faceY: 0.38,
    zoom: 1.6,
    nameColor: palette.blue300,
  },
  priest: {
    name: tutorialLocale.guides.priest,
    image: 'image/job/13_highpriest.png',
    faceX: 0.45,
    faceY: 0.33,
    zoom: 1.9,
    nameColor: palette.pink,
  },
  mage: {
    name: tutorialLocale.guides.mage,
    image: 'image/job/03_mage.png',
    faceX: 0.18,
    faceY: 0.37,
    zoom: 1.8,
    nameColor: palette.brown200,
  },
  adventurer: {
    name: tutorialLocale.guides.adventurer,
    image: 'image/job/15_sniper.png',
    faceX: 0.48,
    faceY: 0.38,
    zoom: 1.45,
    nameColor: palette.cream,
  },
  captain: {
    name: tutorialLocale.guides.captain,
    image: 'image/job/21_grandranger.png',
    faceX: 0.49,
    faceY: 0.33,
    zoom: 1.5,
    nameColor: palette.sky,
  },
  highPriest: {
    name: tutorialLocale.guides.highPriest,
    image: 'image/job/20_grandpriest.png',
    faceX: 0.49,
    faceY: 0.48,
    zoom: 1.4,
    nameColor: palette.sky,
  },
}

// 枠の横幅を 1 としたとき、顔を置く高さ
const FACE_TARGET_Y = 0.5

/**
 * 顔が枠の中央に来るように、立ち絵の位置を枠の横幅に対する割合で返す。
 * 画像の端が枠の内側に入って余白が出ないように、ずらす量は画像の端までにとどめる。
 */
export function getGuidePortraitPlacement({ faceX, faceY, zoom }: TutorialGuide) {
  const left = Math.min(0, Math.max(1 - zoom, 0.5 - faceX * zoom))
  const top = Math.min(0, FACE_TARGET_Y - faceY * zoom)
  return { left, top }
}
