import { Box, Button, Typography, useMediaQuery } from '@mui/material'
import { keyframes, useTheme } from '@mui/material/styles'
import { loadDefaultJapaneseParser } from 'budoux'
import { Fragment, useLayoutEffect, useState } from 'react'
import { useAuth } from '@/contexts/useAuth'
import { useDebugStatus } from '@/hooks/useDebugStatus'
import { resolvePublicAssetPath } from '@/lib/assets'
import { completeTutorial } from '@/lib/tutorial'
import tutorialLocale from '../../../locale/tutorial/Tutorial.json'
import { getGuidePortraitPlacement, TUTORIAL_GUIDES, type TutorialGuideId } from './tutorialGuides'
import { font, palette, withAlpha } from '@/theme/tokens'

type TargetRect = {
  top: number
  left: number
  width: number
  height: number
}

type SpotlightTutorialProps = {
  targetId?: string
  // 矢印で指す要素。指定がなければ targetId を指す
  pointerTargetId?: string
  message: string
  subMessage?: string
  showDismiss?: boolean
  dismissLabel?: string
  onDismiss?: () => void
  padding?: number
  // 話しているキャラ。指定がなければ受付嬢
  guide?: TutorialGuideId
  // 周りを暗くするか。ほかの所も自由に操作してよい場面では false にして、枠と矢印だけで示す
  dimBackground?: boolean
}

// 実際の画面（歯車ボタン・外枠）の配色に合わせる
const WINDOW_COLOR = palette.brown500
const WINDOW_OUTLINE_COLOR = palette.brown500
const WINDOW_BORDER_COLOR = palette.white
const ACCENT_COLOR = palette.yellow
const ACCENT_TEXT_COLOR = palette.brown900
const BUBBLE_COLOR = palette.cream
const BUBBLE_TEXT_COLOR = palette.brown900
// 背景が明るくても暗くても読めるように、ラベルを茶色で縁取る
const POINTER_LABEL_OUTLINE = [
  [-2, 0],
  [2, 0],
  [0, -2],
  [0, 2],
  [-2, -2],
  [2, 2],
  [-2, 2],
  [2, -2],
]
  .map(([x, y]) => `${x}px ${y}px 0 ${WINDOW_OUTLINE_COLOR}`)
  .join(', ')
const OVERLAY_COLOR = withAlpha(palette.black, 0.75)
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

const blink = keyframes`
  0%, 49% { opacity: 1; }
  50%, 100% { opacity: 0; }
`

const reducedMotionSx = {
  '@media (prefers-reduced-motion: reduce)': {
    animation: 'none',
  },
} as const

const phraseParser = loadDefaultJapaneseParser()

