import { useCallback, useEffect, useMemo, useRef, useState, type ReactNode } from 'react'
import { Alert, Box, Slide } from '@mui/material'
import { NotificationContext, type Notifier, type NotificationSeverity } from '@/contexts/NotificationContext'
import { palette, withAlpha } from '@/theme/tokens'

type Toast = {
  id: number
  message: string
  severity: NotificationSeverity
  open: boolean
}

/** 同時に出す件数。超えたら古いものから閉じる */
const MAX_VISIBLE = 3

/** 表示時間（ミリ秒）。失敗・注意は読み切れるよう長めにする */
const DURATION: Record<NotificationSeverity, number> = {
  success: 3000,
  info: 3000,
  warning: 5000,
  error: 5000,
}

const BACKGROUND: Record<NotificationSeverity, string> = {
  success: palette.green700,
  info: palette.blue700,
  warning: palette.gold700,
  error: palette.red700,
}

export function NotificationProvider({ children }: { children: ReactNode }) {
  const [toasts, setToasts] = useState<Toast[]>([])
  const nextIdRef = useRef(1)
  const timersRef = useRef(new Map<number, ReturnType<typeof setTimeout>>())

  const close = useCallback((id: number) => {
    const timer = timersRef.current.get(id)
    if (timer) {
      clearTimeout(timer)
      timersRef.current.delete(id)
    }
    setToasts((current) => current.map((toast) => (toast.id === id ? { ...toast, open: false } : toast)))
  }, [])

  const remove = useCallback((id: number) => {
    setToasts((current) => current.filter((toast) => toast.id !== id))
  }, [])

  const notify = useCallback(
    (message: string, severity: NotificationSeverity) => {
      if (!message) {
        return
      }
      const id = nextIdRef.current
      nextIdRef.current += 1
      setToasts((current) => {
        const visible = current.filter((toast) => toast.open)
        const overflow = visible.slice(0, Math.max(0, visible.length - (MAX_VISIBLE - 1))).map((toast) => toast.id)
        return [
          ...current.map((toast) => (overflow.includes(toast.id) ? { ...toast, open: false } : toast)),
          { id, message, severity, open: true },
        ]
      })
      timersRef.current.set(
        id,
        setTimeout(() => close(id), DURATION[severity]),
      )
    },
    [close],
  )

  useEffect(() => {
    const timers = timersRef.current
    return () => {
      timers.forEach((timer) => clearTimeout(timer))
      timers.clear()
    }
  }, [])

  const value = useMemo<Notifier>(
    () => ({
      success: (message) => notify(message, 'success'),
      info: (message) => notify(message, 'info'),
      warning: (message) => notify(message, 'warning'),
      error: (message) => notify(message, 'error'),
    }),
    [notify],
  )

  return (
    <NotificationContext.Provider value={value}>
      {children}
      <Box
        aria-live="polite"
        sx={{
          position: 'fixed',
          zIndex: (theme) => theme.zIndex.snackbar,
          // スマホは下中央に幅いっぱい、PC は右下
          left: { xs: 8, md: 'auto' },
          right: { xs: 8, md: 24 },
          bottom: { xs: 16, md: 24 },
          width: { md: 360 },
          display: 'flex',
          flexDirection: 'column',
          gap: 1,
          pointerEvents: 'none',
        }}
      >
        {toasts.map((toast) => (
          <Slide
            key={toast.id}
            in={toast.open}
            direction="up"
            appear
            mountOnEnter
            unmountOnExit
            onExited={() => remove(toast.id)}
          >
            <Alert
              severity={toast.severity}
              variant="filled"
              onClose={() => close(toast.id)}
              sx={{
                pointerEvents: 'auto',
                alignItems: 'center',
                bgcolor: BACKGROUND[toast.severity],
                color: palette.white,
                boxShadow: `0 4px 12px ${withAlpha(palette.black, 0.25)}`,
              }}
            >
              {toast.message}
            </Alert>
          </Slide>
        ))}
      </Box>
    </NotificationContext.Provider>
  )
}
