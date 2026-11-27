import { useContext } from 'react'
import { NotificationContext } from '@/contexts/NotificationContext'

/** 画面の隅にトースト通知を出す（notify.success(message) など）。操作の成功・失敗の知らせに使う */
export function useNotify() {
  const context = useContext(NotificationContext)

  if (!context) {
    throw new Error('useNotify must be used within a NotificationProvider')
  }

  return context
}
