import { Box, Button, Paper, Typography, useMediaQuery } from '@mui/material'
import { useTheme } from '@mui/material/styles'
import { useLayoutEffect, useState } from 'react'

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
  messagePosition?: 'top' | 'bottom'
  showDismiss?: boolean
  dismissLabel?: string
  onDismiss?: () => void
  padding?: number
}

function useTargetRect(targetId: string | undefined, padding: number): TargetRect | null {
  const [rect, setRect] = useState<TargetRect | null>(null)

  useLayoutEffect(() => {
    if (!targetId) {
      return
    }

    function measure() {
      const el = document.getElementById(targetId!)
      if (!el) {
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

  return rect
}

export default function SpotlightTutorial({
  targetId,
  message,
  subMessage,
  messagePosition = 'bottom',
  showDismiss = false,
  dismissLabel = 'わかった！',
  onDismiss,
  padding = 12,
}: SpotlightTutorialProps) {
  const targetRect = useTargetRect(targetId, padding)

  const theme = useTheme()
  const isMobile = useMediaQuery(theme.breakpoints.down('sm'))
  const hasSpotlight = targetId != null && targetRect != null
  const viewportHeight = typeof window !== 'undefined' ? window.innerHeight : 800

  function getMessageTop(): number {
    if (!targetRect) {
      return isMobile ? viewportHeight * 0.6 : viewportHeight - 160
    }

    if (messagePosition === 'top') {
      return Math.max(8, targetRect.top - 120)
    }

    const below = targetRect.top + targetRect.height + 12
    if (below + 120 > viewportHeight) {
      return Math.max(8, targetRect.top - 120)
    }

    return below
  }

  function getMessageLeft(): number {
    if (!targetRect) {
      return 16
    }

    return Math.max(8, Math.min(targetRect.left, (typeof window !== 'undefined' ? window.innerWidth : 400) - 308))
  }

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
        <Box
          sx={{
            position: 'absolute',
            top: targetRect.top,
            left: targetRect.left,
            width: targetRect.width,
            height: targetRect.height,
            borderRadius: 2,
            boxShadow: '0 0 0 9999px rgba(0, 0, 0, 0.68)',
            pointerEvents: 'none',
          }}
        />
      )}

      <Paper
        elevation={8}
        sx={{
          position: 'absolute',
          top: getMessageTop(),
          ...(isMobile
            ? { left: '50%', transform: 'translateX(-50%)', width: 'calc(100% - 32px)' }
            : { left: hasSpotlight ? getMessageLeft() : 16, right: hasSpotlight ? 'auto' : 16 }),
          maxWidth: 300,
          p: 2,
          borderRadius: 3,
          backgroundColor: '#fff9ef',
          border: '1px solid #e8d49c',
          pointerEvents: 'auto',
        }}
      >
        <Typography variant="body2" fontWeight={700} sx={{ color: '#3a2c0f', whiteSpace: 'pre-line', lineHeight: 1.7 }}>
          {message}
        </Typography>
        {subMessage && (
          <Typography
            variant="body2"
            sx={{
              color: '#5c4a1e',
              whiteSpace: 'pre-line',
              lineHeight: 1.7,
              mt: 1,
              pt: 1,
              borderTop: '1px solid #e8d49c',
            }}
          >
            {subMessage}
          </Typography>
        )}
        {showDismiss && onDismiss && (
          <Button
            size="small"
            variant="contained"
            onClick={onDismiss}
            sx={{
              mt: 1.5,
              borderRadius: 999,
              fontWeight: 800,
              backgroundColor: '#b65f49',
              color: '#fff5ef',
              boxShadow: 'none',
              '&:hover': {
                backgroundColor: '#c96a52',
                boxShadow: 'none',
              },
            }}
          >
            {dismissLabel}
          </Button>
        )}
      </Paper>
    </Box>
  )
}
