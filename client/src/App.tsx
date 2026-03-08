import './App.css'
import { Navigate, Route, Routes } from 'react-router-dom'
import Auth from '@/pages/Auth'
import Home from '@/pages/Home'
import Training from '@/pages/Training'
import Thanks from '@/pages/Thanks'
import PlayerSetting from '@/pages/PlayerSetting'
import MoveSetting from '@/pages/MoveSetting'
import { useAuth } from '@/contexts/useAuth'

function App() {
  const { isLoading, user } = useAuth()

  if (isLoading) {
    return null
  }

  return (
    <Routes>
      <Route path="/auth" element={user ? <Navigate to="/" replace /> : <Auth />} />
      <Route path="/" element={user ? <Home /> : <Navigate to="/auth" replace />} />
      <Route path="/training" element={user ? <Training /> : <Navigate to="/auth" replace />} />
      <Route path="/player-setting" element={user ? <PlayerSetting /> : <Navigate to="/auth" replace />} />
      <Route path="/move-setting" element={user ? <MoveSetting /> : <Navigate to="/auth" replace />} />
      <Route path="/thanks" element={<Thanks />} />
      <Route path="*" element={<Navigate to={user ? '/' : '/auth'} replace />} />
    </Routes>
  )
}

export default App
