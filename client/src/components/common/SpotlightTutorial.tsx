import { Box, Button, Typography, useMediaQuery } from '@mui/material'
import { keyframes, useTheme } from '@mui/material/styles'
import { useLayoutEffect, useState } from 'react'
import { useAuth } from '@/contexts/useAuth'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { resolvePublicAssetPath } from '@/lib/assets'
import { completeTutorial } from '@/lib/tutorial'
import tutorialLocale from '../../../locale/tutorial/Tutorial.json'

type TargetRect = {
  top: number
  left: number
  width: number
  height: number
}

type SpotlightTutorialProps = {
  targetId?: string
  message: string
  subMessage?: string
  showDismiss?: boolean
  dismissLabel?: string
  onDismiss?: () => void
  padding?: number
}

// 実際の画面（歯車ボタン・外枠）の配色に合わせる
const WINDOW_COLOR = '#6e4415'
const WINDOW_BORDER_COLOR = '#ffffff'
const ACCENT_COLOR = '#ffcc00'
const ACCENT_TEXT_COLOR = '#4f392c'
const OVERLAY_COLOR = 'rgba(20, 12, 4, 0.62)'
const GUIDE_IMAGE_PATH = 'image/battle/character_daitenshi_01_02_gold.png'
const POINTER_HEIGHT = 44
const POINTER_GAP = 6

const pointDown = keyframes`
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(8px); }
`

const pointUp = keyframes`
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-8px); }
`

const float = keyframes`
  0%, 100% { transform: translateY(0); }
  50% { transform: translateY(-3px); }
`

const blink = keyframes`
  0%, 49% { opacity: 1; }
  50%, 100% { opacity: 0; }
`

const reducedMotionSx = {
  '@media (prefers-reduced-motion: reduce)': {
    animation: 'none',
  },
} as const

function useTargetRect(targetId: string | undefined, padding: number): TargetRect | null {
  const [rect, setRect] = useState<TargetRect | null>(null)

  useLayoutEffect(() => {
    if (!targetId) return

    function measure() {
      const el = document.getElementById(targetId!)
      if (!el) {
        setRect(null)
        return
      }

      const r = el.getBoundingClientRect()
      setRect({
        top: r.top - padding,
        left: r.left - padding,
        width: r.width + padding * 2,
        height: r.height + padding * 2,
      })
    }

    measure()
    window.addEventListener('resize', measure)
    window.addEventListener('scroll', measure, true)

    const observer = new ResizeObserver(measure)
    observer.observe(document.documentElement)

    return () => {
      window.removeEventListener('resize', measure)
      window.removeEventListener('scroll', measure, true)
      observer.disconnect()
    }
  }, [targetId, padding])

  return targetId ? rect : null
}

function PointerArrow({ direction }: { direction: 'up' | 'down' }) {
  const path = direction === 'up' ? 'M14 1 L27 21 L1 21 Z' : 'M1 1 L27 1 L14 21 Z'

  return (
    <svg aria-hidden="true" width="28" height="22" viewBox="0 0 28 22">
      <path d={path} fill={ACCENT_COLOR} stroke={WINDOW_BORDER_COLOR} strokeWidth="2" strokeLinejoin="round" />
    </svg>
  )
}

