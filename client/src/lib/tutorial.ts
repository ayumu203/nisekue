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

function getStorageKey(userId: string): string {
  return `${KEY_PREFIX}:${userId}:step`
}

export function getTutorialStep(userId: string): TutorialStep {
  if (typeof window === 'undefined') {
    return 'home-training'
  }

  try {
    const stored = window.localStorage.getItem(getStorageKey(userId))
    if (!stored) {
      return 'home-training'
    }

    return stored as TutorialStep
  } catch {
    return 'home-training'
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
