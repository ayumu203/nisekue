import { useState } from 'react'

type Message = { severity: 'success' | 'error'; text: string }

/** デバッグ操作の実行中フラグと結果メッセージをまとめて扱う */
export function useDebugAction() {
  const [isRunning, setIsRunning] = useState(false)
  const [message, setMessage] = useState<Message | null>(null)

  async function run(action: () => Promise<void>, successText: string | (() => string)): Promise<void> {
    setIsRunning(true)
    setMessage(null)
    try {
      await action()
      setMessage({ severity: 'success', text: typeof successText === 'function' ? successText() : successText })
    } catch (error) {
      setMessage({ severity: 'error', text: error instanceof Error ? error.message : '失敗しました' })
    } finally {
      setIsRunning(false)
    }
  }

  return { isRunning, message, run }
}