export default function SpotlightTutorial({
  targetId,
  message,
  subMessage,
  showDismiss = false,
  dismissLabel = 'わかった！',
  onDismiss,
  padding = 12,
}: SpotlightTutorialProps) {
  const targetRect = useTargetRect(targetId, padding)
  const { session } = useAuth()
  const { canUseDebug } = useDebugStatus()
  const userId = session?.user.id ?? null

  // デバッグ機能を使えるときだけ、チュートリアルをまとめて完了扱いにして閉じられる
  function handleSkip(): void {
    if (!userId) {
      return
    }

    completeTutorial(userId)
    window.location.reload()
  }

  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const hasSpotlight = targetId != null && targetRect != null

  // 指し先が画面の下半分にあるときは、ウィンドウを上に出して指し先を隠さない
  const viewportHeight = typeof window === 'undefined' ? 0 : window.innerHeight
  const viewportWidth = typeof window === 'undefined' ? 0 : window.innerWidth
  const targetCenterY = hasSpotlight ? targetRect.top + targetRect.height / 2 : 0
  const isTargetInLowerHalf = hasSpotlight && targetCenterY > viewportHeight / 2
  const pointerDirection: 'up' | 'down' = isTargetInLowerHalf ? 'down' : 'up'
  const pointerTop = hasSpotlight
    ? isTargetInLowerHalf
      ? targetRect.top - POINTER_GAP - POINTER_HEIGHT
      : targetRect.top + targetRect.height + POINTER_GAP
    : 0
  const pointerCenterX = hasSpotlight
    ? Math.min(Math.max(targetRect.left + targetRect.width / 2, 40), Math.max(viewportWidth - 40, 40))
    : 0

  const portraitSize = isMobile ? { width: 72, height: 62, image: 66 } : { width: 92, height: 78, image: 86 }

  return (
    <Box
      sx={{
        position: 'fixed',
        inset: 0,
        zIndex: 9999,
        pointerEvents: 'none',
      }}
    >
      {hasSpotlight && (
        <>
          <Box
            sx={{
              position: 'absolute',
              top: targetRect.top,
              left: targetRect.left,
              width: targetRect.width,
              height: targetRect.height,
              borderRadius: 2,
              boxShadow: `0 0 0 9999px ${OVERLAY_COLOR}`,
              border: `3px solid ${ACCENT_COLOR}`,
              boxSizing: 'border-box',
              pointerEvents: 'none',
            }}
          />
          <Box
            sx={{
              position: 'absolute',
              top: pointerTop,
              left: pointerCenterX,
              transform: 'translateX(-50%)',
              pointerEvents: 'none',
            }}
          >
            <Box
              sx={{
                display: 'flex',
                flexDirection: pointerDirection === 'up' ? 'column' : 'column-reverse',
                alignItems: 'center',
                gap: '2px',
                height: POINTER_HEIGHT,
                justifyContent: pointerDirection === 'up' ? 'flex-start' : 'flex-end',
                animation: `${pointerDirection === 'up' ? pointUp : pointDown} 0.9s ease-in-out infinite`,
                ...reducedMotionSx,
              }}
            >
              <PointerArrow direction={pointerDirection} />
              <Typography
                component="span"
                sx={{ fontSize: 15, lineHeight: 1, color: ACCENT_COLOR, whiteSpace: 'nowrap' }}
              >
                {tutorialLocale.pointerLabel}
              </Typography>
            </Box>
          </Box>
        </>
      )}

      <Box
        sx={{
          position: 'absolute',
          left: '50%',
          transform: 'translateX(-50%)',
          width: 'calc(100% - 24px)',
          maxWidth: 560,
          ...(isTargetInLowerHalf ? { top: 28 } : { bottom: 20 }),
          pointerEvents: 'auto',
        }}
      >
        <Box
          sx={{
            position: 'absolute',
            top: -18,
            left: 16,
            zIndex: 1,
            px: 1.75,
            py: 0.5,
            backgroundColor: ACCENT_COLOR,
            border: `3px solid ${WINDOW_BORDER_COLOR}`,
            borderRadius: '6px',
          }}
        >
          <Typography component="span" sx={{ fontSize: 14, lineHeight: 1.2, color: ACCENT_TEXT_COLOR }}>
            {tutorialLocale.guideName}
          </Typography>
        </Box>

        <Box
          role="dialog"
          aria-label={tutorialLocale.guideName}
          sx={{
            display: 'flex',
            gap: 1.5,
            alignItems: 'flex-start',
            pt: 2.75,
            pb: 2,
            px: 2,
            backgroundColor: WINDOW_COLOR,
            border: `3px solid ${WINDOW_BORDER_COLOR}`,
            outline: `3px solid ${WINDOW_COLOR}`,
            borderRadius: '8px',
          }}
        >
          <Box
            sx={{
              flexShrink: 0,
              width: portraitSize.width,
              height: portraitSize.height,
              boxSizing: 'border-box',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              backgroundColor: '#fff7e8',
              border: `2px solid ${WINDOW_BORDER_COLOR}`,
              borderRadius: '6px',
              animation: `${float} 1.6s ease-in-out infinite`,
              ...reducedMotionSx,
            }}
          >
            <Box
              component="img"
              src={resolvePublicAssetPath(GUIDE_IMAGE_PATH)}
              alt=""
              aria-hidden="true"
              sx={{ width: portraitSize.image, height: 'auto', imageRendering: 'pixelated' }}
            />
          </Box>

          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography
              sx={{ color: '#ffffff', whiteSpace: 'pre-line', lineHeight: 1.7, fontSize: { xs: 15, sm: 16 } }}
            >
              {message}
            </Typography>
            {subMessage && (
              <Typography
                sx={{
                  color: '#fff1d6',
                  whiteSpace: 'pre-line',
                  lineHeight: 1.7,
                  fontSize: 14,
                  mt: 1,
                  pt: 1,
                  borderTop: '1px solid rgba(255, 255, 255, 0.35)',
                }}
              >
                {subMessage}
              </Typography>
            )}
            {((showDismiss && onDismiss) || (canUseDebug && userId)) && (
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 1, mt: 1.5 }}>
                {canUseDebug && userId && (
                  <Button size="small" variant="text" onClick={handleSkip} sx={{ color: '#fff1d6' }}>
                    {tutorialLocale.debugSkip}
                  </Button>
                )}
                {showDismiss && onDismiss && (
                  <Button
                    size="small"
                    variant="contained"
                    onClick={onDismiss}
                    sx={{
                      '&&': {
                        borderRadius: 999,
                        backgroundColor: ACCENT_COLOR,
                        borderColor: WINDOW_BORDER_COLOR,
                        color: ACCENT_TEXT_COLOR,
                        boxShadow: 'none',
                      },
                      '&&:hover': {
                        backgroundColor: '#f2c200',
                        boxShadow: 'none',
                      },
                    }}
                  >
                    {dismissLabel}
                    <Box
                      component="span"
                      aria-hidden="true"
                      sx={{ ml: 0.75, animation: `${blink} 1s steps(1) infinite`, ...reducedMotionSx }}
                    >
                      ▼
                    </Box>
                  </Button>
                )}
              </Box>
            )}
          </Box>
        </Box>
      </Box>
    </Box>
  )
}
