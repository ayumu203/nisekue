import { useEffect } from 'react'
import type { RefObject } from 'react'

type UseMobileScrollToRefOptions = {
  enabled?: boolean
  mediaQuery?: string
  maxAttempts?: number
  intervalMs?: number
  offsetTop?: number
}

export function useMobileScrollToRef(
  targetRef: RefObject<HTMLElement | null>,
  options: UseMobileScrollToRefOptions = {},
): void {
  const {
    enabled = true,
    mediaQuery = '(max-width:899.95px)',
    maxAttempts = 20,
    intervalMs = 50,
    offsetTop = 0,
  } = options

  useEffect(() => {
    if (typeof window === 'undefined' || !enabled) {
      return
    }

    if (!window.matchMedia(mediaQuery).matches) {
      return
    }

    let attempts = 0
    const timerId = window.setInterval(() => {
      const target = targetRef.current
      if (!target) {
        attempts += 1
        if (attempts >= maxAttempts) {
          window.clearInterval(timerId)
        }

        return
      }

      const targetTop = target.getBoundingClientRect().top + window.scrollY - offsetTop
      window.scrollTo({ top: Math.max(0, targetTop), left: 0, behavior: 'auto' })
      window.clearInterval(timerId)
    }, intervalMs)

    return () => {
      window.clearInterval(timerId)
    }
  }, [enabled, intervalMs, maxAttempts, mediaQuery, offsetTop, targetRef])
}
