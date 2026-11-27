import { createContext } from 'react'

export type NotificationSeverity = 'success' | 'info' | 'warning' | 'error'

export type Notifier = Record<NotificationSeverity, (message: string) => void>

export const NotificationContext = createContext<Notifier | undefined>(undefined)
