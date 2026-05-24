export type TutorialStep =
  | 'home-training'
  | 'training-fight'
  | 'training-confirm'
  | 'training-to-lv5'
  | 'home-job-change'
  | 'job-change-info'
  | 'training-to-lv7'
  | 'training-quest-guide'
  | 'completed'

const KEY_PREFIX = 'nisekue:tutorial'
const DEFAULT_TUTORIAL_STEP: TutorialStep = 'home-training'
const TUTORIAL_STEPS: readonly TutorialStep[] = [
  'home-training',
  'training-fight',
  'training-confirm',
  'training-to-lv5',
  'home-job-change',
  'job-change-info',
  'training-to-lv7',
  'training-quest-guide',
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
