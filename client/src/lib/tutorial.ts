export type TutorialStep =
  | 'home-player-setting'
  | 'player-setting-random'
  | 'player-setting-save'
  | 'player-setting-account'
  | 'player-setting-to-home'
  | 'home-training'
  | 'training-fight'
  | 'training-confirm'
  | 'training-to-lv5'
  | 'home-job-change'
  | 'job-change-info'
  | 'job-change-roadmap'
  | 'training-to-lv7'
  | 'training-quest-guide'
  | 'home-move-setting'
  | 'move-setting-info'
  | 'home-treasure-map'
  | 'treasure-map-info'
  | 'treasure-map-to-home'
  | 'completed'

const KEY_PREFIX = 'nisekue:tutorial'
const REBIRTH_SHOWN_KEY_SUFFIX = 'rebirth-shown'
const DEFAULT_TUTORIAL_STEP: TutorialStep = 'home-player-setting'
const TUTORIAL_STEPS: readonly TutorialStep[] = [
  'home-player-setting',
  'player-setting-random',
  'player-setting-save',
  'player-setting-account',
  'player-setting-to-home',
  'home-training',
  'training-fight',
  'training-confirm',
  'training-to-lv5',
  'home-job-change',
  'job-change-info',
  'job-change-roadmap',
  'training-to-lv7',
  'training-quest-guide',
  'home-move-setting',
  'move-setting-info',
  'home-treasure-map',
  'treasure-map-info',
  'treasure-map-to-home',
  'completed',
]

function getStorageKey(userId: string): string {
  return `${KEY_PREFIX}:${userId}:step`
}

function isTutorialStep(value: string): value is TutorialStep {
  return TUTORIAL_STEPS.includes(value as TutorialStep)
}

export function getTutorialStep(userId: string): TutorialStep {
  if (typeof window === 'undefined') {
    return DEFAULT_TUTORIAL_STEP
  }

  try {
    const storageKey = getStorageKey(userId)
    const stored = window.localStorage.getItem(storageKey)
    if (!stored) {
      return DEFAULT_TUTORIAL_STEP
    }

    if (isTutorialStep(stored)) {
      return stored
    }

    window.localStorage.setItem(storageKey, DEFAULT_TUTORIAL_STEP)
    return DEFAULT_TUTORIAL_STEP
  } catch {
    return DEFAULT_TUTORIAL_STEP
  }
}

export function setTutorialStep(userId: string, step: TutorialStep): void {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.setItem(getStorageKey(userId), step)
  } catch {
    // localStorage が利用できない環境でも継続する
  }
}

export function isTutorialComplete(userId: string): boolean {
  return getTutorialStep(userId) === 'completed'
}

function getRebirthShownKey(userId: string): string {
  return `${KEY_PREFIX}:${userId}:${REBIRTH_SHOWN_KEY_SUFFIX}`
}

export function isRebirthTutorialShown(userId: string): boolean {
  if (typeof window === 'undefined') {
    return false
  }

  try {
    return window.localStorage.getItem(getRebirthShownKey(userId)) === 'true'
  } catch {
    return false
  }
}

export function setRebirthTutorialShown(userId: string): void {
  if (typeof window === 'undefined') {
    return
  }

  try {
    window.localStorage.setItem(getRebirthShownKey(userId), 'true')
  } catch {
    // localStorage が利用できない環境でも継続する
  }
}