// 文節の切れ目でだけ折り返すように <wbr> を挟む（wordBreak: keep-all と組み合わせる）
const renderPhrases = (text: string) =>
  text.split('\n').map((line, lineIndex) => (
    <Fragment key={lineIndex}>
      {lineIndex > 0 && <br />}
      {phraseParser.parse(line).map((phrase, phraseIndex) => (
        <Fragment key={phraseIndex}>
          {phraseIndex > 0 && <wbr />}
          {phrase}
        </Fragment>
      ))}
    </Fragment>
  ))

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
      const next = {
        top: r.top - padding,
        left: r.left - padding,
        width: r.width + padding * 2,
        height: r.height + padding * 2,
      }
      // 位置が変わらないときは更新しない（DOM の変化のたびに描き直さないように）
      setRect((prev) =>
        prev &&
        prev.top === next.top &&
        prev.left === next.left &&
        prev.width === next.width &&
        prev.height === next.height
          ? prev
          : next,
      )
    }

    measure()
    window.addEventListener('resize', measure)
    window.addEventListener('scroll', measure, true)

    const observer = new ResizeObserver(measure)
    observer.observe(document.documentElement)

    // 指す要素が後から表示される（読み込み後に一覧が出るなど）ときにも探し直す。
    // 全画面のレイアウトではページ全体の大きさが変わらず、ResizeObserver では気づけないため
    let frame = 0
    const mutationObserver = new MutationObserver(() => {
      if (frame) return
      frame = requestAnimationFrame(() => {
        frame = 0
        measure()
      })
    })
    mutationObserver.observe(document.body, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ['id'],
    })

    return () => {
      window.removeEventListener('resize', measure)
      window.removeEventListener('scroll', measure, true)
      observer.disconnect()
      mutationObserver.disconnect()
      if (frame) cancelAnimationFrame(frame)
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
  dismissLabel = '次へ',
  onDismiss,
  padding = 12,
  guide = 'receptionist',
  dimBackground = true,
  pointerTargetId,
}: SpotlightTutorialProps) {
  const guideInfo = TUTORIAL_GUIDES[guide]
  const portraitPlacement = getGuidePortraitPlacement(guideInfo)
  const targetRect = useTargetRect(targetId, padding)
  const pointerTargetRect = useTargetRect(pointerTargetId, 4)
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
  // 画面幅ではなく入力方法で判定する（タブレットや狭いウィンドウの PC でも正しい表記になるように）
  const isTouchDevice = useMediaQuery('(pointer: coarse)')
  const hasSpotlight = targetId != null && targetRect != null

  // 指し先が画面の下半分にあるときは、ウィンドウを上に出して指し先を隠さない（矢印の指し先があればそちらを優先）
  const viewportHeight = typeof window === 'undefined' ? 0 : window.innerHeight
  const viewportWidth = typeof window === 'undefined' ? 0 : window.innerWidth
  const targetCenterY = hasSpotlight ? targetRect.top + targetRect.height / 2 : 0
  const isTargetInLowerHalf = hasSpotlight && targetCenterY > viewportHeight / 2
  const pointerRect = pointerTargetRect ?? targetRect
  const pointerCenterY = pointerRect ? pointerRect.top + pointerRect.height / 2 : 0
  const pointerDirection: 'up' | 'down' = pointerRect && pointerCenterY > viewportHeight / 2 ? 'down' : 'up'
  const pointerTop = pointerRect
    ? pointerDirection === 'down'
      ? pointerRect.top - POINTER_GAP - POINTER_HEIGHT
      : pointerRect.top + pointerRect.height + POINTER_GAP
    : 0
  const pointerCenterX = pointerRect
    ? Math.min(Math.max(pointerRect.left + pointerRect.width / 2, 40), Math.max(viewportWidth - 40, 40))
    : 0

  const portraitSize = isMobile ? { width: 76, minHeight: 112 } : { width: 140, minHeight: 168 }

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
              boxShadow: dimBackground ? `0 0 0 9999px ${OVERLAY_COLOR}` : 'none',
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
                gap: 0.25,
                height: POINTER_HEIGHT,
                justifyContent: pointerDirection === 'up' ? 'flex-start' : 'flex-end',
                animation: `${pointerDirection === 'up' ? pointUp : pointDown} 0.9s ease-in-out infinite`,
                ...reducedMotionSx,
              }}
            >
              <PointerArrow direction={pointerDirection} />
              <Typography
                component="span"
                sx={{
                  fontSize: font.md,
                  fontWeight: 700,
                  lineHeight: 1,
                  color: palette.white,
                  textShadow: POINTER_LABEL_OUTLINE,
                  whiteSpace: 'nowrap',
                }}
              >
                {isTouchDevice ? tutorialLocale.pointerLabelTap : tutorialLocale.pointerLabelClick}
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
          maxWidth: 760,
          ...((pointerTargetRect ? pointerDirection === 'down' : isTargetInLowerHalf) ? { top: 28 } : { bottom: 20 }),
          pointerEvents: 'auto',
        }}
      >
        <Box
          role="dialog"
          aria-label={guideInfo.name}
          sx={{
            display: 'flex',
            gap: { xs: 1.5, sm: 2.5 },
            alignItems: 'stretch',
            py: { xs: 2, sm: 2.5 },
            px: { xs: 2, sm: 2.5 },
            backgroundColor: WINDOW_COLOR,
            border: `3px solid ${WINDOW_BORDER_COLOR}`,
            outline: `3px solid ${WINDOW_OUTLINE_COLOR}`,
            borderRadius: '8px',
          }}
        >
          <Box
            sx={{
              flexShrink: 0,
              width: portraitSize.width,
              minHeight: portraitSize.minHeight,
              position: 'relative',
              boxSizing: 'border-box',
              overflow: 'hidden',
              backgroundColor: palette.cream,
              border: `2px solid ${WINDOW_BORDER_COLOR}`,
              borderRadius: '6px',
            }}
          >
            <Box
              component="img"
              src={resolvePublicAssetPath(guideInfo.image)}
              alt=""
              aria-hidden="true"
              sx={{
                position: 'absolute',
                top: 0,
                left: `${portraitPlacement.left * 100}%`,
                width: `${guideInfo.zoom * 100}%`,
                maxWidth: 'none',
                // margin の % は親の横幅が基準なので、顔の高さを枠の横幅に対する割合で合わせられる
                mt: `${portraitPlacement.top * 100}%`,
              }}
            />
          </Box>

          <Box sx={{ flexGrow: 1, minWidth: 0 }}>
            <Typography
              component="p"
              sx={{
                mb: 0.75,
                fontSize: { xs: font.xl, sm: font.xxl },
                fontWeight: 700,
                lineHeight: 1.2,
                letterSpacing: '0.05em',
                color: guideInfo.nameColor,
              }}
            >
              {guideInfo.name}
            </Typography>
            <Box
              sx={{
                position: 'relative',
                px: { xs: 1.5, sm: 2.5 },
                py: { xs: 1, sm: 1.5 },
                backgroundColor: BUBBLE_COLOR,
                borderRadius: '14px',
                // 立ち絵の方を向いた吹き出しのしっぽ
                '&::before': {
                  content: '""',
                  position: 'absolute',
                  top: { xs: 14, sm: 18 },
                  left: -11,
                  borderStyle: 'solid',
                  borderWidth: '8px 12px 8px 0',
                  borderColor: `transparent ${BUBBLE_COLOR} transparent transparent`,
                },
              }}
            >
              <Typography
                sx={{
                  color: BUBBLE_TEXT_COLOR,
                  wordBreak: 'keep-all',
                  overflowWrap: 'anywhere',
                  lineHeight: 1.7,
                  fontSize: { xs: font.md, sm: font.xl },
                }}
              >
                {renderPhrases(message)}
              </Typography>
              {subMessage && (
                <Typography
                  sx={{
                    color: palette.brown900,
                    wordBreak: 'keep-all',
                    overflowWrap: 'anywhere',
                    lineHeight: 1.7,
                    fontSize: { xs: font.sm, sm: font.md },
                    mt: 1,
                    pt: 1,
                    borderTop: `1px dashed ${withAlpha(palette.brown900, 0.25)}`,
                  }}
                >
                  {renderPhrases(subMessage)}
                </Typography>
              )}
            </Box>
            {((showDismiss && onDismiss) || (canUseDebug && userId)) && (
              <Box sx={{ display: 'flex', justifyContent: 'flex-end', alignItems: 'center', gap: 1, mt: 1.5 }}>
                {canUseDebug && userId && (
                  <Button
                    size="small"
                    variant="text"
                    onClick={handleSkip}
                    sx={{ color: palette.cream, fontSize: { xs: font.sm, sm: font.md } }}
                  >
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
                        fontSize: { xs: font.sm, sm: font.md },
                        px: { xs: 2, sm: 2.5 },
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
