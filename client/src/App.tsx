import { useEffect, useState } from 'react'
import './App.css'

function App() {
  const [ message, setMessage ] = useState('')
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL ?? '/api'

  useEffect(() => {
    fetch(`${apiBaseUrl}/test-message`)
      .then(res => {
        if (!res.ok) {
          throw new Error(`HTTP ${res.status}`)
        }
        return res.json()
      })
      .then(data => setMessage(data.message))
      .catch(() => setMessage('メッセージ取得に失敗しました'))
  }, [])

  return (
    <>
      <div>{message}</div>
    </>
  )
}

export default App
