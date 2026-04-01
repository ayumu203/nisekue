import './App.css'
import { Navigate, Route, Routes } from 'react-router-dom'
import Auth from '@/pages/Auth'
import Home from '@/pages/Home'
import Players from '@/pages/Players'
import Training from '@/pages/Training'
import Thanks from '@/pages/Thanks'
import PlayerSetting from '@/pages/PlayerSetting'
import PlayerImageList from '@/pages/PlayerImageList'
import MoveSetting from '@/pages/MoveSetting'
import JobChange from '@/pages/JobChange'
import Rebirth from '@/pages/Rebirth'
import Quest from '@/pages/Quest'
import VisitPlayer from '@/pages/VisitPlayer'
import Items from '@/pages/Items'
import TreasureMap from '@/pages/TreasureMap'
import Threads from '@/pages/Threads'
import ThreadDetail from '@/pages/ThreadDetail'
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
      <Route path="/players" element={user ? <Players /> : <Navigate to="/auth" replace />} />
      <Route path="/players/:playerId/visit" element={user ? <VisitPlayer /> : <Navigate to="/auth" replace />} />
      <Route path="/quest" element={user ? <Quest /> : <Navigate to="/auth" replace />} />
      <Route path="/quest/quest-solo-test" element={<Navigate to="/quest" replace />} />
      <Route path="/quest/quest-mult-test" element={<Navigate to="/quest" replace />} />
      <Route path="/training" element={user ? <Training /> : <Navigate to="/auth" replace />} />
      <Route path="/items" element={user ? <Items /> : <Navigate to="/auth" replace />} />
      <Route path="/treasure-map" element={user ? <TreasureMap /> : <Navigate to="/auth" replace />} />
      <Route path="/threads" element={user ? <Threads /> : <Navigate to="/auth" replace />} />
      <Route path="/threads/:threadId" element={user ? <ThreadDetail /> : <Navigate to="/auth" replace />} />
      <Route path="/player-setting" element={user ? <PlayerSetting /> : <Navigate to="/auth" replace />} />
      <Route path="/player-images" element={user ? <PlayerImageList /> : <Navigate to="/auth" replace />} />
      <Route path="/move-setting" element={user ? <MoveSetting /> : <Navigate to="/auth" replace />} />
      <Route path="/job-change" element={user ? <JobChange /> : <Navigate to="/auth" replace />} />
      <Route path="/rebirth" element={user ? <Rebirth /> : <Navigate to="/auth" replace />} />
      <Route path="/thanks" element={<Thanks />} />
      <Route path="*" element={<Navigate to={user ? '/' : '/auth'} replace />} />
    </Routes>
  )
}

export default App
